using PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

/// <summary>
/// Groups articles BY their mesh terms. Hierarchically clusters them based on the mesh term taxonomy.
/// </summary>
public class HierarchicalClusterService : IClusterService
{
    private const int MinClusterSize = 10;     // any selected cluster should have a minimum of this number of articles
    private const int MinLineageDistance = 3;  // any selected cluster should have a minimum of these levels of lineage

    private readonly Dictionary<string, MeshTerm> _meshTermsByTreeNumber = [];
    private readonly IReadOnlyDictionary<string, MeshTerm> _meshTermsById;

    #region constructor

    public HierarchicalClusterService(IReadOnlyDictionary<string, MeshTerm> meshTerms)
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

    #endregion constructor

    public List<ArticleSet> ClusterArticles(List<Article> articles)
    {
        // get the counts of articles relevent to each location in the mesh term taxonomy
        var taxonomyCounts = Utilities.GetTaxonomyCounts(articles);

        // group all articles by their mesh terms
        var articlesByMeshTermId = ArticlesByMeshTermId(taxonomyCounts, articles);

        // remove articles if they're overrepresented in clusters where they're unnecessary
        var clusters = DeduplicateClusters(articlesByMeshTermId);

        // create the article sets from the clusters
        var clusteredList = new List<ArticleSet>();
        foreach (var cluster in clusters)
        {
            if (cluster.Value.Count <= 0) continue;

            clusteredList.Add(new ArticleSet()
            {
                Articles = cluster.Value,
                Name = _meshTermsById[cluster.Key].DescriptorName
            });
        }

        return clusteredList;
    }

    /// <summary>
    /// Gets a list of all articles by their best match mesh terms
    /// </summary>
    private Dictionary<string, List<Article>> ArticlesByMeshTermId(Dictionary<string, TaxonomyCount> taxonomyCounts, List<Article> articles)
    {
        var articlesByMeshTermId = new Dictionary<string, List<Article>>();
        foreach (var article in articles)
        {
            var bestTreeNumbers = BestFitTreeNumbers(taxonomyCounts, article);

            foreach (var number in bestTreeNumbers)
            {
                if (!_meshTermsByTreeNumber.ContainsKey(number))
                {
                    // write an error then move on
                    Console.WriteLine($"Could not find mesh term for tree number {number}");
                    continue;
                }

                var meshTerm = _meshTermsByTreeNumber[number];

                if (!articlesByMeshTermId.ContainsKey(meshTerm.DescriptorId))
                    articlesByMeshTermId.Add(meshTerm.DescriptorId, new List<Article>());

                articlesByMeshTermId[meshTerm.DescriptorId].Add(article);
            }
        }

        return articlesByMeshTermId;
    }

    /*
     *  for every article
     *   - cluster it into the smallest tree number that has a count of > Min cluster count
     *   - if there are ties, prefer the one that is at the most specific level (most dots)
     *   - if still tied, add all that are tied
     *
     *   - if there is nothing that exceeds 3, add the biggest one that has at least 3 level of specificity
     */

