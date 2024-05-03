using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

public class HierarchicalByMeshTermClusterService : IClusterService
{
    //this is the size at which we deem a cluster sufficiently large
    private const int MinClusterSize = 10;     //any selected clustre should have a minimum of this number of articles
    private const int MinLineageDistance = 3;  //any selected cluster should have a minimum of these levels of lineage

    private readonly Dictionary<string, MeshTerm> _meshTermsByTreeNumber = new();
    private readonly IReadOnlyDictionary<string, MeshTerm> _meshTermsById;

    public HierarchicalByMeshTermClusterService(IReadOnlyDictionary<string, MeshTerm> meshTerms)
    {
        _meshTermsById = meshTerms;
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
        var hierachyCount = BuildHierarchyCount(articles);

        return ClusterArticlesByHierarchy(hierachyCount, articles);

    }


    private List<ArticleSet> ClusterArticlesByHierarchy(Dictionary<string, HierarchyCount> hierarchyCount, List<Article> articles)
    {
        var articlesByMeshTermId = new Dictionary<string, List<Article>>();
        foreach (var article in articles)
        {
            var bestTreeNumbers = BestMatchTreeNumbers(hierarchyCount, article);

            foreach (var number in bestTreeNumbers)
            {

                if (!_meshTermsByTreeNumber.ContainsKey(number))
                {
                    // write an error then move on
                    continue;
                }

                var meshTerm = _meshTermsByTreeNumber[number];

                if (!articlesByMeshTermId.ContainsKey(meshTerm.DescriptorId))
                    articlesByMeshTermId.Add(meshTerm.DescriptorId, new List<Article>());

                articlesByMeshTermId[meshTerm.DescriptorId].Add(article);
            }
        }

        var clusteredList = new List<ArticleSet>();
        foreach (var group in articlesByMeshTermId)
        {
            clusteredList.Add(new ArticleSet()
            {
                Articles = group.Value,
                Name = _meshTermsById[group.Key].DescriptorName
            });
        }

        return clusteredList;
    }


    /*
     *  for every article
     *   - cluster it into the smallest tree number that has a count of > Min cluster count
     *   - if there are ties, prefer the one that is at the most specific level (most dots)
     *   - if still tied, add all that are tied
     *   
     *   - if there is nothing that exceeds 3, add the biggest one that has at least 3 level of specificity
     */
    private List<string> BestMatchTreeNumbers(Dictionary<string, HierarchyCount> hierarchyCount, Article article)
    {
        var lineage = ArticleLineage(article);

        var articleHierarchyCounts = hierarchyCount.Where(x => lineage.Contains(x.Key));

        var validSize = new List<string>();
        var descendingComparer = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var validSpecificity = new SortedList<int, List<string>>(descendingComparer);
        foreach (var (k,v) in articleHierarchyCounts)
        {
            if (v.Count >= MinClusterSize)
            {
                validSize.Add(k);
            }
            if (k.Count(x => x == '.') >= MinLineageDistance)
            {
                if (!validSpecificity.ContainsKey(v.Count))
                    validSpecificity.Add(v.Count, new List<string>());
                validSpecificity[v.Count].Add(k);
            }
        }

        if (validSize.Count > 0)
            return SelectMostSpecific(validSize);

        if (validSpecificity.Count > 0)
            return validSpecificity.First().Value;

        //todo throw error - what kind of bullshit is this?
        return lineage;
    }

    private List<string> SelectMostSpecific(List<string> validSize)
    {
        var mostSpecific = new List<string>();

        if (validSize.Count <= 0) return mostSpecific; //todo: maybe throw error?

        if (validSize.Count == 1)
        {
            mostSpecific.Add(validSize.First());
            return mostSpecific;
        }

        var maxLineage = 0;
        foreach (var item in validSize)
        {
            var lineageCount = item.Count(x => x == '.');
            if (lineageCount > maxLineage)
            {
                maxLineage = lineageCount;

                mostSpecific.Clear();
                mostSpecific.Add(item);
            }
            else if (lineageCount == maxLineage)
            {
                mostSpecific.Add(item);
            }

        }

        return mostSpecific;

    }

    // Creates a count of all the articles that could be rolled up to every particular hierarchy level
    private Dictionary<string, HierarchyCount> BuildHierarchyCount(List<Article> articles)
    {
        var hierarchyCount = new Dictionary<string, HierarchyCount>();

        foreach (var article in articles)
        {
            var lineage = ArticleLineage(article);

            foreach (var number in lineage)
            {
                if (!hierarchyCount.ContainsKey(number))
                    hierarchyCount.Add(number, new HierarchyCount());

                hierarchyCount[number].ArticleIds.Add(article.Id);
            }
        }

        return hierarchyCount;
    }

    private List<string> ArticleLineage(Article article)
    {
        var lineage = new List<string>();

        if (article.MajorTopicMeshHeadings == null) return lineage;

        foreach (var meshHeading in article.MajorTopicMeshHeadings)
        {
            foreach (var treeNumber in meshHeading.TreeNumbers)
            {
                var splitNumbers = treeNumber.Split(".");
                for (var i = 0; i < splitNumbers.Length; i++)
                {
                    var number = string.Join(".", splitNumbers[..i]);
                    lineage.Add(number);
                }
            }
        }

        return lineage;
    }


    private class HierarchyCount
    {
        // ease of access for sorting
       public int Count { get { return ArticleIds.Count; } }

        public List<string> ArticleIds = [];
    }
}