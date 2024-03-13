using PubMed.Study.Buddy.Domains.Search;
using PubMed.Study.Buddy.Domains.Search.Exceptions;
using System.Xml.Serialization;

namespace PubMed.Study.Buddy.Tests.Domains.Search;

[TestClass]
public class XmlDeserializerTests
{
    [TestMethod]
    public void DeserializeXml_ValidXmlDeserializes()
    {
        const string attributeValue = "attribute_value";
        const string idValue = "id_value";

        const string xmlData = $"<XmlTestClass Attribute=\"{attributeValue}\"><Id>{idValue}</Id></XmlTestClass>";

        var result = XmlDeserializer<XmlTestClass>.DeserializeXml(xmlData);

        Assert.IsNotNull(result);
        Assert.AreEqual(attributeValue, result.Attribute);
        Assert.AreEqual(idValue, result.Id);
    }

    [TestMethod]
    public void DeserializeXml_InvalidXmlThrowsException()
    {
        const string badData = "this ain't xml";
        try
        {
            _ = XmlDeserializer<XmlTestClass>.DeserializeXml(badData);
            Assert.Fail();
        }
        catch (InvalidPubMedDataException ex)
        {
            Assert.AreEqual(badData, ex.InvalidData);
        }
    }
}

public class XmlTestClass
{
    public string Id { get; set; } = string.Empty;

    [XmlAttribute(AttributeName = "Attribute")]
    public string Attribute { get; set; } = string.Empty;
}