using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Output;

public interface IOutputService
{
    Task GenerateArticleList(List<Article> articles);
}