namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;

/// <summary>
/// Taxonomy count class to ease of access for sorting
/// </summary>
internal class TaxonomyCount
{
    public int Count
    { get { return ArticleIds.Count; } }

    public List<string> ArticleIds = [];
}