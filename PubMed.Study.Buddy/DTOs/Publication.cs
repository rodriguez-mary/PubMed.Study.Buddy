namespace PubMed.Study.Buddy.DTOs;

public class Publication
{
    public string JournalName { get; set; } = string.Empty;

    public string? Volume { get; set; }

    public DateTime? JournalDate { get; set; }
}