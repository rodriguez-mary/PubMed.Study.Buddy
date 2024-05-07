using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Client;

public interface IPubMedClient
{
    Task<List<Article>> GetArticles(List<ArticleFilter> filter);

    Task<Dictionary<string, MeshTerm>> GetMeshTerms();

    Task<List<CardSet>> GenerateFlashCards(List<ArticleSet> articles);
}