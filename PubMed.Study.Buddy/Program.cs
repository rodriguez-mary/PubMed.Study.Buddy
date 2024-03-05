using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PubMed.Article.Extract.Utility.Domains.Search;
using PubMed.Article.Extract.Utility.DTOs;
using PubMed.Study.Buddy.Domains.Search;

var builder = Host.CreateApplicationBuilder(args);

// Set up the demo project to read form secrets and appsettings files

var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
var configurationBuilder = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddJsonFile("appsettings.json");
builder.Services.AddSingleton<IConfiguration>(configurationBuilder.Build());

builder.Services.AddLogging();
builder.Services.AddHttpClient<IPubMedSearchService, PubMedSearchService>();

/*
using var loggerFactory = LoggerFactory.Create(c =>
{
    c
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
        .AddConsole();
});*/

var serviceProvider = builder.Services.BuildServiceProvider();
var pubMedSearchService = serviceProvider.GetRequiredService<IPubMedSearchService>();

var articleFilter = new ArticleFilter
{
    EndYear = 2019,
    StartYear = 2019,
    MeshTerm = new List<string> { "veterinary", "surgery" },
    Journal = new List<string> { "Vet Surg" }
};

var client = new HttpClient()
{
};

var results = await pubMedSearchService.FindArticles(articleFilter);