using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public interface IPubMedClient
{
    Task<List<Article>> FindArticles(List<ArticleFilter> filter);

    Task<Dictionary<string, MeshTerm>> GetMeshTerms();

    Task GenerateArticleDataFile(List<Article> articles);

    Task<List<CardSet>> GenerateFlashCards(List<ArticleSet> articles);
}