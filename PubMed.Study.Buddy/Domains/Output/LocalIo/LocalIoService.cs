using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Output.LocalIo;

public class LocalIoService(ILogger<LocalIoService> _, IConfiguration config) : IOutputService
{
    private readonly string _articleListCsvFileName = "pubmed_extract_" + DateTime.Now.ToString("yyyyMMddTHHmmss");  //ISO 8601 date format

    private readonly string _fileDirectory = config["localIoDirectory"] ?? Environment.CurrentDirectory;

    public Task GenerateArticleList(List<Article> articles)
    {
        File.WriteAllText(Path.Combine(_fileDirectory, $"{_articleListCsvFileName}.csv"), Utilities.CreateCsvString(articles));

        return Task.CompletedTask;
    }
}