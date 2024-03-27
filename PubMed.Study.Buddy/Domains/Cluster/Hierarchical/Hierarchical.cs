using PubMed.Study.Buddy.DTOs;
using System.Text;

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

    public List<Models.Cluster> GetClusters(List<Article> baseArticles)
    {
        var articles = GetArticles(baseArticles, new List<string>{  });

        var articleKeys = new Dictionary<string, int>();
        var index = 0;
        foreach (var article in articles)
        {
            articleKeys[article.Id] = index;
            index++;
        }

        var size = articles.Count;
        var matrix = new int[size, size];

        for (var i = 0; i < articles.Count; i++)
        {
            for (var j = i + 1; j < articles.Count; j++)
            {
                var distance = ArticleDistance(articles[i], articles[j]);
                matrix[i, j] = distance;
                matrix[j, i] = distance;
            }
        }

        using var sw = new StreamWriter(@"c:\temp\studybuddy\agglomerative.csv", false, Encoding.UTF8);

        var header = "";
        for (var i = 0; i < articles.Count; i++)
            header += $",{articles[i].Id}";

        sw.WriteLine(header);

        for (var i = 0; i < articles.Count; i++)
        {
            sw.Write($"{articles[i].Id}");
            for (var j = 0; j < articles.Count; j++)
            {
                var x = matrix[i, j];
                if (x < MaxDistance)
                    sw.Write($",{x}");
                else
                    sw.Write(",");
            }

            sw.WriteLine();
        }

        return new List<Models.Cluster>();
    }

    private List<Article> GetArticles(List<Article> baseArticles, List<string> ids)
    {
        if (ids.Count == 0) return baseArticles;

        var articleLookup = new Dictionary<string, Article>();
        foreach (var art in baseArticles)
        {
            articleLookup.Add(art.Id, art);
        }

        var articles = new List<Article>();
        foreach (var id in ids)
        {
            articles.Add(articleLookup[id]);
        }

        return articles;
    }

    private int ArticleDistance(Article article1, Article article2)
    {
        var article1ListLength = article1.MajorTopicMeshHeadings?.Count ?? 0;
        var article2ListLength = article2.MajorTopicMeshHeadings?.Count ?? 0;
        if (article1ListLength == 0 || article2ListLength == 0) return MaxDistance;

        List<MeshTerm> longList;
        List<MeshTerm> shortList;
        if (article1ListLength > article2ListLength)
        {
            longList = article1.MajorTopicMeshHeadings!;
            shortList = article2.MajorTopicMeshHeadings!;
        }
        else
        {
            longList = article2.MajorTopicMeshHeadings!;
            shortList = article1.MajorTopicMeshHeadings!;
        }

        // we need to determine the minimum distance for every descriptor on that article
        // to ANY descriptor on the other article 
        var distance = 0;
        foreach (var meshTermA in longList)
        {
            // get the index for 
            if (!_matrixKeys.TryGetValue(meshTermA.DescriptorId, out var meshAIndex))
            {
                distance += MaxDistance;
                continue;
            }

            var minDistance = MaxDistance;
            // get the minimum distance to any of the other descriptors
            foreach (var meshTermB in shortList)
            {
                if (!_matrixKeys.TryGetValue(meshTermB.DescriptorId, out var meshBIndex))
                    continue;

                var thisDistance = _distanceMatrix[meshAIndex, meshBIndex];
                if (thisDistance < minDistance) minDistance = thisDistance;

                if (minDistance == 0) break;
            }

            // add that min distance to the total
            distance += minDistance;
        }

        return distance;
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