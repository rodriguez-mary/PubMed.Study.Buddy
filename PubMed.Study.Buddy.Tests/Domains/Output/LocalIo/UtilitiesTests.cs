using PubMed.Study.Buddy.Domains.Output.LocalIo;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Tests.Domains.Output.LocalIo;

[TestClass]
public class UtilitiesTests
{
    [TestMethod]
    public void CreateCsvString_EmptyListSucceeds()
    {
        var csv = Utilities.CreateCsvString([]);
        Assert.IsNotNull(csv);
        var expectedCsv =
            "id,impact score,title,first author,publication date,journal,major topics,cited count,abstract,PubMed link" + Environment.NewLine;
        Assert.AreEqual(expectedCsv, csv);
    }

    [TestMethod]
    public void CreateCsvString_IncludesAllValues()
    {
        const string id = "article_id";
        const double score = 4.2;
        const string title = "article_title";
        const string fName = "first_name";
        const string lName = "last_name";
        const int year = 2023;
        const int month = 1;
        const int day = 2;
        const string jName = "journal_name";
        const string majTopic1 = "major_topic1";
        const string majTopic2 = "major_topic2";
        const string abs = "this is the abstract";

        var citedBy = new List<string> { "cited_by" };

        var article = new Article
        {
            Id = id,
            ImpactScore = score,
            Title = title,
            AuthorList = new List<Author> { new() { First = true, LastName = lName, FirstName = fName } },
            PublicationDate = new DateTime(year, month, day),
            Publication = new Publication { JournalName = jName },
            MajorTopicMeshHeadings = new List<MeshTerm> { new() { DescriptorName = majTopic1 }, new() { DescriptorName = majTopic2 } },
            CitedBy = citedBy,
            Abstract = abs
        };

        var csv = Utilities.CreateCsvString([article]);

        var expectedCsv =
            "id,impact score,title,first author,publication date,journal,major topics,cited count,abstract,PubMed link" +
            Environment.NewLine +
            $"{id},{score},\"{title}\",\"{lName},{fName}\",{year}-0{month}-0{day},\"{jName}\",\"{majTopic1},{majTopic2}\",{citedBy.Count},\"{abs}\",\"{article.PubMedUrl}\""
            + Environment.NewLine;

        Assert.AreEqual(expectedCsv, csv);
    }
}