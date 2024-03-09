using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Search.EUtils;

public class EUtilsSearchService : IPubMedSearchService
{
    private const string DefaultEUtilsAddress = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";

    private readonly string _apiKey;
    private readonly ILogger<EUtilsSearchService> _logger;
    private readonly HttpClient _httpClient;

    public EUtilsSearchService(ILogger<EUtilsSearchService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        var eUtilsAddress = configuration["pubmedEUtilsAddress"] ?? DefaultEUtilsAddress;
        var apiKey = configuration["pubmedApiKey"] ?? string.Empty;

        _apiKey = apiKey;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(eUtilsAddress);
    }

    public async Task<List<Article>> FindArticles(ArticleFilter filter)
    {
        var articles = new List<Article>();

        //todo validate year input
        var articleIds = await GetArticleIds(filter);

        var citationCounts = await GetArticleCitationData(articleIds);
        var articleMetadata = await GetArticleMetadata(articleIds);
        var articleAbstracts = await GetArticleAbstract(articleIds);

        foreach (var id in articleIds)
        {
            if (!articleMetadata.TryGetValue(id, out var fetchData)) continue;
            if (!citationCounts.TryGetValue(id, out var linkData)) linkData = new ELinkResult();
            if (!articleAbstracts.TryGetValue(id, out var articleAbstract)) articleAbstract = string.Empty;

            articles.Add(Utilities.CompileArticleFromResponses(id, fetchData, linkData, articleAbstract));
        }

        return articles;
    }

    //get list of article IDs
    private async Task<List<string>> GetArticleIds(ArticleFilter filter)
    {
        var uri = $"{EUtilsConstants.SearchEndpoint}?{Utilities.GetQueryFromArticleFilter(filter)}";
        if (!string.IsNullOrEmpty(_apiKey))
            uri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        return await PaginateThroughArticleSearch(uri);
    }

    private async Task<List<string>> PaginateThroughArticleSearch(string uri)
    {
        var idList = new List<string>();
        var hasMoreData = true;
        var retStart = 0;
        var retMax = 100;

        while (hasMoreData)
        {
            var result = await _httpClient.GetAsync($"{uri}&retstart={retStart}&retmax={retMax}");

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();

            var searchResponse = XmlDeserializer<ESearchResult>.DeserializeXml(contentString);

            if (searchResponse == null)
            {
                //todo throw error
                break;
            }

            hasMoreData = searchResponse.RetStart < searchResponse.Count;

            idList.AddRange(searchResponse.IdList);

            retStart += searchResponse.RetMax;
        }

        return idList;
    }

    //get citations per article
    private async Task<Dictionary<string, ELinkResult>> GetArticleCitationData(List<string> ids)
    {
        var citationCounts = new Dictionary<string, ELinkResult>();

        var baseUri =
            $"{EUtilsConstants.LinkEndpoint}?{EUtilsConstants.OriginalDatabaseParameter}={EUtilsConstants.PubMedDbId}&{EUtilsConstants.LinkTypeParameter}={EUtilsConstants.CitationLinkType}";
        if (!string.IsNullOrEmpty(_apiKey))
            baseUri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        foreach (var id in ids)
        {
            var uri = $"{baseUri}&{EUtilsConstants.IdParameter}={id}";
            var result = await _httpClient.GetAsync(uri);

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();
            var linkResult = XmlDeserializer<ELinkResult>.DeserializeXml(contentString);

            if (linkResult != null) citationCounts.Add(id, linkResult);
        }

        return citationCounts;
    }

    //query for metadata for the top X articles
    private async Task<Dictionary<string, PubmedArticle>> GetArticleMetadata(List<string> ids)
    {
        const int stepCount = 100;
        var results = new Dictionary<string, PubmedArticle>();

        var baseUri =
            $"{EUtilsConstants.FetchEndpoint}?{EUtilsConstants.DatabaseParameter}={EUtilsConstants.PubMedDbId}";
        if (!string.IsNullOrEmpty(_apiKey))
            baseUri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        for (var i = 0; i < ids.Count; i += stepCount)
        {
            //request in bulk
            var count = i + stepCount > ids.Count ? ids.Count - i : stepCount;
            var requestIds = string.Join(",", ids.GetRange(i, count));

            var uri = $"{baseUri}&{EUtilsConstants.IdParameter}={requestIds}";
            var result = await _httpClient.GetAsync(uri);

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();
            var fetchResult = XmlDeserializer<EFetchResult>.DeserializeXml(contentString);

            if (fetchResult != null)
            {
                foreach (var article in fetchResult.PubmedArticles)
                {
                    results.Add(article.MedlineCitation.Id, article);
                }
            }
        }

        return results;
    }

    private async Task<Dictionary<string, string>> GetArticleAbstract(List<string> ids)
    {
        //https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pubmed&id=37846027&retmode=text&rettype=abstract

        var results = new Dictionary<string, string>();

        var baseUri =
            $"{EUtilsConstants.FetchEndpoint}?{EUtilsConstants.DatabaseParameter}={EUtilsConstants.PubMedDbId}";
        if (!string.IsNullOrEmpty(_apiKey))
            baseUri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        foreach (var id in ids)
        {
            var uri = $"{baseUri}&{EUtilsConstants.IdParameter}={id}&retmode=text&rettype=abstract";
            var result = await _httpClient.GetAsync(uri);

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();
            results.Add(id, contentString);
        }

        return results;
    }
}