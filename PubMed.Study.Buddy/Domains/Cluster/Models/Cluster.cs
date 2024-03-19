using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Models;

public class Cluster
{
    public string Id { get; set; } = string.Empty;

    public List<Article> Articles { get; set; } = new();
}