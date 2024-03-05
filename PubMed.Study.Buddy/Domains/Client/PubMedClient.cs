using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public class PubMedClient : IPubMedClient
{
    private readonly ILogger _logger;
    private readonly IPubMedSearchService _searchService;
    private readonly IImpactScoringService _impactScoringService;
    private readonly IOutputService _outputService;

    public PubMedClient(ILogger<PubMedClient> logger, IPubMedSearchService searchService,
        IImpactScoringService impactScoringService, IOutputService outputService)
    {
        _logger = logger;
        _searchService = searchService;
        _impactScoringService = impactScoringService;
        _outputService = outputService;
    }

    public async Task<List<Article>> FindArticles(ArticleFilter filter)
    {
        var articles = await _searchService.FindArticles(filter);

        foreach (var article in articles)
            article.ImpactScore = await _impactScoringService.GetImpactScore(article);

        return articles;
    }

    public async Task GenerateContent(List<Article> articles)
    {
        await _outputService.GenerateArticleList(articles);
    }
}