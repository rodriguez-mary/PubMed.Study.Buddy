namespace PubMed.Study.Buddy.DTOs;

public class CardSet
{
    public string Title { get; set; } = string.Empty;

    public List<Card> Cards { get; set; } = [];
}