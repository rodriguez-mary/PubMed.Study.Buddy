using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

public class HierarchicalClusterService(IReadOnlyDictionary<string, MeshTerm> meshTerms) : IClusterService
{
    //this is the size at which we deem a cluster sufficiently large
    private const int MinClusterSize = 10;

    // this defines how many unconnected terms we allow in the mesh terms. this allows for articles to match even if all their terms don't connect to each other
    private const double MaxUnconnectedPercentage = .25;

    private const int MaxDistance = 1000;
    private Dictionary<string, int> _matrixKeys = [];
    private int[,] _distanceMatrix = new int[0, 0];

    private readonly Utilities _utilities = new(meshTerms);

    /// <summary>
    /// Create a preprocessed distance matrix for all the mesh terms from the articles being clustered.
    /// </summary>
    private void Initialize(IEnumerable<Article> articles)
    {
        var meshTermDict = new Dictionary<string, MeshTerm>();
        foreach (var meshHeading in articles.Where(article => article.MajorTopicMeshHeadings != null).SelectMany(article => article.MajorTopicMeshHeadings!))
        {
            meshTermDict.TryAdd(meshHeading.DescriptorId, meshHeading);
        }

        var meshTerms = meshTermDict.Values.ToList();

        _matrixKeys = [];

        var index = 0;
        foreach (var meshTerm in meshTerms)
        {
            _matrixKeys[meshTerm.DescriptorId] = index;
            index++;
        }

        _distanceMatrix = Utilities.CreateDistanceMatrix(meshTerms);
    }

    public List<ArticleSet> ClusterArticles(List<Article> baseArticles)
    {
        var articles = GetArticles(baseArticles, []);// ["38318920", "31016746", "37116877", "35137433", "34643954", "34590311", "33978435", "33587301", "33539210", "32974903", "32597736"]);
        Initialize(articles);

        var matrix = GetArticleDistanceMatrix(articles);
        var clusters = ClusterArticles(articles, matrix);

        // deduplicate the clusters
        var deduplicatedClusters = new List<ArticleSet>();
        foreach (var cluster in clusters)
        {
            if (deduplicatedClusters.Contains(cluster)) continue;
            deduplicatedClusters.Add(cluster);
        }

        // cluster article sets that have the same name
        var clustersByName = new Dictionary<string, ArticleSet>();
        foreach (var cluster in deduplicatedClusters)
        {
            if (!clustersByName.ContainsKey(cluster.Name))
                clustersByName.Add(cluster.Name, new ArticleSet { Name = cluster.Name });

            clustersByName[cluster.Name].Articles.AddRange(cluster.Articles);
        }
        var clustersList = clustersByName.Values.ToList();

        using var sw = new StreamWriter(@"c:\temp\studybuddy\hierarchical.csv", false, Encoding.UTF8);
        sw.WriteLine("articles,cluster name,articles");
        foreach (var cluster in clustersList)
        {
            var a = cluster.Articles;
            sw.WriteLine($"{a.Count},{cluster.Name.Replace(",", "")},{string.Join(",", a)}");
        }

        return clustersList;
    }

    private List<ArticleSet> ClusterArticles(List<Article> articles, int[,] matrix)
    {
        var maxMeshTerms = 0;
        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings?.Count > maxMeshTerms)
                maxMeshTerms = article.MajorTopicMeshHeadings.Count;
        }

        var clusterDict = new Dictionary<int, ArticleSet>();
        for (var i = 0; i < matrix.GetLength(0); i++)
            clusterDict.Add(i, new ArticleSet());

        // basically, we have our list of articles that need to be clustered (articleKeys)
        // we iteratively loop through all the articles and cluster them by increasing distances
        // if they meet the requirement of min size
        // then we pull them from the list that needs to be clustered
        var colLength = matrix.GetLength(1);
        var currentMaxDistance = 0;
        while (currentMaxDistance <= maxMeshTerms * MaxDistance)
        {
            foreach (var (pos, cluster) in clusterDict)
            {
                if (cluster.Articles.Count >= MinClusterSize) continue;

                var posMaxDistance = Math.Round(articles[pos].MajorTopicMeshHeadings?.Count ?? 0 * MaxUnconnectedPercentage) * MaxDistance;
                if (currentMaxDistance > posMaxDistance) continue;

                for (var i = 0; i < colLength; i++)
                {
                    // we require at least 50% of the terms be connected
                    // we know that there are unconnected terms for every 1k in the distance
                    // so, if we're in unconnected term territory then we need to make sure it's less than the allowed percentage
                    var iMaxDistance = Math.Round(articles[i].MajorTopicMeshHeadings?.Count ?? 0 * MaxUnconnectedPercentage) * MaxDistance;
                    if (currentMaxDistance > iMaxDistance) continue;

                    var distance = matrix[pos, i];
                    if (distance == currentMaxDistance)  // == rather than <= to avoid adding the same article multiple times
                    {
                        cluster.Articles.Add(articles[i]);
                        cluster.Distance = distance;
                    }
                }
            }

            currentMaxDistance++;
        }

        //write out distance matrix to eyeball
        using var sw = new StreamWriter(@"c:\temp\studybuddy\distance_matrix.csv", false, Encoding.UTF8);

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
                sw.Write($",{x}");
            }

            sw.WriteLine();
        }

        var clusters = clusterDict.Values.ToList();

        foreach (var cluster in clusters)
        {
            cluster.Name = _utilities.DetermineClusterName(cluster.Articles);
        }

        return clusters;
    }

    private List<Article> GetArticles(List<Article> baseArticles, List<string> ids)
    {
        if (ids.Count == 0) return baseArticles;

        var articles = new List<Article>();
        foreach (var art in baseArticles)
        {
            if (ids.Contains(art.Id))
                articles.Add(art);
        }

        return articles;
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

        return matrix;
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
}