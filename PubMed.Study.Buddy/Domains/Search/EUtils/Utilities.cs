﻿using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Author = PubMed.Study.Buddy.DTOs.Author;

namespace PubMed.Study.Buddy.Domains.Search.EUtils;

internal static class Utilities
{
    /// <summary>
    /// Create a query string for the eSearch PubMed utility from an article filter.
    /// </summary>
    public static string GetQueryFromArticleFilter(ArticleFilter filter)
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
            .Select(j => $"{Uri.EscapeDataString(j)}[{EUtilsConstants.JournalField}]") ?? Enumerable.Empty<string>();
        if (journalParams.Any())
            termParams.Add($"({string.Join("+OR+", journalParams)})");

        // MeSH terms parameters
        // I cannot for the life of me figure out why some mesh terms are listed as majors, some as minors, and some just as terms, so we're searching for any of them
        var meshParams = filter.MeshTerm?.Select(term =>
            $"({string.Join("+OR+", $"{term}[{EUtilsConstants.MeshField}]",
                $"{term}[{EUtilsConstants.MeshMajorTopicField}]",
                $"{term}[{EUtilsConstants.MeshSubheadingField}]")})") ?? Enumerable.Empty<string>();
        if (meshParams.Any())
            termParams.Add(string.Join("+AND+", meshParams));

        // Put together the journal & MeSH params into the single term query parameter
        if (termParams.Any())
            queryParams.Add($"{EUtilsConstants.TermParameter}={string.Join("+AND+", termParams)}");

        return string.Join("&", queryParams);
    }

    public static Article CompileArticleFromResponses(string id, EFetchResult fetchResponse, ELinkResult linkResponse)
    {
        var article = new Article
        {
            Id = id
        };

        if (fetchResponse.PubmedArticles.Count <= 0) return article;

        var pubMedArticle = fetchResponse.PubmedArticles.First(); //there should only be one

        var medlineArticle = pubMedArticle.MedlineCitation.Article;
        article.Title = medlineArticle.ArticleTitle;

        var publishedDate = GetPublishedDateFromArticleDate(pubMedArticle) ?? GetPublishedDateFromPubmedHistory(pubMedArticle);
        if (publishedDate.HasValue) article.PublicationDate = publishedDate.Value;

        if (medlineArticle.AuthorList is { Authors.Count: > 0 })
        {
            article.AuthorList = new List<Author>();
            for (var i = 0; i < medlineArticle.AuthorList.Authors.Count; i++)
            {
                var author = medlineArticle.AuthorList.Authors[i];

                article.AuthorList.Add(new Author
                {
                    First = (i == 0), //we should get back the "first" author first from the pubmed xml
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
                    DateTime.TryParseExact($"01/{date.Month}/{date.Year}", new[] { "DD/MM/YYYY", "DD/MM/YY" },
                        new CultureInfo("en-US"), DateTimeStyles.None, out var pubDate))
                    article.Publication.JournalDate = pubDate;
            }
        }

        if (medlineArticle.MeshHeadingList is { MeshHeadings.Count: > 0 })
        {
            article.MeshMainHeadings = new List<string>();
            foreach (var meshHeading in medlineArticle.MeshHeadingList.MeshHeadings)
            {
                if (meshHeading.DescriptorName != null)
                    article.MeshMainHeadings.Add(meshHeading.DescriptorName.Name);
            }
        }

        if (linkResponse.LinkSet.LinkSetDb is { Links.Count: > 0 })
        {
            article.CitedBy = new List<string>();
            foreach (var link in linkResponse.LinkSet.LinkSetDb.Links)
            {
                article.CitedBy.Add(link.Id);
            }
        }

        //todo get abstract

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
        PubMedPubDate? date = null;
        //get the date for the history status of "pubmed"
        foreach (var hx in pubmedArticle.PubmedData.History.PubMedPubDates)
        {
            if (!string.Equals("pubmed", hx.PubStatus)) continue;
            date = hx;
            break;
        }
        if (date == null) return null;

        if (DateTime.TryParse($"{date.Year}-{date.Month}-{date.Day}", out var pubDate))
            return pubDate;

        return null;
    }
}