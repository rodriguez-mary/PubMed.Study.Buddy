using PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;
using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Jaccard;

public class JaccardSimilarityClusterService
{
    private readonly List<DataPoint> _dataPoints;

    private readonly List<Article> _articles;

    private readonly List<DataPoint> _testDataPoints = new();
    private readonly List<string> _testIds = new() { "35521894", "35481715", "34085305" };

    private List<string> _meshTerms = new();

    public JaccardSimilarityClusterService(List<Article> articles)
    {
        //_dataPoints = GetDataPoints(articles);
        _articles = articles;
    }

    public void GetClusters()
    {
        var similarityDict = new Dictionary<string, List<string>>();
        // Calculate Jaccard Similarity between all elements
        for (var i = 0; i < _articles.Count; i++)
        {
            var article1 = _articles[i];
            for (var j = i + 1; j < _articles.Count; j++)
            {
                var article2 = _articles[j];
                var similarity =
                    CalculateJaccardSimilarity(article1.MajorTopicMeshHeadings, article2.MajorTopicMeshHeadings);
                if (similarity >= .75)
                {
                    if (!similarityDict.ContainsKey(article1.Id))
                        similarityDict.Add(article1.Id, new List<string>());
                    similarityDict[article1.Id].Add(article2.Id);
                }
            }
        }

        using var sw = new StreamWriter(@"c:\temp\studybuddy\jaccard.csv", false, Encoding.UTF8);
        sw.WriteLine("similarity,articles");
        foreach (var (key, value) in similarityDict)
        {
            sw.WriteLine($"{key},{string.Join(",", value)}");
        }
    }

    private void WriteOutDataPoints()
    {
        using var sw = new StreamWriter(@"c:\temp\studybuddy\datapoints.csv", false, Encoding.UTF8);

        var meshTerms = _meshTerms.Select(term => term.Replace(",", "")).ToList();

        var meshTermsString = string.Join(",", meshTerms);
        sw.WriteLine($"id,{meshTermsString}");
        foreach (var dataPoint in _dataPoints)
        {
            sw.WriteLine($"{dataPoint.ID},{string.Join(",", dataPoint.Value)}");
        }
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

    private List<DataPoint> GetDataPoints(List<Article> articles)
    {
        //get all the possible mesh headings

        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings == null) continue;
            foreach (var meshTerm in article.MajorTopicMeshHeadings.Where(meshTerm => !_meshTerms.Contains(meshTerm)))
                _meshTerms.Add(meshTerm);
        }

        //add data points for each article where having that mesh heading is a 1 in that data point, otherwise a 0
        var dataPoints = new List<DataPoint>();
        foreach (var article in articles)
        {
            var fields = InitializeArray(_meshTerms.Count);

            //set all fields to 1 for all the article's mesh terms
            if (article.MajorTopicMeshHeadings != null)
            {
                foreach (var meshTerm in article.MajorTopicMeshHeadings)
                {
                    var meshTermIndex = _meshTerms.IndexOf(meshTerm);
                    if (meshTermIndex > 0 && meshTermIndex < fields.Length)
                        fields[meshTermIndex] = 1;
                }
            }

            var dataPoint = new DataPoint(article.Id, fields);
            if (_testIds.Contains(article.Id))
                _testDataPoints.Add(dataPoint);

            dataPoints.Add(dataPoint);
        }

        return dataPoints;
    }

    private static double[] InitializeArray(int size)
    {
        var array = new double[size];
        for (var i = 0; i < array.Length; i++)
            array[i] = 0;

        return array;
    }
}