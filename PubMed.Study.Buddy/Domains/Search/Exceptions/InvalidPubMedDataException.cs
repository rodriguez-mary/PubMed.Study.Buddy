namespace PubMed.Study.Buddy.Domains.Search.Exceptions;

public class InvalidPubMedDataException(string data, string message) : Exception(message)
{
    public string InvalidData { get; } = data;
}