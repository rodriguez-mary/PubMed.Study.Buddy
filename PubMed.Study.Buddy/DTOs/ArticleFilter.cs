namespace PubMed.Study.Buddy.DTOs;

public class ArticleFilter
{
    //todo validate input against https://ftp.ncbi.nih.gov/pubmed/J_Medline.txt
    public List<string>? Journal { get; set; }

    //the outer list will be ANDed together, the inner list will be ORed together
    public List<List<string>>? MeshTerm { get; set; }

    public int? StartYear { get; set; }

    public int? EndYear { get; set; }
}