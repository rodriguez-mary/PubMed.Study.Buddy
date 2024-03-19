using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Jaccard;

public class JaccardSimilarityClusterService : IClusterService
{
    private readonly List<string> _testIds = new() { "35521894", "35481715" };

    public List<Models.Cluster> GetClusters(List<Article> allArticles)
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
                    article1.MajorTopicMeshHeadings.Sort();
                    var key = string.Join(";", article1.MajorTopicMeshHeadings.Select(s => s.Replace(",", "")));
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

        return new List<Models.Cluster>();
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
    private static double CalculateJaccardSimilarity(List<string> set1, List<string> set2)
    {
        // Convert lists to sets
        HashSet<string> set1HashSet = new HashSet<string>(set1);
        HashSet<string> set2HashSet = new HashSet<string>(set2);

        // Calculate intersection and union sizes
        int intersectionSize = set1HashSet.Intersect(set2HashSet).Count();
        int unionSize = set1HashSet.Union(set2HashSet).Count();

        // Calculate Jaccard Index
        double jaccardIndex = (double)intersectionSize / unionSize;
        return jaccardIndex;
    }
}