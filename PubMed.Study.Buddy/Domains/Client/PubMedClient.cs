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

    public async Task<List<Article>> FindArticles(List<ArticleFilter> filters)
    {
        var articleIds = new List<string>();
        var articles = new List<Article>();

        //find the articles
        foreach (var filter in filters)
        {
            articles.AddRange(await _searchService.FindArticles(filter));
        }

        //get additional data about each article
        for (var i = 0; i < articles.Count; i++)
        {
            var article = articles[i];

            //deduplicate while we're at it
            if (articleIds.Contains(article.Id))
            {
                articles.RemoveAt(i);
                continue;
            }

            article.ImpactScore = await _impactScoringService.GetImpactScore(article);
            articleIds.Add(article.Id);
        }

        return articles;
    }

    public async Task<List<Article>> FindArticles(ArticleFilter filter)
    {
        return await FindArticles(new List<ArticleFilter> { filter });
    }

    public async Task GenerateContent(List<Article> articles)
    {
        await _outputService.GenerateArticleList(articles);
    }
}