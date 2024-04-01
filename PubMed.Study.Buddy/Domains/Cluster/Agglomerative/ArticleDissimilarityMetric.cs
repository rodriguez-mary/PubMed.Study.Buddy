using Aglomera;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Agglomerative;

public class ArticleDissimilarityMetric : IDissimilarityMetric<Article>
{
    private const int MaxDistance = 1000;

    public double Calculate(Article instance1, Article instance2)
    {
        return ArticleDistance(instance1, instance2);
    }

    private int ArticleDistance(Article instance1, Article instance2)
    {
        var list1 = instance1.MajorTopicMeshHeadings;
        var list2 = instance2.MajorTopicMeshHeadings;

        if (list1 == null || list2 == null) return MaxDistance;
        if (list1.Count == 0 || list2.Count == 0) return MaxDistance;

        return DirectionalArticleDistance(list1, list2) + DirectionalArticleDistance(list2, list1);
    }

    /// <summary>
    /// Every article has a list of mesh terms. Each mesh term has a list of valid tree "addresses".
    /// In order to calculate the distance from one article to another, we need to count up
    /// the minimum distance from every mesh term in an article to ANY mesh term in the other article
    /// </summary>
    private static int DirectionalArticleDistance(List<MeshTerm> start, List<MeshTerm> end)
    {
        var distance = 0;
        foreach (var startTerm in start)
        {
            var minDistance = MaxDistance;
            foreach (var endTerm in end)
            {
                var thisDistance = MinimumDistance(startTerm.TreeNumber, endTerm.TreeNumber);
                if (thisDistance < minDistance) minDistance = thisDistance;
                if (minDistance == 0) break; //we can't get any better
            }

            distance += minDistance;
        }

        return distance;
    }

    /// <summary>
    /// Determines the minimum distance from one list of mesh terms to another list of mesh terms.
    /// This should represent the FEWEST steps needed to traverse from any one member of one group
    /// to any one member of the other group.
    /// </summary>
    private static int MinimumDistance(IReadOnlyList<string> treeNumbers1, IReadOnlyList<string> treeNumbers2)
    {
        var minDistance = MaxDistance;

        for (var i = 0; i < treeNumbers1.Count; i++)
        {
            for (var j = 0; j < treeNumbers2.Count; j++)
            {
                var distance = Distance(treeNumbers1[i], treeNumbers2[j]);
                if (distance < minDistance) minDistance = distance;
                if (minDistance == 0) break;
            }
            if (minDistance == 0) break;
        }

        return minDistance;
    }

    /// <summary>
    /// Calculates the distance from one Mesh term to another.
    /// </summary>
    private static int Distance(string treeNumber1, string treeNumber2)
    {
        var decimals1 = treeNumber1.Split(".");
        var decimals2 = treeNumber2.Split(".");

        var maxLength = Math.Max(decimals1.Length, decimals2.Length);

        Array.Resize(ref decimals1, maxLength);
        Array.Resize(ref decimals2, maxLength);

        var distanceCount = MaxDistance;
        for (var i = 0; i < maxLength; i++)
        {
            // we march through the decimals until we find one that doesn't match
            if (decimals1[i] == decimals2[i]) continue;

            // if it doesn't match then the distance to traverse the tree is the number of
            // decimals that are off because each decimal divider represents another branch of the tree
            // unless we're at the first decimal--that is the root of the tree. if they don't match then
            // the terms are in entirely different trees
            if (i != 0) distanceCount = maxLength - i;
            break;
        }

        return distanceCount;
    }
}