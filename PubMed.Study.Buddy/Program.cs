using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using PubMed.Study.Buddy.Domains.Client;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.ImpactScoring.CitationNumber;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Output.LocalIo;
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
builder.Services.AddHttpClient<IPubMedSearchService, EUtilsSearchService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5)) //Set lifetime to five minutes
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddSingleton<IImpactScoringService, CitationNumberImpactScoringService>();
builder.Services.AddSingleton<IOutputService, LocalIoService>();
builder.Services.AddSingleton<IPubMedClient, PubMedClient>();

var serviceProvider = builder.Services.BuildServiceProvider();
var pubMedClient = serviceProvider.GetRequiredService<IPubMedClient>();

var vetSurgMeshTerms = new List<List<string>> { new() { "veterinary" }, new() { "surgery" }, new() { "dogs", "cats" } };

var threeYearArticles = new ArticleFilter
{
    EndYear = 2024,
    StartYear = 2021,
    MeshTerm = vetSurgMeshTerms,
    Journal = new List<string> { "Vet Surg" }
};

var fiveYearArticles = new ArticleFilter
{
    EndYear = 2024,
    StartYear = 2019,
    MeshTerm = vetSurgMeshTerms,
    Journal = new List<string> { "J Vet Intern Med" }
};

var articles = await pubMedClient.FindArticles(new List<ArticleFilter> { threeYearArticles });

await pubMedClient.GenerateContent(articles);

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
            retryAttempt)));
}