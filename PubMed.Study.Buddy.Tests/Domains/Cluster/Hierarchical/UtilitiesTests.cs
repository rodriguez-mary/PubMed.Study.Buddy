using PubMed.Study.Buddy.Domains.Cluster.Hierarchical;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Tests.Domains.Cluster.Hierarchical;

[TestClass]
public class UtilitiesTests
{
    #region GetTaxonomyCounts

    [TestMethod]
    public void GetTaxonomyCounts_EmptySetOnNoArticles()
    {
        var result = Utilities.GetTaxonomyCounts(new List<DTOs.Article>());

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetTaxonomyCounts_EmptySetOnEmptyLineage()
    {
        var result = Utilities.GetTaxonomyCounts(new List<DTOs.Article> { new DTOs.Article { MajorTopicMeshHeadings = new List<DTOs.MeshTerm> { new DTOs.MeshTerm { TreeNumbers = new List<string> { } } } } });

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetTaxonomyCounts_NullLineageValueIgnored()
    {
        var result = Utilities.GetTaxonomyCounts(new List<DTOs.Article> { new DTOs.Article { MajorTopicMeshHeadings = new List<DTOs.MeshTerm> { new DTOs.MeshTerm { TreeNumbers = new List<string> { "" } } } } });

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetTaxonomyCounts_LineageValueDeduplicated()
    {
        var result = Utilities.GetTaxonomyCounts(new List<Article> { new Article { MajorTopicMeshHeadings = new List<MeshTerm> { new MeshTerm { TreeNumbers = new List<string> { "A", "A" } } } } });

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.Keys.Contains("A"));
    }

    [TestMethod]
    public void GetTaxonomyCounts_AllLineageAssociatedWithArticle()
    {
        var articleId = "article_id";
        var level1 = "l1";
        var level2 = "l2";
        var articles = new List<Article>
        {
            new Article { Id = articleId, MajorTopicMeshHeadings = new List<MeshTerm> { new MeshTerm { TreeNumbers = new List<string> { $"{level1}.{level2}" } } } }
        };

        var result = Utilities.GetTaxonomyCounts(articles);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Keys.Contains(level1));
        Assert.IsTrue(result[level1].ArticleIds.Contains(articleId));
        Assert.IsTrue(result.Keys.Contains($"{level1}.{level2}"));
        Assert.IsTrue(result[$"{level1}.{level2}"].ArticleIds.Contains(articleId));
    }

    #endregion GetTaxonomyCounts

    #region ArticleLineage

    [TestMethod]
    public void ArticleLineage_EmptySetOnNullMajor()
    {
        var result = Utilities.ArticleLineage(new DTOs.Article());

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ArticleLineage_ExcludesBTaxonomyBranch()
    {
        var result = Utilities.ArticleLineage(new DTOs.Article { MajorTopicMeshHeadings = new List<DTOs.MeshTerm> { new DTOs.MeshTerm { TreeNumbers = new List<string> { "B.X" } } } });

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ArticleLineage_IncludesAllTaxonomyParents()
    {
        var level1 = "l1";
        var level2 = "l2";
        var level3 = "l3";

        var result = Utilities.ArticleLineage(new DTOs.Article { MajorTopicMeshHeadings = new List<DTOs.MeshTerm> { new DTOs.MeshTerm { TreeNumbers = new List<string> { $"{level1}.{level2}.{level3}" } } } });

        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains(level1));
        Assert.IsTrue(result.Contains($"{level1}.{level2}.{level3}"));
        Assert.IsTrue(result.Contains($"{level1}.{level2}"));
    }

    #endregion ArticleLineage
}