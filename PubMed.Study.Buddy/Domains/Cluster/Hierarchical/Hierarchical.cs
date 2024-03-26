using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

public class HierarchicalClusteringService(Dictionary<string, MeshTerm> meshTerms) : IClusterService
{
    private const int MaxDistance = 1000;
    private List<MeshTerm> _meshTermsList = [];
    private Dictionary<string, int> _matrixKeys = [];
    private int[,] _distanceMatrix = new int[0, 0];

    /// <summary>
    /// Create a preprocessed distance matrix for all the mesh terms.
    /// </summary>
    public void Initialize()
    {
        _meshTermsList = [.. meshTerms.Values];
        _matrixKeys = [];

        var index = 0;
        foreach (var meshTerm in _meshTermsList)
        {
            _matrixKeys[meshTerm.DescriptorId] = index;
            index++;
        }

        _distanceMatrix = CreateDistanceMatrix();
    }

    public List<Models.Cluster> GetClusters(List<Article> articles)
    {
        throw new NotImplementedException();
    }

    private static int ArticleDistance(Article article1, Article article2)
    {
        // for an article, we need to determine the minimum distance for every descriptor on that article
        // to ANY descriptor on the other article
        return 0;
    }

    /// <summary>
    /// This creates a preprocessed distance matrix with all the mesh terms we care about.
    /// </summary>
    private int[,] CreateDistanceMatrix()
    {
        var size = _meshTermsList.Count;
        var matrix = new int[size, size];

        for (var i = 0; i < _meshTermsList.Count; i++)
        {
            for (var j = i + 1; j < _meshTermsList.Count; j++)
            {
                var distance = MinimumDistance(_meshTermsList[i].TreeNumber, _meshTermsList[j].TreeNumber);
                matrix[i, j] = distance;
                matrix[j, i] = distance;
            }
        }

        return matrix;
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