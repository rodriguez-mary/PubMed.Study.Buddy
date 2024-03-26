using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.IterativeMesh;

public class IterativeMeshClusteringService : IClusterService
{
    public List<Models.Cluster> GetClusters(List<Article> articles)
    {
        var meshTermsDictionary = new Dictionary<string, List<string>>();

        foreach (var article in articles)
        {
            var key = string.Empty;
            if (article.MajorTopicMeshHeadings != null)
            {
                var ds = article.MajorTopicMeshHeadings.Select(meshHeading => $"{meshHeading.DescriptorId}:{string.Join(";", meshHeading.TreeNumber)}").ToList();
                key = string.Join("~", ds);
            }

            if (!meshTermsDictionary.ContainsKey(key)) meshTermsDictionary.Add(key, []);

            meshTermsDictionary[key].Add(article.Id);
        }

        using var sw = new StreamWriter(@"c:\temp\studybuddy\iterative_mesh.csv", false, Encoding.UTF8);
        sw.WriteLine("mesh terms,articles");
        foreach (var (key, value) in meshTermsDictionary)
        {
            sw.WriteLine($"{key},{string.Join(",", value)}");
        }

        return new List<Models.Cluster>();
    }
}