    private List<string> BestFitTreeNumbers(Dictionary<string, TaxonomyCount> taxonomyCounts, Article article)
    {
        var lineage = Utilities.ArticleLineage(article);

        var articleTaxonomyCounts = taxonomyCounts.Where(x => lineage.Contains(x.Key));

        var validSize = new List<string>();
        var descendingComparer = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var validSpecificity = new SortedList<int, List<string>>(descendingComparer);  //needs to be a sorted list so we can select the first value

        foreach (var (k, v) in articleTaxonomyCounts)
        {
            if (v.Count >= MinClusterSize)
            {
                validSize.Add(k);
            }

            var depth = k.Count(x => x == '.') + 1;  // todo centralize depth count
            if (depth >= MinLineageDistance)
            {
                if (!validSpecificity.ContainsKey(v.Count))
                    validSpecificity.Add(v.Count, new List<string>());
                validSpecificity[v.Count].Add(k);
            }
        }

        // do we prefer more specific or bigger?
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

    /// <summary>
    /// Analyze clusters for duplicates and remove duplicated articles from "worse" clusters.
    /// The best clusters are smaller and more specific.
    /// </summary>
    private Dictionary<string, List<Article>> DeduplicateClusters(Dictionary<string, List<Article>> articlesByMeshTermId)
    {
        var deduplicatedClusters = articlesByMeshTermId.ToDictionary(entry => entry.Key, entry => new List<Article>(entry.Value));
        var groupingsPerArticle = new Dictionary<string, List<string>>();

        foreach (var (meshTermId, articles) in articlesByMeshTermId)
        {
            foreach (var article in articles)
            {
                if (!groupingsPerArticle.ContainsKey(article.Id))
                    groupingsPerArticle.Add(article.Id, new List<string>());

                groupingsPerArticle[article.Id].Add(meshTermId);
            }
        }

        foreach (var (articleId, meshTerms) in groupingsPerArticle)
        {
            var extraneousTerms = ExtraneousMeshTerms(meshTerms, articlesByMeshTermId);

            foreach (var extraneousTerm in extraneousTerms)
            {
                // kinda janked. should extract the remove into it's own function
                // but this will work for now because of the IEquatable override in Article
                var article = new Article { Id = articleId };

                if (!deduplicatedClusters.ContainsKey(extraneousTerm) || !deduplicatedClusters[extraneousTerm].Contains(article)) continue;

                deduplicatedClusters[extraneousTerm].Remove(article);
            }
        }

        return deduplicatedClusters;
    }

    // return unnecessary mesh terms
    // these are any terms where there are more specific terms
    // or, for equal specificity, larger article sets associated with the terms
    private List<string> ExtraneousMeshTerms(List<string> meshTermIds, Dictionary<string, List<Article>> articlesByMeshTermId)
    {
        var extraneousTerms = new List<string>();
        if (meshTermIds.Count <= 1) return extraneousTerms;

        var maxDepth = 0;
        var maxDepthTerms = new List<string>();

        foreach (var meshTermId in meshTermIds)
        {
            var depth = MeshTermDepth(meshTermId);
            if (depth > maxDepth)
            {
                // set the new max depth
                maxDepth = depth;

                // add the old max depth terms to the extraneous terms
                extraneousTerms.AddRange(maxDepthTerms);

                // start tracking the new max depth terms
                maxDepthTerms.Clear();
                maxDepthTerms.Add(meshTermId);
            }
            else if (depth == maxDepth)
            {
                maxDepthTerms.Add(meshTermId);
            }
            else
            {
                extraneousTerms.Add(meshTermId);
            }
        }

        if (maxDepthTerms.Count <= 1) return extraneousTerms;

        var smallestSize = articlesByMeshTermId[maxDepthTerms.First()].Count;
        var extraneousTermsForSize = new List<string>();
        var smallestSizeTerms = new List<string>();
        foreach (var term in maxDepthTerms)
        {
            var size = articlesByMeshTermId[term].Count;
            if (size == 1)
            {
                extraneousTermsForSize.Add(term);
            }
            else if (size < smallestSize)
            {
                smallestSize = size;

                extraneousTermsForSize.AddRange(smallestSizeTerms);

                smallestSizeTerms.Clear();
                smallestSizeTerms.Add(term);
            }
            else if (size == smallestSize)
            {
                smallestSizeTerms.Add(term);
            }
            else
            {
                extraneousTermsForSize.Add(term);
            }
        }

        if (smallestSizeTerms.Count < 1) return extraneousTerms;

        extraneousTerms.AddRange(extraneousTermsForSize);
        return extraneousTerms;
    }

    private int MeshTermDepth(string meshTermId)
    {
        if (!_meshTermsById.ContainsKey(meshTermId)) return 0;

        var meshTerm = _meshTermsById[meshTermId];

        var maxDepth = 0;

        foreach (var number in meshTerm.TreeNumbers)
        {
            var depth = number.Split(".").Length;
            if (depth > maxDepth) maxDepth = depth;
        }

        return maxDepth;
    }
}