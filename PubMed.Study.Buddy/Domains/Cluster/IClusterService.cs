using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster;

public interface IClusterService
{
    public List<Models.Cluster> GetClusters(List<Article> articles);
}