using PubMed.Study.Buddy.Domains.Search.EUtils;

namespace PubMed.Study.Buddy.DTOs;

[Serializable]
public class Article
{
    public string Id { get; set; } = string.Empty;

    public DateTime PublicationDate { get; set; } = DateTime.MinValue;

    public string Abstract { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string PubMedUrl => $"{EUtilsConstants.PubMedBaseUrl}{Id}";

    public List<Author>? AuthorList { get; set; }

    public List<string>? MajorTopicMeshHeadings { get; set; }
    public List<string>? MinorTopicMeshHeadings { get; set; }

    public Publication? Publication { get; set; }

    public double ImpactScore { get; set; } = 0;

    /// <summary>
    /// List of PubMed article IDs that cite this article.
    /// </summary>
    public List<string>? CitedBy { get; set; }
}