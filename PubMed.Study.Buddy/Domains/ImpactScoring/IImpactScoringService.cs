namespace PubMed.Study.Buddy.Domains.ImpactScoring;

internal interface IImpactScoringService
{
    double GetImpactScore(DTOs.Article article);
}