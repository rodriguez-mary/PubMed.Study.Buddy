using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster;

public interface IClusterService
{
    public List<ArticleSet> ClusterArticles(List<Article> articles);
}