namespace PubMed.Study.Buddy.DTOs;

public class FlashCardSet
{
    public string Title { get; set; } = string.Empty;

    public List<FlashCard> Cards { get; set; } = [];

    // list of the articles used to generate this flash card set
    public List<Article> Articles { get; set; } = [];
}