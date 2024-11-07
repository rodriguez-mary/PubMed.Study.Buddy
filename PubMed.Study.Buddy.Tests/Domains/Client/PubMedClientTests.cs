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
            GivenIHaveAPubMedSearchServiceMock(null, null).Object,
            GivenIHaveAnImpactScoringServiceMock().Object,
            GivenIHaveAFlashCardServiceMock(null).Object));
    }

    #region FindArticles

    [TestMethod]
    public async Task FindArticles_CallsReturnsArticles()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_2" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn, null);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock(null).Object);

        var result = await client.GetArticles([new ArticleFilter()]);

        Assert.IsNotNull(result);
        Assert.AreEqual(articlesToReturn.Count, result.Count);
    }

    [TestMethod]
    public async Task FindArticles_CallsSearchServiceAndScoreService()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_2" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn, null);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock(null).Object);

        _ = await client.GetArticles([new ArticleFilter()]);

        searchServiceMock.Verify(s => s.FindArticles(It.IsAny<ArticleFilter>()), Times.Once);   //called once per filter
        scoringServiceMock.Verify(s => s.GetImpactScore(It.IsAny<Article>()), Times.Exactly(articlesToReturn.Count));  //called once per article returned
    }

    [TestMethod]
    public async Task FindArticles_DeDuplicatesArticles()
    {
        var articlesToReturn = new List<Article> { new() { Id = "article_1" }, new() { Id = "article_1" } };

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(articlesToReturn, null);
        var scoringServiceMock = GivenIHaveAnImpactScoringServiceMock();

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            scoringServiceMock.Object, GivenIHaveAFlashCardServiceMock(null).Object);

        var result = await client.GetArticles([new ArticleFilter()]);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    #endregion FindArticles

    #region GetMeshTerms

    [TestMethod]
    public async Task GetMeshTerms_CallsGetMeshTerms()
    {
        const string key = "key";
        var meshTermsToReturn = new Dictionary<string, MeshTerm>();
        meshTermsToReturn.Add(key, new MeshTerm() { DescriptorId = "descriptor" });

        var searchServiceMock = GivenIHaveAPubMedSearchServiceMock(null, meshTermsToReturn);

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, searchServiceMock.Object,
            GivenIHaveAnImpactScoringServiceMock().Object, GivenIHaveAFlashCardServiceMock(null).Object);

        var result = await client.GetMeshTerms();

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey(key));
        Assert.AreEqual(result[key].DescriptorId, meshTermsToReturn[key].DescriptorId);
    }

    #endregion GetMeshTerms

    #region GenerateFlashCards

    [TestMethod]
    public async Task GenerateFlashCards_CallsGetFlashCardSet()
    {
        var name1 = "name1";
        var name2 = "name2";

        var articleSets = new List<ArticleSet>
        {
            new ArticleSet{ Name = name1 },
            new ArticleSet{ Name = name2 }
        };

        var client = new PubMedClient(NullLogger<PubMedClient>.Instance, GivenIHaveAPubMedSearchServiceMock(null, null).Object,
            GivenIHaveAnImpactScoringServiceMock().Object, GivenIHaveAFlashCardServiceMock(null).Object);

        var cardSets = await client.GenerateFlashCards(articleSets);

        Assert.IsNotNull(cardSets);
        Assert.AreEqual(2, cardSets.Count);

        var titles = cardSets.Select(item => item.Title).ToList();

        Assert.IsTrue(titles.Contains(name1));
        Assert.IsTrue(titles.Contains(name2));
    }

    #endregion GenerateFlashCards

    #region steps

    private static Mock<IPubMedSearchService> GivenIHaveAPubMedSearchServiceMock(List<Article>? articlesToReturn, Dictionary<string, MeshTerm>? meshTerms)
    {
        var mock = new Mock<IPubMedSearchService>();
        mock.Setup(m => m.FindArticles(It.IsAny<ArticleFilter>()))
            .ReturnsAsync(articlesToReturn ?? new List<Article>());
        mock.Setup(m => m.GetMeshTerms())
            .ReturnsAsync(meshTerms ?? new Dictionary<string, MeshTerm>());

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

    private static Mock<IFlashCardService> GivenIHaveAFlashCardServiceMock(List<Card>? cards)
    {
        var mock = new Mock<IFlashCardService>();
        mock.Setup(m => m.GetFlashCardSet(It.IsAny<ArticleSet>()))
            .ReturnsAsync(cards ?? new List<Card>());

        return mock;
    }

    private static Mock<IFlashCardDatabase> GivenIHaveAFlashCardDatabaseMock()
    {
        return new Mock<IFlashCardDatabase>();
    }

    #endregion steps
}