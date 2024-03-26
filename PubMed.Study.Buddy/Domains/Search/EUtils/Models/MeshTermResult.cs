using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Domains.Search.EUtils.Models;

[XmlRoot(ElementName = "DescriptorRecordSet")]
public class MeshTermResult
{
    [XmlElement(ElementName = "DescriptorRecord")]
    public List<DescriptorRecord> DescriptorRecords { get; set; } = [];
}

public class DescriptorRecord
{
    [XmlElement(ElementName = "DescriptorUI")]
    public string DescriptorId { get; set; } = string.Empty;

    [XmlElement(ElementName = "DescriptorName")]
    public DescriptorNameAttribute DescriptorName { get; set; } = new();

    public TreeNumberList TreeNumberList { get; set; } = new();
}

public class DescriptorNameAttribute
{
    public string String { get; set; } = string.Empty;
}

public class TreeNumberList
{
    [XmlElement(ElementName = "TreeNumber")]
    public List<string> TreeNumber { get; set; } = [];
}