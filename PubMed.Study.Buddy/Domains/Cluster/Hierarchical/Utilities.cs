using PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

internal static class Utilities
{
    private static readonly IReadOnlyList<string> _taxonomyBranchesToExclude = ["B"];  // branches of the mesh term taxonomy we want to ignore

    /// <summary>
    /// Creates a count of all the articles that *could* be rolled up to every particular taxonomy level.
    /// This will be used to determine how high up we would need to climb to get to an appropriately sized taxonomy grouping.
    /// </summary>
    public static Dictionary<string, TaxonomyCount> GetTaxonomyCounts(List<Article> articles)
    {
        var taxonomyCount = new Dictionary<string, TaxonomyCount>();

        foreach (var article in articles)
        {
            var lineage = ArticleLineage(article);

            foreach (var number in lineage)
            {
                if (string.IsNullOrEmpty(number)) continue;
                if (!taxonomyCount.ContainsKey(number))
                    taxonomyCount.Add(number, new TaxonomyCount());

                taxonomyCount[number].ArticleIds.Add(article.Id);
            }
        }

        return taxonomyCount;
    }

    /// <summary>
    /// Given an article, return it's mesh term taxonomy lineage.
    /// </summary>
    /// <returns>List of tree numbers in the taxonomy to which the article belongs or inherits.</returns>
    public static List<string> ArticleLineage(Article article)
    {
        var lineage = new List<string>();

        if (article.MajorTopicMeshHeadings == null) return lineage;

        foreach (var meshHeading in article.MajorTopicMeshHeadings)
        {
            foreach (var treeNumber in meshHeading.TreeNumbers)
            {
                if (_taxonomyBranchesToExclude.Contains(TreeBranch(treeNumber))) continue;

                var splitNumbers = treeNumber.Split(".");
                for (var i = 0; i < splitNumbers.Length; i++)
                {
                    var number = string.Join(".", splitNumbers[..(i + 1)]);
                    lineage.Add(number);
                }
            }
        }

        return lineage;
    }

    /// <summary>
    ///  A tree number's taxonomy branch.
    /// </summary>
    private static string TreeBranch(string treeNumber)
    {
        if (treeNumber.Length < 1) return string.Empty;
        return treeNumber[..1];
    }
}