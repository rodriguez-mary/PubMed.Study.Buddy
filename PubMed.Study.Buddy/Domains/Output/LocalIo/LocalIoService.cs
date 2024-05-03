using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Output.LocalIo;

public class LocalIoService(ILogger<LocalIoService> logger, IConfiguration config) : IOutputService
{
    // ReSharper disable once StringLiteralTypo
    private readonly string _articleListCsvFileName = "pubmed_extract_" + DateTime.Now.ToString("yyyyMMddTHHmmss");  //ISO 8601 date format

    private readonly string _fileDirectory = config["localIoDirectory"] ?? Environment.CurrentDirectory;

    public Task GenerateArticleDataFile(List<Article> articles)
    {
        EnsureFilePathCreated();

        var filePath = Path.Combine(_fileDirectory, $"{_articleListCsvFileName}.csv");

        File.WriteAllText(filePath, Utilities.CreateCsvString(articles));

        logger.LogInformation("File written to {filePath}.", filePath);
        return Task.CompletedTask;
    }


    private void EnsureFilePathCreated()
    {
        if (!Directory.Exists(_fileDirectory))
            Directory.CreateDirectory(_fileDirectory);
    }
}