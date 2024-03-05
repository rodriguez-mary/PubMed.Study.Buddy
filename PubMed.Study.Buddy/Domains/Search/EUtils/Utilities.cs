using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;

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
            $"{EUtilsConstants.DatabaseField}={EUtilsConstants.PubMedDbId}"
        };
        var termParams = new List<string>();

        // Date parameters
        if (filter.StartYear != null)
            queryParams.Add($"{EUtilsConstants.StartDateParameter}={filter.StartYear}[{EUtilsConstants.PublishDateType}]");
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

    public static DTOs.Article CompileArticleFromResponses(EFetchResult fetchResponse, ELinkResult linkResponse)
    {
        throw new NotImplementedException();
    }
}