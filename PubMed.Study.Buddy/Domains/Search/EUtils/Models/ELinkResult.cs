using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Domains.Search.EUtils.Models;

[XmlRoot(ElementName = "eLinkResult")]
public class ELinkResult
{
    public LinkSet LinkSet { get; set; } = new();
}

public class LinkSet
{
    public LinkSetDb? LinkSetDb { get; set; }
}

public class LinkSetDb
{
    [XmlElement(ElementName = "Link")]
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public string Id { get; set; } = string.Empty;
}