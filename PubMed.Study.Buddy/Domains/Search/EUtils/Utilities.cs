using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;
using Author = PubMed.Study.Buddy.DTOs.Author;

namespace PubMed.Study.Buddy.Domains.Search.EUtils;

internal class Utilities(IReadOnlyDictionary<string, MeshTerm> meshTerms)
{
    /// <summary>
    /// Create a query string for the eSearch PubMed utility from an article filter.
    /// </summary>
    public string GetQueryFromArticleFilter(ArticleFilter filter)
    {
        var queryParams = new List<string>
        {
            $"{EUtilsConstants.DatabaseParameter}={EUtilsConstants.PubMedDbId}"
        };
        var termParams = new List<string>();

        // Date parameters
        if (filter.StartYear != null)
            queryParams.Add(
                $"{EUtilsConstants.StartDateParameter}={filter.StartYear}[{EUtilsConstants.PublishDateType}]");
        if (filter.EndYear != null)
            queryParams.Add($"{EUtilsConstants.EndDateParameter}={filter.EndYear}[{EUtilsConstants.PublishDateType}]");

        // Journal parameters
        var journalParams = filter.Journal?.Where(j => !string.IsNullOrEmpty(j))
            .Select(j => $"{Uri.EscapeDataString(j)}[{EUtilsConstants.JournalField}]").ToList() ?? [];
        if (journalParams.Count > 0)
            termParams.Add($"({string.Join("+OR+", journalParams)})");

        // MeSH terms parameters
        // I cannot for the life of me figure out why some mesh terms are listed as majors, some as minors, and some just as terms, so we're searching for any of them
        if (filter.MeshTerm != null)
        {
            //outer list is ANDed together
            foreach (var orList in filter.MeshTerm)
            {
                var orTerms = new List<string>();
                //inner list is ORed together
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var termId in orList)
                {
                    //map the id to the mesh descriptor name; eutils search uses the descriptor name for filtering
                    if (!meshTerms.ContainsKey(termId)) continue;

                    var term = meshTerms[termId].DescriptorName;
                    var escapedTerm = Uri.EscapeDataString(term);
                    orTerms.Add(string.Join("+OR+", $"{escapedTerm}[{EUtilsConstants.MeshField}]",
                        $"{escapedTerm}[{EUtilsConstants.MeshMajorTopicField}]",
                        $"{escapedTerm}[{EUtilsConstants.MeshSubheadingField}]"));
                }
                termParams.Add($"({string.Join("+OR+", orTerms)})");
            }
        }

        // Put together the journal & MeSH params into the single term query parameter
        if (termParams.Count > 0)
            queryParams.Add($"{EUtilsConstants.TermParameter}={string.Join("+AND+", termParams)}");

        return string.Join("&", queryParams);
    }

    public Article CompileArticleFromResponses(string id, PubmedArticle pubMedArticle, ELinkResult linkResponse, string articleAbstract)
    {
        var article = new Article
        {
            Id = id,
            Abstract = articleAbstract
        };

        var medlineCitation = pubMedArticle.MedlineCitation;
        var medlineArticle = medlineCitation.Article;
        article.Title = medlineArticle.ArticleTitle;

        var publishedDate = GetPublishedDateFromArticleDate(pubMedArticle) ?? GetPublishedDateFromPubmedHistory(pubMedArticle);
        if (publishedDate.HasValue) article.PublicationDate = publishedDate.Value;

        if (medlineArticle.AuthorList is { Authors.Count: > 0 })
        {
            article.AuthorList = [];
            for (var i = 0; i < medlineArticle.AuthorList.Authors.Count; i++)
            {
                var author = medlineArticle.AuthorList.Authors[i];

                article.AuthorList.Add(new Author
                {
                    First = i == 0, //we should get back the "first" author first from the pubmed xml
                    FirstName = author.ForeName,
                    LastName = author.LastName,
                    Initials = author.Initials
                });
            }
        }

        if (medlineArticle.Journal != null)
        {
            article.Publication = new Publication
            {
                JournalName = medlineArticle.Journal.Title
            };

            if (medlineArticle.Journal.JournalIssue != null)
            {
                article.Publication.Volume = medlineArticle.Journal.JournalIssue.Volume;

                var date = medlineArticle.Journal.JournalIssue.PubDate;
                if (date != null &&
                    DateTime.TryParse($"{date.Year}-{date.Month}-01", out var pubDate))
                    article.Publication.JournalDate = pubDate;
            }
        }

        if (medlineCitation.MeshHeadingList is { MeshHeadings.Count: > 0 })
        {
            var majorMeshHeading = new List<MeshTerm>();
            var minorMeshHeading = new List<MeshTerm>();

            foreach (var meshHeading in medlineCitation.MeshHeadingList.MeshHeadings)
            {
                if (meshHeading.DescriptorName == null) continue;
                if (!meshTerms.TryGetValue(meshHeading.DescriptorName.Id, out var meshTerm)) continue;

                if (string.Equals(meshHeading.DescriptorName.MajorTopicYn, "Y"))
                {
                    majorMeshHeading.Add(meshTerm);
                }
                else
                {
                    //if the descriptor isn't flagged as major topic, check the qualifiers
                    //looking at the pubmed data, it seems many articles pre-2020 are flagged in this way rather than flagging on the descriptor
                    if (meshHeading.QualifierNames != null)
                    {
                        var major = meshHeading.QualifierNames.Any(qualifierName => string.Equals(qualifierName.MajorTopicYn, "Y"));

                        if (major)
                        {
                            majorMeshHeading.Add(meshTerm);
                        }
                        else
                        {
                            minorMeshHeading.Add(meshTerm);
                        }
                    }
                    else
                    {
                        minorMeshHeading.Add(meshTerm);
                    }
                }
            }

            if (majorMeshHeading.Count > 0) article.MajorTopicMeshHeadings = majorMeshHeading;
            if (minorMeshHeading.Count > 0) article.MinorTopicMeshHeadings = minorMeshHeading;
        }

        if (linkResponse.LinkSet.LinkSetDb is not { Links.Count: > 0 }) return article;

        article.CitedBy = [];
        foreach (var link in linkResponse.LinkSet.LinkSetDb.Links)
        {
            article.CitedBy.Add(link.Id);
        }

        return article;
    }

    private static DateTime? GetPublishedDateFromArticleDate(PubmedArticle pubmedArticle)
    {
        var date = pubmedArticle.MedlineCitation.Article.ArticleDate;
        if (date == null) return null;

        if (DateTime.TryParse($"{date.Year}-{date.Month}-{date.Day}", out var pubDate))
            return pubDate;

        return null;
    }

    private static DateTime? GetPublishedDateFromPubmedHistory(PubmedArticle pubmedArticle)
    {
        //get the date for the history status of "pubmed"
        var date = pubmedArticle.PubmedData.History.PubMedPubDates.FirstOrDefault(hx => string.Equals(EUtilsConstants.PublishedHxStatusIdentifier, hx.PubStatus));

        if (date == null) return null;

        if (DateTime.TryParse($"{date.Year}-{date.Month}-{date.Day}", out var pubDate))
            return pubDate;

        return null;
    }
}