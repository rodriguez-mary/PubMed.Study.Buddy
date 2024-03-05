using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Search.EUtils;

internal class EUtilsSearchService : IPubMedSearchService
{
    private const string DefaultEUtilsAddress = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";

    private readonly string _apiKey;
    private readonly ILogger<EUtilsSearchService> _logger;
    private readonly HttpClient _httpClient;

    private static readonly XmlDeserializer<ESearchResult> SearchResultDeserializer = new();
    private static readonly XmlDeserializer<ELinkResult> LinkResultDeserializer = new();

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
        //todo validate year input
        var articleIds = await GetArticleIds(filter);
        var citationCounts = await GetArticleCitationCounts(articleIds);

        throw new NotImplementedException();
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

        while (hasMoreData)
        {
            var result = await _httpClient.GetAsync($"{uri}&retstart={retStart}");

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();

            var searchResponse = SearchResultDeserializer.DeserializeXml(contentString);

            if (searchResponse == null)
            {
                //throw error
                break;
            }

            hasMoreData = searchResponse.RetStart < searchResponse.Count;

            idList.AddRange(searchResponse.IdList);

            retStart += searchResponse.RetMax;
        }

        return idList;
    }

    //get citations per article
    private async Task<Dictionary<string, int>> GetArticleCitationCounts(List<string> ids)
    {
        var citationCounts = new Dictionary<string, int>();

        var baseUri =
            $"{EUtilsConstants.LinkEndpoint}?{EUtilsConstants.OriginalDatabaseParameter}={EUtilsConstants.PubMedDbId}&{EUtilsConstants.LinkTypeParameter}={EUtilsConstants.CitationLinkType}";
        if (!string.IsNullOrEmpty(_apiKey))
            baseUri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        foreach (var id in ids)
        {
            await Task.Delay(1000);
            var uri = $"{baseUri}&id={id}";
            var result = await _httpClient.GetAsync(uri);

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();
            var linkResult = LinkResultDeserializer.DeserializeXml(contentString);

            citationCounts.Add(id, linkResult?.LinkSet.LinkSetDb?.Links?.Count ?? 0);
        }

        var byCitationNumber = new Dictionary<int, List<string>>();

        foreach (var (k, v) in citationCounts)
        {
            if (!byCitationNumber.ContainsKey(v))
                byCitationNumber.Add(v, new List<string>());

            byCitationNumber[v].Add(k);
        }

        return citationCounts;
    }

    //query for metadata for the top X articles
    private List<EFetchResult> GetArticleMetadata(List<string> articleIds)
    {
        throw new NotImplementedException();
    }
}