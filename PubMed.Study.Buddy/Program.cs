using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using PubMed.Study.Buddy.Domains.Client;
using PubMed.Study.Buddy.Domains.Cluster.Agglomerative;
using PubMed.Study.Buddy.Domains.Cluster.Hierarchical;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.ImpactScoring.CitationNumber;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Output.LocalIo;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.Domains.Search.EUtils;
using PubMed.Study.Buddy.DTOs;

// ReSharper disable StringLiteralTypo

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

// Do the stuff
var pubMedClient = serviceProvider.GetRequiredService<IPubMedClient>();

var filename = Path.Combine(@"c:\temp\studybuddy", "articles.json");
var articles = File.Exists(filename) ? await LoadArticlesFromFile(filename) : await LoadFromPubMed(pubMedClient, filename);
//await pubMedClient.GenerateArticleDataFile(articles);

var meshTerms = new Dictionary<string, MeshTerm>();
foreach (var meshHeading in articles.Where(article => article.MajorTopicMeshHeadings != null).SelectMany(article => article.MajorTopicMeshHeadings!))
{
    meshTerms.TryAdd(meshHeading.DescriptorId, meshHeading);
}

var clustering = new AgglomerativeClusteringService();
clustering.GetClusters(articles);

return;

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
            retryAttempt)));
}

static async Task<List<Article>> LoadArticlesFromFile(string filename)
{
    Console.WriteLine("Loading from file");
    return JsonConvert.DeserializeObject<List<Article>>(await File.ReadAllTextAsync(filename)) ?? [];
}

static async Task<List<Article>> LoadFromPubMed(IPubMedClient pubMedClient, string filename)
{
    Console.WriteLine("Loading from PubMed");

    var vetSurgeryMeshTerms = new List<List<string>> { new() { "veterinary" }, new() { "surgery" }, new() { "dogs", "cats" } };

    var threeYearArticles = new ArticleFilter
    {
        EndYear = 2024,
        StartYear = 2021,
        MeshTerm = vetSurgeryMeshTerms,
        Journal = ["J Feline Med Surg", "J Vet Emerg Crit Care (San Antonio)", "J Vet Intern Med", "Vet Radiol Ultrasound"]
    };

    var fiveYearArticles = new ArticleFilter
    {
        EndYear = 2024,
        StartYear = 2019,
        MeshTerm = vetSurgeryMeshTerms,
        Journal = ["J Am Vet Med Assoc", "J Small Anim Pract", "Vet Comp Orthop Traumatol", "Vet Surg"]
    };

    var articles = await pubMedClient.FindArticles([threeYearArticles, fiveYearArticles]);

    //save to file
    var jsonString = JsonConvert.SerializeObject(articles);
    File.WriteAllText(filename, jsonString);

    return articles;
}