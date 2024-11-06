using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PubMed.Study.Buddy.Domains.Client;
using PubMed.Study.Buddy.Domains.FlashCard.Database;
using PubMed.Study.Buddy.Domains.FlashCard.Service;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Tests.Domains.Client;

[TestClass]
public class PubMedClientTests
{
    [TestMethod]
    public void ConstructorSucceeds()
    {
        Assert.IsNotNull(new PubMedClient(NullLogger<PubMedClient>.Instance,
            GivenIHaveAPubMedSearchServiceMock([]).Object,
            GivenIHaveAnImpactScoringServiceMock().Object,
            GivenIHaveAFlashCardServiceMock().Object));
    }

    #region FindArticles

    [TestMethod]
    public async Task FindArticles_CallsReturnsArticles()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_2" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock().Object);

        var result = await client.GetArticles([new ArticleFilter()]);

        Assert.IsNotNull(result);
        Assert.AreEqual(articlesToReturn.Count, result.Count);
    }

    [TestMethod]
    public async Task FindArticles_CallsSearchServiceAndScoreService()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_2" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock().Object);

        _ = await client.GetArticles([new ArticleFilter()]);

        searchServiceMock.Verify(s => s.FindArticles(It.IsAny<ArticleFilter>()), Times.Once);   //called once per filter
        scoringServiceMock.Verify(s => s.GetImpactScore(It.IsAny<Article>()), Times.Exactly(articlesToReturn.Count));  //called once per article returned
    }

    [TestMethod]
    public async Task FindArticles_DeDuplicatesArticles()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_1" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock().Object);

        var result = await client.GetArticles([new ArticleFilter()]);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    #endregion FindArticles

    #region steps

    private static Mock<IPubMedSearchService> GivenIHaveAPubMedSearchServiceMock(List<Article> articlesToReturn)
    {
        var mock = new Mock<IPubMedSearchService>();
        mock.Setup(m => m.FindArticles(It.IsAny<ArticleFilter>()))
            .ReturnsAsync(articlesToReturn);

        return mock;
    }

    private static Mock<IImpactScoringService> GivenIHaveAnImpactScoringServiceMock()
    {
        return new Mock<IImpactScoringService>();
    }

    private static Mock<IOutputService> GivenIHaveOutputServiceMock()
    {
        return new Mock<IOutputService>();
    }

    private static Mock<IFlashCardService> GivenIHaveAFlashCardServiceMock()
    {
        return new Mock<IFlashCardService>();
    }

    private static Mock<IFlashCardDatabase> GivenIHaveAFlashCardDatabaseMock()
    {
        return new Mock<IFlashCardDatabase>();
    }

    #endregion steps
}