using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Jaccard;

public class JaccardSimilarityClusterService : IClusterService
{
    private readonly List<string> _testIds = new() { "35521894", "35481715" };

    public List<ArticleSet> ClusterArticles(List<Article> allArticles)
    {
        var articles = GetArticles(allArticles, false);

        var similarityDict = new Dictionary<string, List<string>>();
        // Calculate Jaccard Similarity between all elements
        for (var i = 0; i < articles.Count; i++)
        {
            var article1 = articles[i];
            for (var j = i + 1; j < articles.Count; j++)
            {
                var article2 = articles[j];
                var similarity = CalculateJaccardSimilarity(article1.MajorTopicMeshHeadings, article2.MajorTopicMeshHeadings);
                if (similarity == 1)
                {
                    article1.MajorTopicMeshHeadings!.Sort();
                    var key = string.Join(";", article1.MajorTopicMeshHeadings.Select(s => s.DescriptorId));
                    if (!similarityDict.ContainsKey(key))
                        similarityDict.Add(key, new List<string>());
                    similarityDict[key].AddRange([article1.Id, article2.Id]);
                }
            }
        }

        using var sw = new StreamWriter(@"c:\temp\studybuddy\jaccard_1.csv", false, Encoding.UTF8);
        sw.WriteLine("similarity,articles");
        foreach (var (key, value) in similarityDict)
        {
            sw.WriteLine($"{key},{string.Join(",", value)}");
        }

        return [];
    }

    private List<Article> GetArticles(List<Article> allArticles, bool useTestIds)
    {
        if (!useTestIds) return allArticles;

        var articles = new List<Article>();
        foreach (var article in allArticles)
        {
            if (!_testIds.Contains(article.Id)) continue;
            articles.Add(article);
        }

        return articles;
    }

    // Function to calculate Jaccard Similarity between two arrays of doubles
    private static double CalculateJaccardSimilarity(List<MeshTerm>? set1, List<MeshTerm>? set2)
    {
        if (set1 == null || set2 == null) return 0;

        // Convert lists to sets
        var set1HashSet = new HashSet<string>(set1.Select(s => s.DescriptorId));
        var set2HashSet = new HashSet<string>(set2.Select(s => s.DescriptorId));

        // Calculate intersection and union sizes
        var intersectionSize = set1HashSet.Intersect(set2HashSet).Count();
        var unionSize = set1HashSet.Union(set2HashSet).Count();

        // Calculate Jaccard Index
        var jaccardIndex = (double)intersectionSize / unionSize;
        return jaccardIndex;
    }
}