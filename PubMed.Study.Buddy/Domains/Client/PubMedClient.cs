﻿using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public class PubMedClient(ILogger<PubMedClient> logger, IPubMedSearchService searchService,
    IImpactScoringService impactScoringService, IOutputService outputService) : IPubMedClient
{
    public async Task<List<Article>> FindArticles(List<ArticleFilter> filters)
    {
        var articleIds = new List<string>();
        var articles = new List<Article>();

        // find the articles
        foreach (var filter in filters)
        {
            articles.AddRange(await searchService.FindArticles(filter));
        }

        // process each article, cleaning up data and adding additional data
        for (var i = 0; i < articles.Count; i++)
        {
            var article = articles[i];

            //deduplicate
            if (articleIds.Contains(article.Id))
            {
                articles.RemoveAt(i);
                continue;
            }

            article.ImpactScore = await impactScoringService.GetImpactScore(article);
            articleIds.Add(article.Id);
        }

        logger.LogInformation("{articleCount} articles found", articles.Count);

        return articles;
    }

    public async Task GenerateContent(List<Article> articles)
    {
        await outputService.GenerateArticleList(articles);
    }
}