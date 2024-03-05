namespace PubMed.Article.Extract.Utility.DTOs;

public class Author
{
    public string LastName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? Initials { get; set; }

    public bool Primary { get; set; } = false;
}