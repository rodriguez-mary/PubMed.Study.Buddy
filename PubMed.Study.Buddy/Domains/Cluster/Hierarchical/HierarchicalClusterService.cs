using System.Text;
using Aglomera.Linkage;
using Aglomera;
using Aglomera.Evaluation.Internal;
using PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;
using PubMed.Study.Buddy.DTOs;
using System.IO;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

/// <summary>
/// Clusters the given articles using a hierarchical clustering algorithm.
/// </summary>
public class HierarchicalClusterService : IClusterService
{
    private readonly ISet<DataPoint> _dataPoints;

    public HierarchicalClusterService(List<Article> articles)
    {
        _dataPoints = GetDataPoints(articles);
    }

    public void GetClusters()
    {
        var metric = new DataPoint(); // Euclidean distance
        var linkage = new AverageLinkage<DataPoint>(metric);
        var clusteringAlg = new AgglomerativeClusteringAlgorithm<DataPoint>(linkage);
        var clustering = clusteringAlg.GetClustering(_dataPoints);

        CentroidFunction<DataPoint> centroidFunc = DataPoint.GetCentroid;
        var criteria =
            new Dictionary<string, IInternalEvaluationCriterion<DataPoint>>
            {
                {"Silhouette coefficient", new SilhouetteCoefficient<DataPoint>(metric)},
                {"Dunn index", new DunnIndex<DataPoint>(metric)},
                {"Davies-Bouldin index", new DaviesBouldinIndex<DataPoint>(metric, centroidFunc)},
                {"Calinski-Harabasz index", new CalinskiHarabaszIndex<DataPoint>(metric, centroidFunc)},
                {"Modified Gamma statistic", new ModifiedGammaStatistic<DataPoint>(metric, centroidFunc)},
                {"Xie-Beni index", new XieBeniIndex<DataPoint>(metric, centroidFunc)},
                {"Within-Between ratio", new WithinBetweenRatio<DataPoint>(metric, centroidFunc)},
                {"I-index", new IIndex<DataPoint>(metric, centroidFunc)},
                {"Xu index", new XuIndex<DataPoint>(metric, centroidFunc)}

                //{"RMSSD", new RootMeanSquareStdDev<DataPoint>(metric, centroidFunc)},
                //{"R-squared", new RSquared<DataPoint>(metric, centroidFunc)},
            };

        foreach (var criterion in criteria)
            GetBestPartition(clustering, criterion.Value, criterion.Key);
    }

    private static void GetBestPartition(
        ClusteringResult<DataPoint> clustering,
        IInternalEvaluationCriterion<DataPoint> criterion, string criterionName)
    {
        // gets coeffs for all cluster-sets
        var evals = clustering.EvaluateClustering(criterion);

        // saves cluster-sets indexes to CSV file
        SaveToCsv(evals, Path.GetFullPath(Path.Combine(@"c:\temp\studybuddy", $"{criterionName}.csv")), criterionName);

        // gets max coeff
        var maxEval = new ClusterSetEvaluation<DataPoint>(null, double.MinValue);
        foreach (var eval in evals)
            if (eval.EvaluationValue > maxEval.EvaluationValue)
                maxEval = eval;

        // prints cluster set info
        Console.WriteLine("======================================");
        Console.WriteLine($"Max {criterionName}: {maxEval.EvaluationValue:0.00}");
        if (maxEval.ClusterSet == null) return;
        Console.WriteLine(
            $"Clusters at distance: {maxEval.ClusterSet.Dissimilarity:0.00} ({maxEval.ClusterSet.Count})");
        foreach (var cluster in maxEval.ClusterSet)
            Console.WriteLine($" - {cluster}");
    }

    private static void SaveToCsv(
        IEnumerable<ClusterSetEvaluation<DataPoint>> evals, string filePath, string criterionName,
        char sepChar = ',')
    {
        using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
        // writes header
        sw.WriteLine($"Num. clusters{sepChar}{criterionName}{sepChar}Cluster-set");

        // writes all key-value-pairs, one per line
        foreach (var eval in evals)
            sw.WriteLine($"{eval.ClusterSet.Count}{sepChar}{eval.EvaluationValue}{sepChar}" +
                         $"{eval.ClusterSet.ToString(false).Replace(sepChar, ';')}");
        sw.Close();
    }

    private static ISet<DataPoint> GetDataPoints(List<Article> articles)
    {
        //get all the possible mesh headings
        var meshTerms = new List<string>();

        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings == null) continue;
            foreach (var meshTerm in article.MajorTopicMeshHeadings.Where(meshTerm => !meshTerms.Contains(meshTerm)))
                meshTerms.Add(meshTerm);
        }

        //add data points for each article where having that mesh heading is a 1 in that data point, otherwise a 0
        var dataPoints = new HashSet<DataPoint>();
        foreach (var article in articles)
        {
            var fields = InitializeArray(meshTerms.Count);

            //set all fields to 1 for all the article's mesh terms
            if (article.MajorTopicMeshHeadings != null)
            {
                foreach (var meshTerm in article.MajorTopicMeshHeadings)
                {
                    var meshTermIndex = meshTerms.IndexOf(meshTerm);
                    if (meshTermIndex > 0 && meshTermIndex < fields.Length)
                        fields[meshTermIndex] = 1;
                }
            }

            dataPoints.Add(new DataPoint(article.Id, fields));
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