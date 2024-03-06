using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Domains.Search.EUtils.Models;

[XmlRoot(ElementName = "PubmedArticleSet")]
public class EFetchResult
{
    [XmlElement(ElementName = "PubmedArticle")]
    public List<PubmedArticle> PubmedArticles { get; set; } = new();
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
    public List<PubMedPubDate> PubMedPubDates { get; set; } = new();
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
    public string ArticleTitle { get; set; } = string.Empty;

    public Journal? Journal { get; set; }

    [XmlElement(ElementName = "AuthorList")]
    public AuthorList? AuthorList { get; set; }

    public ArticleDate? ArticleDate { get; set; }
}

public class Journal
{
    public JournalIssue? JournalIssue { get; set; }
    public string Title { get; set; } = string.Empty;
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
    [XmlElement(ElementName = "Author")] public List<Author> Authors { get; set; } = new();
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
    public List<MeshHeading> MeshHeadings { get; set; } = new();
}

public class MeshHeading
{
    public DescriptorName? DescriptorName { get; set; }
}

public class DescriptorName
{
    [XmlAttribute(AttributeName = "MajorTopicYN")]
    public string MajorTopicYn { get; set; } = "N";

    [XmlText]
    public string Name { get; set; } = string.Empty;
}