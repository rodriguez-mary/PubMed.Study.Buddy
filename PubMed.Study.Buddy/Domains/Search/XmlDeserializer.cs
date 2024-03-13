using PubMed.Study.Buddy.Domains.Search.Exceptions;
using System.Xml;
using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Domains.Search;

internal static class XmlDeserializer<T>
{
    public static T? DeserializeXml(string data)
    {
        var serializer = new XmlSerializer(typeof(T));

        try
        {
            using var reader = new StringReader(data);
            var xml = serializer.Deserialize(reader);
            if (xml == null) return default;
            return (T)xml;
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            throw new InvalidPubMedDataException(data, ex.Message);
        }
    }
}