using PubMed.Article.Extract.Utility.Domains.Search;

namespace PubMed.Article.Extract.Utility.DTOs;

public class Article
{
    public string Id { get; set; } = string.Empty;

    public Publication Publication { get; set; } = new();

    public string Title { get; set; } = string.Empty;

    public string PubMedUrl => $"{PubMedConstants.PubMedBaseUrl}{Id}";

    public List<Author>? AuthorList { get; set; }

    public List<string>? CategoryList { get; set; }

    /// <summary>
    /// List of PubMed article IDs that cite this article.
    /// </summary>
    public List<string>? CitedBy { get; set; }
}