using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.ImpactScoring.CitationNumber;

public class CitationNumberImpactScoringService : IImpactScoringService
{
    public Task Initialize(InitializationData _)
    {
        return Task.CompletedTask;
    }

    public Task<double> GetImpactScore(Article article)
    {
        return Task.FromResult<double>(article.CitedBy?.Count ?? 0);
    }
}