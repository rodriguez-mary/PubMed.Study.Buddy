using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Search;

internal interface IPubMedSearchService
{
    //return a list of article Ids
    public Task<List<Study.Buddy.DTOs.Article>> FindArticles(ArticleFilter filter);
}