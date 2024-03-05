using System.Xml.Serialization;

namespace PubMed.Article.Extract.Utility.Domains.Search.Models;

[XmlRoot(ElementName = "eSearchResult")]
public class ESearchResult
{
    [XmlElement(ElementName = "Count")]
    public int Count { get; set; }

    [XmlElement(ElementName = "RetMax")]
    public int RetMax { get; set; }

    [XmlElement(ElementName = "RetStart")]
    public int RetStart { get; set; }

    [XmlArray("IdList")]
    [XmlArrayItem("Id")]
    public List<string> IdList { get; set; } = new();

    [XmlElement(ElementName = "ErrorList")]
    public ErrorList? ErrorList { get; set; }
}

public class ErrorList
{
    [XmlElement(ElementName = "PhraseNotFound")]
    public List<string>? PhraseNotFound { get; set; }

    [XmlElement(ElementName = "FieldNotFound")]
    public List<string>? FieldNotFound { get; set; }
}