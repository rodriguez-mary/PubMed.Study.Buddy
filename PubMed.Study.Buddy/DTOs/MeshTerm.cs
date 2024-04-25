namespace PubMed.Study.Buddy.DTOs;

public class MeshTerm
{
    public string DescriptorId { get; set; } = string.Empty;
    public string DescriptorName { get; set; } = string.Empty;

    public List<string> TreeNumbers { get; set; } = [];
}