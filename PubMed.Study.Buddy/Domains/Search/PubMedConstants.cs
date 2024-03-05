namespace PubMed.Article.Extract.Utility.Domains.Search;

internal class PubMedConstants
{
    public const string PubMedBaseUrl = "https://pubmed.ncbi.nlm.nih.gov/";

    public const string PubMedDbId = "pubmed";

    // endpoint constants

    public const string SearchEndpoint = "esearch.fcgi";
    public const string LinkEndpoint = "elink.fcgi";

    // general parameters

    public const string ApiKeyParameter = "api_key";

    // link endpoint parameters

    public const string OriginalDatabaseParameter = "dbfrom";
    public const string LinkTypeParameter = "linkname";
    public const string CitationLinkType = "pubmed_pmc_refs";

    // search endpoint parameters

    public const string DatabaseField = "db";
    public const string JournalField = "JOUR";
    public const string MeshField = "MESH";
    public const string MeshSubheadingField = "SUBH";
    public const string MeshMajorTopicField = "MAJR";
    public const string PublishDateType = "pdat";
    public const string StartDateParameter = "mindate";
    public const string EndDateParameter = "maxdate";
    public const string TermParameter = "term";
}