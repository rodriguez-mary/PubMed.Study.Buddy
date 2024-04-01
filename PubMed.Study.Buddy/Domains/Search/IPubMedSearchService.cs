using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Search;

public interface IPubMedSearchService
{
    //return a list of article Ids
    Task<List<Article>> FindArticles(ArticleFilter filter);

    Task<Dictionary<string, MeshTerm>> GetMeshTerms();
}