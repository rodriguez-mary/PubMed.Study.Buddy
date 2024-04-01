namespace PubMed.Study.Buddy.DTOs;

public class ArticleSet : IEquatable<ArticleSet>
{
    public string Name { get; set; } = string.Empty;

    public List<Article> Articles { get; set; } = [];

    public int Distance { get; set; } = 0;

    public bool Equals(ArticleSet? other)
    {
        return other != null && Articles.All(other.Articles.Contains);
    }
}