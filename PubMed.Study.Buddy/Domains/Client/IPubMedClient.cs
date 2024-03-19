using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public interface IPubMedClient
{
    Task<List<Article>> FindArticles(List<ArticleFilter> filter);

    Task GenerateArticleDataFile(List<Article> articles);
}