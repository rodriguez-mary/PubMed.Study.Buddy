namespace PubMed.Study.Buddy.Domains.ImpactScoring;

public interface IImpactScoringService
{
    Task<double> GetImpactScore(DTOs.Article article);
}