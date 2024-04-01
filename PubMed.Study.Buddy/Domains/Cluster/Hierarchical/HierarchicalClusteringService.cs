using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

public class HierarchicalClusteringService : IClusterService
{
    private readonly Dictionary<string, MeshTerm> _meshTerms;

    public HierarchicalClusteringService(Dictionary<string, MeshTerm> meshTerms)
    {
        _meshTerms = meshTerms;
        Initialize();
    }

    //this is the size at which we deem a cluster sufficiently large
    private const int MinClusterSize = 3;

    //this is the distance we deem "too far" to cluster content
    //max distance should supersede min size--it's useless if we cluster very disparate things together
    private const int MaxClusterDistance = 3000;  //a max distance of 3k means we allow for two bizzaro mesh terms that don't ever match with anything

    private const int MaxDistance = 1000;
    private List<MeshTerm> _meshTermsList = [];
    private Dictionary<string, int> _matrixKeys = [];
    private int[,] _distanceMatrix = new int[0, 0];

    /// <summary>
    /// Create a preprocessed distance matrix for all the mesh terms.
    /// </summary>
    private void Initialize()
    {
        _meshTermsList = [.. _meshTerms.Values];
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
        var articles = GetArticles(baseArticles, []);

        var matrix = GetArticleDistanceMatrix(articles);
        var clusters = ClusterArticles(articles, matrix);

        using var sw = new StreamWriter(@"c:\temp\studybuddy\hierarchical.csv", false, Encoding.UTF8);
        sw.WriteLine("article count,articles");
        foreach (var cluster in clusters)
        {
            var a = cluster.Articles;
            sw.WriteLine($"{a.Count},{string.Join(",", a)}");
        }

        return clusters;
    }

    private List<Models.Cluster> ClusterArticles(List<Article> articles, int[,] matrix)
    {
        var articleKeys = new Dictionary<string, int>();
        var index = 0;
        foreach (var article in articles)
        {
            articleKeys[article.Id] = index;
            index++;
        }

        var clusterDict = new Dictionary<int, Models.Cluster>();
        for (var i = 0; i < matrix.GetLength(0); i++)
            clusterDict.Add(i, new Models.Cluster());

        //basically, we have our list of articles that need to be clustered (articleKeys)
        //we iteratively loop through all the articles and cluster them by increasing distances
        //if they meet the requirement of min size
        //then we pull them from the list that needs to be clustered
        var colLength = matrix.GetLength(1);
        var currentMaxDistance = 0;
        while (currentMaxDistance <= MaxClusterDistance)
        {
            foreach (var (pos, cluster) in clusterDict)
            {
                if (cluster.Articles.Count >= MinClusterSize) continue;

                for (var i = 0; i < colLength; i++)
                {
                    if (matrix[pos, i] == currentMaxDistance)  //== rather than <= to avoid adding the same article multiple times
                    {
                        cluster.Articles.Add(articles[i]);
                    }
                }
            }

            currentMaxDistance++;
        }

        return [.. clusterDict.Values];
    }

    private int[,] GetArticleDistanceMatrix(IReadOnlyList<Article> articles)
    {
        var size = articles.Count;
        var matrix = new int[size, size];

        for (var i = 0; i < articles.Count; i++)
        {
            matrix[i, i] = 0;
            for (var j = i + 1; j < articles.Count; j++)
            {
                var distance = ArticleDistance(articles[i], articles[j]);
                matrix[i, j] = distance;
                matrix[j, i] = distance;
            }
        }

        //write out distance matrix to eyeball
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

        return matrix;
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
        var list1 = article1.MajorTopicMeshHeadings;
        var list2 = article2.MajorTopicMeshHeadings;

        if (list1 == null || list2 == null) return MaxDistance;
        if (list1.Count == 0 || list2.Count == 0) return MaxDistance;

        return ArticleListDistance(list1, list2) + ArticleListDistance(list2, list1);
    }

    /// <summary>
    /// Calculate the distance it takes for all of listA to get to any of listB.
    /// </summary>
    private int ArticleListDistance(List<MeshTerm> listA, List<MeshTerm> listB)
    {
        // we need to determine the minimum distance for every descriptor on that article
        // to ANY descriptor on the other article
        var distance = 0;
        foreach (var meshTermA in listA)
        {
            // get the index for
            if (!_matrixKeys.TryGetValue(meshTermA.DescriptorId, out var meshAIndex))
            {
                distance += MaxDistance;
                continue;
            }

            var minDistance = MaxDistance;
            // get the minimum distance to any of the other descriptors
            foreach (var meshTermB in listB)
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