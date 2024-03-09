using Newtonsoft.Json.Linq;
using PubMed.Study.Buddy.Domains.Search;
using System.Xml.Linq;
using System.Xml.Serialization;
using PubMed.Study.Buddy.Domains.Search.Exceptions;

namespace PubMed.Study.Buddy.Tests.Domains.Search;

[TestClass]
public class XmlDeserializerTests
{
    [TestMethod]
    public void DeserializeXml_ValidXmlDeserializes()
    {
        const string attributeValue = "attribute_value";
        const string idValue = "id_value";

        var xmlData = $"<XmlTestClass Attribute=\"{attributeValue}\"><Id>{idValue}</Id></XmlTestClass>";

        var result = XmlDeserializer<XmlTestClass>.DeserializeXml(xmlData);

        Assert.IsNotNull(result);
        Assert.AreEqual(attributeValue, result.Attribute);
        Assert.AreEqual(idValue, result.Id);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidPubMedDataException))]
    public void DeserializeXml_InvalidXmlThrowsException()
    {
        _ = XmlDeserializer<XmlTestClass>.DeserializeXml("this ain't xml");
    }
}

public class XmlTestClass
{
    public string Id { get; set; } = string.Empty;

    [XmlAttribute(AttributeName = "Attribute")]
    public string Attribute { get; set; } = string.Empty;
}