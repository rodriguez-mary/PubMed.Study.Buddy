using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.ImpactScoring;

public class InitializationData
{
    public List<Article> ArticlesToScore { get; set; } = new();
}