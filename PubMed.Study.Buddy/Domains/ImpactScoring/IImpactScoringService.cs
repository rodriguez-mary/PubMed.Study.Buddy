namespace PubMed.Study.Buddy.Domains.ImpactScoring;

public interface IImpactScoringService
{
    Task Initialize(InitializationData data);

    Task<double> GetImpactScore(DTOs.Article article);
}