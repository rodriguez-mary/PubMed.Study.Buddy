using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Output.LocalIo;

public class LocalIoService : IOutputService
{
    private readonly string _articleListCsvFileName = "pubmed_extract_" + DateTime.Now.ToString("yyyyMMddTHHmmss");  //ISO 8601 date format

    private readonly string _fileDirectory;
    private readonly ILogger _logger;

    public LocalIoService(ILogger<LocalIoService> logger, IConfiguration config)
    {
        _logger = logger;
        _fileDirectory = config["localIoDirectory"] ?? Environment.CurrentDirectory;
    }

    public Task GenerateArticleList(List<Article> articles)
    {
        var csv = new StringBuilder();

        csv.AppendLine("id,impact score,title,first author,secondary author(s),publication date,journal,cited count,abstract,PubMed link");
        foreach (var article in articles)
        {
            csv.AppendLine(
                $"{article.Id}," +
                $"{article.ImpactScore}," +
                $"\"{article.Title}\"," +
                $"\"{GetFirstAuthorLastFirstName(article)}\"," +
                $"," + //todo
                $"{article.PublicationDate.ToString("yyyy-MM-dd")}," +
                $"\"{article.Publication?.JournalName ?? string.Empty}\"," +
                $"{(article.CitedBy == null ? "0" : article.CitedBy.Count)}," +
                $"\"{article.Abstract}\"," +
                $"{article.PubMedUrl}");
        }

        File.WriteAllText(Path.Combine(_fileDirectory, $"{_articleListCsvFileName}.csv"), csv.ToString());

        return Task.CompletedTask;
    }

    private string GetFirstAuthorLastFirstName(Article article)
    {
        if (article.AuthorList == null || article.AuthorList.Count == 0) return string.Empty;

        //there should only every be one first..
        var firstAuthor = article.AuthorList.FirstOrDefault(author => author is { First: true }, null);
        if (firstAuthor == null) return string.Empty;

        return $"{firstAuthor.LastName},{firstAuthor.FirstName}";
    }
}