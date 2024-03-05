using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.Domains.Search.EUtils;
using PubMed.Study.Buddy.DTOs;

var builder = Host.CreateApplicationBuilder(args);

// Set up the demo project to read form secrets and appsettings files

var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
var configurationBuilder = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddJsonFile("appsettings.json");
builder.Services.AddSingleton<IConfiguration>(configurationBuilder.Build());

builder.Services.AddLogging();
builder.Services.AddHttpClient<IPubMedSearchService, EUtilsSearchService>();

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