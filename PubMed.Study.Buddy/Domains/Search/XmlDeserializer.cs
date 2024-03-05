using System.Xml;
using System.Xml.Serialization;

namespace PubMed.Article.Extract.Utility.Domains.Search;

internal class XmlDeserializer<T>
{
    public T? DeserializeXml(string data)
    {
        var serializer = new XmlSerializer(typeof(T));

        try
        {
            using (var reader = new StringReader(data))
            {
                var xml = serializer.Deserialize(reader);
                if (xml == null) return default;
                return (T)xml;
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle exceptions related to serialization issues
        }
        catch (XmlException ex)
        {
            // Handle XML format issues
        }
        catch (Exception ex)
        {
            // Handle other exceptions
        }

        return default;
    }
}