namespace PubMed.Study.Buddy.DTOs;

public class Author
{
    public string LastName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? Initials { get; set; }

    // Is this author listed first on the paper?
    public bool First { get; set; } = false;
}