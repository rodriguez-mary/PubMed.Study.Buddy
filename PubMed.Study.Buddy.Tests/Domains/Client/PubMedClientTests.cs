using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PubMed.Study.Buddy.Domains.Client;
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
            GivenIHaveOutputServiceMock().Object));
    }

    #region FindArticles

    [TestMethod]
    public async Task FindArticles_CallsReturnsArticles()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_2" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveOutputServiceMock().Object);

        var result = await client.FindArticles([new ArticleFilter()]);

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
            scoringServiceMock.Object, GivenIHaveOutputServiceMock().Object);

        _ = await client.FindArticles([new ArticleFilter()]);

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
            scoringServiceMock.Object, GivenIHaveOutputServiceMock().Object);

        var result = await client.FindArticles([new ArticleFilter()]);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    #endregion FindArticles

    #region GenerateContent

    [TestMethod]
    public async Task GenerateContent_CallsOutputService()
    {
        var outputServiceMock = GivenIHaveOutputServiceMock();
        var client = new PubMedClient(NullLogger<PubMedClient>.Instance,
            GivenIHaveAPubMedSearchServiceMock([]).Object,
            GivenIHaveAnImpactScoringServiceMock().Object,
            outputServiceMock.Object);

        await client.GenerateContent([]);

        outputServiceMock.Verify(o => o.GenerateArticleList(It.IsAny<List<Article>>()), Times.Once);
    }

    #endregion GenerateContent

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

    #endregion steps
}