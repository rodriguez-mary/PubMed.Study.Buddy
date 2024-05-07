using Aglomera;
using Aglomera.Evaluation.Internal;
using Aglomera.Linkage;
using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Agglomerative;

internal class AgglomerativeClusterService : IClusterService
{
    public List<ArticleSet> ClusterArticles(List<Article> articles)
    {
        Console.WriteLine("Clustering...");
        var articleSet = new HashSet<Article>(articles);

        var metric = new ArticleDissimilarityMetric();
        var linkage = new AverageLinkage<Article>(metric);
        var clusteringAlg = new AgglomerativeClusteringAlgorithm<Article>(linkage);
        var clustering = clusteringAlg.GetClustering(articleSet);

        clustering.SaveToCsv(@"c:\temp\studybuddy\clustering.csv");

        Console.WriteLine("Evaluating...");
        // evaluates the clustering according to several criteria
        //CentroidFunction<DataPoint> centroidFunc = DataPoint.GetMedoid;
        //CentroidFunction<Article> centroidFunc = Article.GetCentroid;
        var criteria =
            new Dictionary<string, IInternalEvaluationCriterion<Article>>
            {
                {"Silhouette coefficient", new SilhouetteCoefficient<Article>(metric)},
                {"Dunn index", new DunnIndex<Article>(metric)},
                //{"Davies-Bouldin index", new DaviesBouldinIndex<Article>(metric, centroidFunc)},
                //{"Calinski-Harabasz index", new CalinskiHarabaszIndex<Article>(metric, centroidFunc)},
               // {"Modified Gamma statistic", new ModifiedGammaStatistic<Article>(metric, centroidFunc)},
               // {"Xie-Beni index", new XieBeniIndex<Article>(metric, centroidFunc)},
               // {"Within-Between ratio", new WithinBetweenRatio<Article>(metric, centroidFunc)},
               // {"I-index", new IIndex<Article>(metric, centroidFunc)},
               // {"Xu index", new XuIndex<Article>(metric, centroidFunc)}

                //{"RMSSD", new RootMeanSquareStdDev<DataPoint>(metric, centroidFunc)},
                //{"R-squared", new RSquared<DataPoint>(metric, centroidFunc)},
            };

        foreach (var criterion in criteria)
            GetBestPartition(clustering, criterion.Value, criterion.Key);

        return [];
    }

    private static void GetBestPartition(ClusteringResult<Article> clustering, IInternalEvaluationCriterion<Article> criterion, string criterionName)
    {
        // gets coeffs for all cluster-sets
        var evals = clustering.EvaluateClustering(criterion);

        // saves cluster-sets indexes to CSV file
        SaveToCsv(evals, Path.GetFullPath(Path.Combine(@"c:\temp\studybuddy\", $"{criterionName}.csv")), criterionName);

        // gets max coeff
        var maxEval = new ClusterSetEvaluation<Article>(null, double.MinValue);
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

    private static void SaveToCsv(IEnumerable<ClusterSetEvaluation<Article>> evals, string filePath, string criterionName, char sepChar = ',')
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
}