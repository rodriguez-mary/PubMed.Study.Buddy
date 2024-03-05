namespace PubMed.Study.Buddy.DTOs;

public class Publication
{
    public DateTime PublicationDate { get; set; } = DateTime.MinValue;

    public string JournalName { get; set; } = string.Empty;

    public int? Volume { get; set; }

    public int? Issue { get; set; }
}