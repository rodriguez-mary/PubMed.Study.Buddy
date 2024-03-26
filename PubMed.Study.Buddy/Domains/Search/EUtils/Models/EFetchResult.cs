using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Domains.Search.EUtils.Models;

//PubMed sometimes encodes article data with xml-like syntax, such as CO<sub>2</sub>
//to accomodate this, any strings that we read in must have a two-step process to handle "XML nodes" in leaf node properties

[XmlRoot(ElementName = "PubmedArticleSet")]
public class EFetchResult
{
    [XmlElement(ElementName = "PubmedArticle")]
    public List<PubmedArticle> PubmedArticles { get; set; } = [];
}

public class PubmedArticle
{
    public MedlineCitation MedlineCitation { get; set; } = new();

    public PubmedData PubmedData { get; set; } = new();
}

public class MedlineCitation
{
    public EFetchArticle Article { get; set; } = new();

    [XmlElement(ElementName = "MeshHeadingList")]
    public MeshHeadingList? MeshHeadingList { get; set; }

    // ReSharper disable once StringLiteralTypo
    [XmlElement(ElementName = "PMID")]
    public string Id { get; set; } = string.Empty;
}

public class PubmedData
{
    public PubMedHistory History { get; set; } = new();
}

public class PubMedHistory
{
    [XmlElement(ElementName = "PubMedPubDate")]
    public List<PubMedPubDate> PubMedPubDates { get; set; } = [];
}

public class PubMedPubDate
{
    [XmlAttribute(AttributeName = "PubStatus")]
    public string PubStatus { get; set; } = string.Empty;

    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
}

public class EFetchArticle
{
    [XmlIgnore]
    public string ArticleTitle
    {
        get
        {
            if (DynamicArticleTitle is not XmlNode[] nodes) return DynamicArticleTitle.ToString();

            var stringBuilder = new StringBuilder();
            foreach (var node in nodes)
                stringBuilder.Append(node.OuterXml);

            return stringBuilder.ToString();
        }
    }

    [XmlElement(ElementName = "ArticleTitle")]
    public dynamic DynamicArticleTitle { get; set; } = string.Empty;

    public Journal? Journal { get; set; }

    [XmlElement(ElementName = "AuthorList")]
    public AuthorList? AuthorList { get; set; }

    public ArticleDate? ArticleDate { get; set; }
}

public class Journal
{
    public JournalIssue? JournalIssue { get; set; }

    [XmlIgnore]
    public string Title
    {
        get
        {
            if (DynamicTitle is not XmlNode[] nodes) return DynamicTitle.ToString();

            var stringBuilder = new StringBuilder();
            foreach (var node in nodes)
                stringBuilder.Append(node.OuterXml);

            return stringBuilder.ToString();
        }
    }

    [XmlElement(ElementName = "Title")]
    public dynamic DynamicTitle { get; set; } = string.Empty;
}

public class JournalIssue
{
    public string Volume { get; set; } = string.Empty;
    public PubDate? PubDate { get; set; }
}

public class PubDate
{
    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
}

public class AuthorList
{
    [XmlElement(ElementName = "Author")] public List<Author> Authors { get; set; } = [];
}

public class Author
{
    public string LastName { get; set; } = string.Empty;
    public string ForeName { get; set; } = string.Empty;
    public string? Initials { get; set; }
}

public class ArticleDate
{
    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
}

public class MeshHeadingList
{
    [XmlElement(ElementName = "MeshHeading")]
    public List<MeshHeading> MeshHeadings { get; set; } = [];
}

public class MeshHeading
{
    public DescriptorName? DescriptorName { get; set; }

    [XmlElement(ElementName = "QualifierName")]
    public List<QualifierName>? QualifierNames { get; set; }
}

public class DescriptorName
{
    [XmlAttribute(AttributeName = "MajorTopicYN")]
    public string MajorTopicYn { get; set; } = "N";

    [XmlAttribute(AttributeName = "UI")]
    public string Id { get; set; } = string.Empty;

    [XmlText]
    public string Name { get; set; } = string.Empty;
}

public class QualifierName
{
    [XmlAttribute(AttributeName = "MajorTopicYN")]
    public string MajorTopicYn { get; set; } = "N";

    [XmlAttribute(AttributeName = "UI")]
    public string Id { get; set; } = string.Empty;

    [XmlText]
    public string Name { get; set; } = string.Empty;
}