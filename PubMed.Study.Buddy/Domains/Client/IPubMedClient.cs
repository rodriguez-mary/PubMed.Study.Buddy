using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public interface IPubMedClient
{
    Task<List<Article>> FindArticles(ArticleFilter filter);

    Task GenerateContent(List<Article> articles);
}