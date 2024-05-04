using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using PubMed.Study.Buddy.Domains.Client;
using PubMed.Study.Buddy.Domains.Cluster.Hierarchical;
using PubMed.Study.Buddy.Domains.FlashCard;
using PubMed.Study.Buddy.Domains.FlashCard.ChatGpt;
using PubMed.Study.Buddy.Domains.ImpactScoring;
using PubMed.Study.Buddy.Domains.ImpactScoring.CitationNumber;
using PubMed.Study.Buddy.Domains.Output;
using PubMed.Study.Buddy.Domains.Output.LocalIo;
using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.Domains.Search.EUtils;
using PubMed.Study.Buddy.DTOs;
using System.Text;

#region register and init services

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
builder.Services.AddSingleton<IFlashCardService, ChatGptFlashCardService>();

var serviceProvider = builder.Services.BuildServiceProvider();

#endregion register and init services

var pubMedClient = serviceProvider.GetRequiredService<IPubMedClient>();

// get the list of articles
var filename = Path.Combine(@"c:\temp\studybuddy", "articles.json");
//var vetSurgeryMeshTerms = new List<List<string>> { new() { "Q000662" }, new() { "D013502", "Q000601" }, new() { "D004285", "D002415" } };
var vetSurgeryMeshTerms = new List<List<string>> { new() { "veterinary" }, new() { "surgery" }, new() { "dogs", "cats" } };
var articles = File.Exists(filename) ? await LoadArticlesFromFile(filename) : await LoadFromPubMed(pubMedClient, filename, vetSurgeryMeshTerms);

// cluster the articles
Console.WriteLine("Getting mesh terms");
var meshTerms = await pubMedClient.GetMeshTerms();
Console.WriteLine("Clustering...");
var clustering = new HierarchicalByMeshTermClusterService(meshTerms);
var clusters = clustering.ClusterArticles(articles);


using var sw = new StreamWriter(@"c:\temp\studybuddy\hierarchical.csv", false, Encoding.UTF8);
sw.WriteLine("articles,cluster name,articles");
foreach (var cluster in clusters)
{
    var a = cluster.Articles;
    sw.WriteLine($"{a.Count},{cluster.Name.Replace(",", "")},{string.Join(",", a)}");
}

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

static async Task<List<Article>> LoadFromPubMed(IPubMedClient pubMedClient, string filename, List<List<string>> meshTerms)
{
    Console.WriteLine("Loading from PubMed");

    var threeYearArticles = new ArticleFilter
    {
        EndYear = 2024,
        StartYear = 2021,
        MeshTerm = meshTerms,
        Journal = ["J Feline Med Surg", "J Vet Emerg Crit Care San Antonio", "J Vet Intern Med", "Vet Radiol Ultrasound"]
    };

    var fiveYearArticles = new ArticleFilter
    {
        EndYear = 2024,
        StartYear = 2019,
        MeshTerm = meshTerms,
        Journal = ["J Am Vet Med Assoc", "J Small Anim Pract", "Vet Comp Orthop Traumatol", "Vet Surg"]
    };

    var articles = await pubMedClient.FindArticles([threeYearArticles, fiveYearArticles]);

    //save to file
    var jsonString = JsonConvert.SerializeObject(articles);
    File.WriteAllText(filename, jsonString);

    await pubMedClient.GenerateArticleDataFile(articles);

    return articles;
}