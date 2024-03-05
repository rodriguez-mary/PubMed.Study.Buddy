using PubMed.Article.Extract.Utility.DTOs;

namespace PubMed.Article.Extract.Utility.Domains.Search;

internal interface IPubMedSearchService
{
    //return a list of article Ids
    public Task<List<DTOs.Article>> FindArticles(ArticleFilter filter);
}