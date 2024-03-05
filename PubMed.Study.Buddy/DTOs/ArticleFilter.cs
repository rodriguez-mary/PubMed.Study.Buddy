namespace PubMed.Study.Buddy.DTOs;

public class ArticleFilter
{
    //todo validate input against https://ftp.ncbi.nih.gov/pubmed/J_Medline.txt
    public List<string>? Journal { get; set; }

    //note that I will need to look across mesh term, mesh header, mesh subheader
    public List<string>? MeshTerm { get; set; }

    public int? StartYear { get; set; }

    public int? EndYear { get; set; }
}