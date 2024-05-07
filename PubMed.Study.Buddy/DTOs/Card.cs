namespace PubMed.Study.Buddy.DTOs;

public class Card
{
    public Guid Id { get; set; }

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    // the article used to generate this flash card set
    public string ArticleId { get; set; } = string.Empty;
}