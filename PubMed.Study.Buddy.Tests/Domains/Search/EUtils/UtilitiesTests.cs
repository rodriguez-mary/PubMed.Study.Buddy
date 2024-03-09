using PubMed.Study.Buddy.Domains.Search.EUtils;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Tests.Domains.Search.EUtils;

[TestClass]
public class UtilitiesTests
{
    [TestMethod]
    public void GetQueryFromArticleFilter_IncludesDatabaseParameter()
    {
        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter());

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(1, qp.Count);
        Assert.AreEqual($"{EUtilsConstants.DatabaseParameter}={EUtilsConstants.PubMedDbId}", qp[0]);
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_IncludesYearParameters()
    {
        const int startYear = 2020;
        const int endYear = 2023;

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            StartYear = startYear,
            EndYear = endYear
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(2, qp.Count);

        Assert.IsTrue(qp.Contains($"{EUtilsConstants.StartDateParameter}={startYear}[{EUtilsConstants.PublishDateType}]"));
        Assert.IsTrue(qp.Contains($"{EUtilsConstants.EndDateParameter}={endYear}[{EUtilsConstants.PublishDateType}]"));
    }

    private static List<string> GetQueryParams(string queryString)
    {
        return queryString.Split("&").ToList();
    }
}