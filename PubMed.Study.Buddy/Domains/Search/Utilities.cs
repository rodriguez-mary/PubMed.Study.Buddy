using PubMed.Article.Extract.Utility.Domains.Search.Models;
using PubMed.Article.Extract.Utility.DTOs;

namespace PubMed.Article.Extract.Utility.Domains.Search;

internal static class Utilities
{
    /// <summary>
    /// Create a query string for the eSearch PubMed utility from an article filter.
    /// </summary>
    public static string GetQueryFromArticleFilter(ArticleFilter filter)
    {
        var queryParams = new List<string>
        {
            $"{PubMedConstants.DatabaseField}={PubMedConstants.PubMedDbId}"
        };
        var termParams = new List<string>();

        // Date parameters
        if (filter.StartYear != null)
            queryParams.Add($"{PubMedConstants.StartDateParameter}={filter.StartYear}[{PubMedConstants.PublishDateType}]");
        if (filter.EndYear != null)
            queryParams.Add($"{PubMedConstants.EndDateParameter}={filter.EndYear}[{PubMedConstants.PublishDateType}]");

        // Journal parameters
        var journalParams = filter.Journal?.Where(j => !string.IsNullOrEmpty(j))
            .Select(j => $"{Uri.EscapeDataString(j)}[{PubMedConstants.JournalField}]") ?? Enumerable.Empty<string>();
        if (journalParams.Any())
            termParams.Add($"({string.Join("+OR+", journalParams)})");

        // MeSH terms parameters
        // I cannot for the life of me figure out why some mesh terms are listed as majors, some as minors, and some just as terms, so we're searching for any of them
        var meshParams = filter.MeshTerm?.Select(term =>
            $"({string.Join("+OR+", $"{term}[{PubMedConstants.MeshField}]",
                $"{term}[{PubMedConstants.MeshMajorTopicField}]",
                $"{term}[{PubMedConstants.MeshSubheadingField}]")})") ?? Enumerable.Empty<string>();
        if (meshParams.Any())
            termParams.Add(string.Join("+AND+", meshParams));

        // Put together the journal & MeSH params into the single term query parameter
        if (termParams.Any())
            queryParams.Add($"{PubMedConstants.TermParameter}={string.Join("+AND+", termParams)}");

        return string.Join("&", queryParams);
    }

    public static DTOs.Article CompileArticleFromResponses(EFetchResult fetchResponse, ELinkResult linkResponse)
    {
        throw new NotImplementedException();
    }
}