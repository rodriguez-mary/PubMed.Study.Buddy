using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.Domains.Search.Exceptions;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Search.EUtils;

public class EUtilsSearchService : IPubMedSearchService
{
    private const string DefaultEUtilsAddress = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";

    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EUtilsSearchService> _logger;

    private Dictionary<string, MeshTerm>? _meshTerms;

    public EUtilsSearchService(ILogger<EUtilsSearchService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        var eUtilsAddress = configuration["pubmedEUtilsAddress"] ?? DefaultEUtilsAddress;
        var apiKey = configuration["pubmedApiKey"] ?? string.Empty;

        _logger = logger;
        _apiKey = apiKey;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.BaseAddress = new Uri(eUtilsAddress);
    }

    public async Task<List<Article>> FindArticles(ArticleFilter filter)
    {
        //todo validate year input

        var articles = new List<Article>();

        var meshTerms = await LoadMeshTerms();
        var utilities = new Utilities(meshTerms);

        var articleIds = await GetArticleIds(utilities, filter);

        var citationCounts = await GetArticleCitationData(articleIds);
        var articleMetadata = await GetArticleMetadata(articleIds);
        var articleAbstracts = await GetArticleAbstract(articleIds);

        foreach (var id in articleIds)
        {
            if (!articleMetadata.TryGetValue(id, out var fetchData)) continue;
            if (!citationCounts.TryGetValue(id, out var linkData)) linkData = new ELinkResult();
            if (!articleAbstracts.TryGetValue(id, out var articleAbstract)) articleAbstract = string.Empty;

            articles.Add(utilities.CompileArticleFromResponses(id, fetchData, linkData, articleAbstract));
        }

        return articles;
    }

    public async Task<Dictionary<string, MeshTerm>> GetMeshTerms()
    {
        var filename = Path.Combine(@"c:\temp\studybuddy", "meshterms.json");
        if (File.Exists(filename))
            return JsonConvert.DeserializeObject<Dictionary<string, MeshTerm>>(await File.ReadAllTextAsync(filename)) ?? [];
        return await LoadMeshTerms();
    }

    private async Task<Dictionary<string, MeshTerm>> LoadMeshTerms()
    {
        var filename = Path.Combine(@"c:\temp\studybuddy", "meshterms.json");
        if (_meshTerms != null) return _meshTerms;

        var meshTerms = new Dictionary<string, MeshTerm>();
        const string uri = "https://nlmpubs.nlm.nih.gov/projects/mesh/MESH_FILES/xmlmesh/desc2024.xml";
        var result = await _httpClient.GetAsync(uri);

        result.EnsureSuccessStatusCode();

        var contentString = await result.Content.ReadAsStringAsync();

        try
        {
            var meshTermResult = XmlDeserializer<MeshTermResult>.DeserializeXml(contentString);

            if (meshTermResult != null)
            {
                foreach (var descriptorRecord in meshTermResult.DescriptorRecords)
                {
                    meshTerms.Add(descriptorRecord.DescriptorId, new MeshTerm
                    {
                        DescriptorId = descriptorRecord.DescriptorId,
                        DescriptorName = descriptorRecord.DescriptorName.String,
                        TreeNumbers = descriptorRecord.TreeNumberList.TreeNumber
                    });
                }
            }
        }
        catch (InvalidPubMedDataException)
        {
            _logger.LogError("Invalid xml in search request for URI {uri}.", uri);
        }

        _meshTerms = meshTerms;

        var jsonString = JsonConvert.SerializeObject(meshTerms);
        await File.WriteAllTextAsync(filename, jsonString);

        return meshTerms;
    }

    //get list of article IDs
    private async Task<List<string>> GetArticleIds(Utilities utilities, ArticleFilter filter)
    {
        var uri = $"{EUtilsConstants.SearchEndpoint}?{utilities.GetQueryFromArticleFilter(filter)}";
        if (!string.IsNullOrEmpty(_apiKey))
            uri += $"&{EUtilsConstants.ApiKeyParameter}={_apiKey}";

        var ids = await PaginateThroughArticleSearch(uri);

        _logger.LogInformation("{articleCount} article IDs returned from eSearch.", ids.Count);

        return ids;
    }

    private async Task<List<string>> PaginateThroughArticleSearch(string uri)
    {
        var idList = new List<string>();
        var hasMoreData = true;
        var retStart = 0;
        const int retMax = 100;

        while (hasMoreData)
        {
            var result = await _httpClient.GetAsync($"{uri}&{EUtilsConstants.SkipParameter}={retStart}&{EUtilsConstants.TopParameter}={retMax}");

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();

            try
            {
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
            catch (InvalidPubMedDataException)
            {
                _logger.LogError("Invalid xml in search request for URI {uri}.", uri);
            }
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
            try
            {
                var linkResult = XmlDeserializer<ELinkResult>.DeserializeXml(contentString);

                if (linkResult != null) citationCounts.Add(id, linkResult);
            }
            catch (InvalidPubMedDataException)
            {
                _logger.LogError("Invalid xml from citation request for id {id}.", id);
            }
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
            try
            {
                var contentString = await result.Content.ReadAsStringAsync();
                var fetchResult = XmlDeserializer<EFetchResult>.DeserializeXml(contentString);

                if (fetchResult == null) continue;

                foreach (var article in fetchResult.PubmedArticles)
                {
                    results.Add(article.MedlineCitation.Id, article);
                }
            }
            catch (InvalidPubMedDataException)
            {
                _logger.LogError("Invalid xml in fetch request for ids {ids}.", string.Join(", ", requestIds));
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
            var uri = $"{baseUri}&{EUtilsConstants.IdParameter}={id}&{EUtilsConstants.ReturnFormatParameter}=text&{EUtilsConstants.ReturnTypeParameter}=abstract";
            var result = await _httpClient.GetAsync(uri);

            result.EnsureSuccessStatusCode();

            var contentString = await result.Content.ReadAsStringAsync();
            results.Add(id, contentString);
        }

        return results;
    }
}