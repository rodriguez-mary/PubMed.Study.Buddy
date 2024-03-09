using System.Runtime.CompilerServices;
using PubMed.Study.Buddy.DTOs;
using System.Text;

[assembly: InternalsVisibleTo("PubMed.Study.Buddy.Tests")]

namespace PubMed.Study.Buddy.Domains.Output.LocalIo;

internal static class Utilities
{
    internal static string CreateCsvString(List<Article> articles)
    {
        var csv = new StringBuilder();

        csv.AppendLine("id,impact score,title,first author,publication date,journal,major topics,cited count,abstract,PubMed link");
        foreach (var article in articles)
        {
            csv.AppendLine(
                $"{article.Id}," +
                $"{article.ImpactScore}," +
                $"\"{article.Title.Replace("\"", "'")}\"," +
                $"\"{GetFirstAuthorLastFirstName(article).Replace("\"", "'")}\"," +
                $"{article.PublicationDate.ToString("yyyy-MM-dd")}," +
                $"\"{article.Publication?.JournalName.Replace("\"", "'") ?? string.Empty}\"," +
                $"\"{(article.MajorTopicMeshHeadings != null ? string.Join(",", article.MajorTopicMeshHeadings) : string.Empty)}\"," +
                $"{(article.CitedBy == null ? "0" : article.CitedBy.Count)}," +
                $"\"{article.Abstract.Replace("\"", "'")}\"," +  //double quote for break lines
                $"\"{article.PubMedUrl}\"");
        }

        return csv.ToString();
    }

    private static string GetFirstAuthorLastFirstName(Article article)
    {
        if (article.AuthorList == null || article.AuthorList.Count == 0) return string.Empty;

        //there should only every be one first..
        var firstAuthor = article.AuthorList.FirstOrDefault(author => author is { First: true }, null);
        return firstAuthor == null ? string.Empty : $"{firstAuthor.LastName},{firstAuthor.FirstName}";
    }
}