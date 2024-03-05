using System.Xml.Serialization;

namespace PubMed.Article.Extract.Utility.Domains.Search.Models;

[XmlRoot(ElementName = "eLinkResult")]
public class ELinkResult
{
    public LinkSet LinkSet { get; set; }
}

public class LinkSet
{
    public LinkSetDb? LinkSetDb { get; set; }
}

public class LinkSetDb
{
    [XmlElement(ElementName = "Link")]
    public List<Link>? Links { get; set; } = new();
}

public class Link
{
    public string Id { get; set; } = string.Empty;
}