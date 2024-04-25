using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

public class HierarchicalByMeshTermClusterService : IClusterService
{
    //this is the size at which we deem a cluster sufficiently large
    private const int MinClusterSize = 10;

    private readonly Dictionary<string, MeshTerm> _meshTermsByTreeNumber = new();

    public HierarchicalByMeshTermClusterService(IReadOnlyDictionary<string, MeshTerm> meshTerms)
    {
        foreach (var meshTerm in meshTerms.Values)
        {
            foreach (var treeNumber in meshTerm.TreeNumbers)
            {
                _meshTermsByTreeNumber.TryAdd(treeNumber, meshTerm);
            }
        }
    }

    public List<ArticleSet> ClusterArticles(List<Article> articles)
    {
        var treeNumberCounts = CreateTreeNumberCount(articles);


        throw new NotImplementedException();
    }

    /// <summary>
    /// Determine how many articles could be rolled into an individual tree number.
    /// TODO: maybe also track distance/the amount of roll-up?
    /// </summary>
    private static Dictionary<string, int> CreateTreeNumberCount(List<Article> articles)
    {
        var treeNumberCounts = new Dictionary<string, int>();

        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings == null) continue;

            foreach (var meshTerm in article.MajorTopicMeshHeadings)
            {
                foreach (var treeNumber in meshTerm.TreeNumbers)
                {
                    // add an entry for the entire lineage
                    var numbers = treeNumber.Split(".");
                    for (var i = 0; i < numbers.Length; i++)
                    {
                        var number = string.Join(".", numbers[..i]);
                        if (!treeNumberCounts.TryAdd(number, 1))
                            treeNumberCounts[number]++;
                    }
                }

            }
        }

        return treeNumberCounts;
    }

    /// <summary>
    /// For an article, get the shortest distance mesh terms that meet the min size requirement
    /// </summary>
    private static List<string> GetTreeNumbers(Dictionary<string, int>  treeNumberCount, Article article)
    {
        var treeNumbers = new List<string>();
        if (article.MajorTopicMeshHeadings == null) return treeNumbers;

        foreach (var meshTerm in article.MajorTopicMeshHeadings)
        {
            foreach (var treeNumber in meshTerm.TreeNumbers)
            {
                var numbers = treeNumber.Split(".");
                for (var i = numbers.Length; i-- > 0;)
                {
                    var number = string.Join(".", numbers[..i]);
                    if (!treeNumberCount.ContainsKey(number)) continue;


                }
        }
        }

        //if we have a tie, then we pick the biggest group
        //if we still have a tie, then we pick the most specific
        //if we still have a tie, return the rest

        return treeNumbers;
    }

    private Dictionary<string, Article> CreateArticleIndex(List<Article> articles)
    {
        var index = new Dictionary<string, Article>();

        foreach (var article in articles)
        {
            index.TryAdd(article.Id, article);
        }

        return index;
    }

    private Dictionary<string, List<Article>> CreateInitialBuckets(List<Article> articles)
    {
        var articlesByMeshTerm = new Dictionary<string, List<Article>>();

        //bucket each article by its mesh terms
        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings == null) continue;

            foreach (var meshTerm in article.MajorTopicMeshHeadings)
            {
                if (!articlesByMeshTerm.ContainsKey(meshTerm.DescriptorId))
                    articlesByMeshTerm.Add(meshTerm.DescriptorId, []);

                articlesByMeshTerm[meshTerm.DescriptorId].Add(article);
            }

        }

        return articlesByMeshTerm;
    }
}