﻿using PubMed.Study.Buddy.Domains.Search.EUtils;

namespace PubMed.Study.Buddy.DTOs;

[Serializable]
public class Article : IComparable<Article>, IEquatable<Article>
{
    public string Id { get; set; } = string.Empty;

    public DateTime PublicationDate { get; set; } = DateTime.MinValue;

    public string Abstract { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string PubMedUrl => $"{EUtilsConstants.PubMedBaseUrl}{Id}";

    public List<Author>? AuthorList { get; set; }

    public List<MeshTerm>? MajorTopicMeshHeadings { get; set; }
    public List<MeshTerm>? MinorTopicMeshHeadings { get; set; }

    public Publication? Publication { get; set; }

    public double ImpactScore { get; set; } = 0;

    /// <summary>
    /// List of PubMed article IDs that cite this article.
    /// </summary>
    public List<string>? CitedBy { get; set; }

    public int CompareTo(Article? other)
    {
        return other == null ? 0 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is Article article && Equals(article);

    public bool Equals(Article? other)
    {
        return other != null && string.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Title.Replace(",", "")}[{Id}]";
    }
}