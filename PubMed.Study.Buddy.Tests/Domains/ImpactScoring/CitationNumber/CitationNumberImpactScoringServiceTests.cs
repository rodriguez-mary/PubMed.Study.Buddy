using PubMed.Study.Buddy.Domains.ImpactScoring.CitationNumber;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Tests.Domains.ImpactScoring.CitationNumber;

[TestClass]
public class CitationNumberImpactScoringServiceTests
{
    [TestMethod]
    public async Task GetImpactScore_ReturnsCitedByCount()
    {
        var citedByList = new List<string> { "val1" };
        var result = await new CitationNumberImpactScoringService().GetImpactScore(new Article
        {
            CitedBy = citedByList
        });

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task GetImpactScore_ReturnsZeroIfCitedByNull()
    {
        var result = await new CitationNumberImpactScoringService().GetImpactScore(new Article
        {
            CitedBy = null
        });

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result);
    }
}