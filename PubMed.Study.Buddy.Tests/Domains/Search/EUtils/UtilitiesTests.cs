using PubMed.Study.Buddy.Domains.Search.EUtils;
using PubMed.Study.Buddy.Domains.Search.EUtils.Models;
using PubMed.Study.Buddy.DTOs;
using Author = PubMed.Study.Buddy.Domains.Search.EUtils.Models.Author;

namespace PubMed.Study.Buddy.Tests.Domains.Search.EUtils;

[TestClass]
public class UtilitiesTests
{
    #region GetQueryFromArticleFilter tests

    [TestMethod]
    public void GetQueryFromArticleFilter_IncludesDatabaseParameter()
    {
        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter());

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(1, qp.Count);
        Assert.AreEqual($"{EUtilsConstants.DatabaseParameter}={EUtilsConstants.PubMedDbId}", qp[0]);
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_IncludesYearParameters()
    {
        const int startYear = 2020;
        const int endYear = 2023;

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            StartYear = startYear,
            EndYear = endYear
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(3, qp.Count); //db param + date params

        Assert.IsTrue(qp.Contains($"{EUtilsConstants.StartDateParameter}={startYear}[{EUtilsConstants.PublishDateType}]"));
        Assert.IsTrue(qp.Contains($"{EUtilsConstants.EndDateParameter}={endYear}[{EUtilsConstants.PublishDateType}]"));
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_IncludeJournalParams()
    {
        const string journal = "super legit journal";

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            Journal = [journal]
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(2, qp.Count);

        var escapedJournalName = Uri.EscapeDataString(journal);
        Assert.IsTrue(qp.Contains($"{EUtilsConstants.TermParameter}=({escapedJournalName}[{EUtilsConstants.JournalField}])"));
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_IncludeMeshParams()
    {
        const string meshTerm = "some term";

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            MeshTerm = [[meshTerm]]
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(2, qp.Count);

        var termValue = GetQueryParamValue(qp, EUtilsConstants.TermParameter);
        var escapedMeshTerm = Uri.EscapeDataString(meshTerm);
        Assert.IsTrue(termValue.Contains($"{escapedMeshTerm}[{EUtilsConstants.MeshField}"));
        Assert.IsTrue(termValue.Contains($"{escapedMeshTerm}[{EUtilsConstants.MeshMajorTopicField}"));
        Assert.IsTrue(termValue.Contains($"{escapedMeshTerm}[{EUtilsConstants.MeshSubheadingField}"));
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_MeshTermsAreJoinedCorrectly()
    {
        const string termA1 = "a1";
        const string termA2 = "a2";
        const string termB1 = "b1";

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            MeshTerm = [[termA1, termA2], [termB1]]  //As should be ORed and B should be ANDed with As
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(2, qp.Count);

        var termValue = GetQueryParamValue(qp, EUtilsConstants.TermParameter);

        var ands = termValue.Split("+AND+");
        Assert.AreEqual(2, ands.Length);

        var aTerms = ands.First(s => s.Contains(termA1));
        var ors = aTerms.Split("+OR+");
        Assert.AreEqual(6, ors.Length);  //should be six values--three per term
    }

    [TestMethod]
    public void GetQueryFromArticleFilter_MeshAndJournalTermsAreJoinedCorrectly()
    {
        const string meshTerm = "meshTerm";
        const string journal = "journal";

        var result = Utilities.GetQueryFromArticleFilter(new ArticleFilter
        {
            MeshTerm = [[meshTerm]],
            Journal = [journal]
        });

        Assert.IsNotNull(result);
        var qp = GetQueryParams(result);
        Assert.AreEqual(2, qp.Count);

        var termValue = GetQueryParamValue(qp, EUtilsConstants.TermParameter);

        var ands = termValue.Split("+AND+");
        Assert.AreEqual(2, ands.Length);
    }

    #endregion GetQueryFromArticleFilter tests

    #region CompileArticleFromResponses tests

    [TestMethod]
    public void CompileArticleFromResponses_IdIsMapped()
    {
        const string id = "article_id";
        var article = Utilities.CompileArticleFromResponses(id, new PubmedArticle(), new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(id, article.Id);
    }

    [TestMethod]
    public void CompileArticleFromResponses_AbstractIsMapped()
    {
        const string articleAbstract = "article_abstract";
        var article = Utilities.CompileArticleFromResponses(string.Empty, new PubmedArticle(), new ELinkResult(), articleAbstract);

        Assert.IsNotNull(article);
        Assert.AreEqual(articleAbstract, article.Abstract);
    }

    [TestMethod]
    public void CompileArticleFromResponses_TitleIsMapped()
    {
        const string title = "article title";
        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            { MedlineCitation = new MedlineCitation { Article = new EFetchArticle { DynamicArticleTitle = title } } },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(title, article.Title);
    }

    [TestMethod]
    public void CompileArticleFromResponses_PublishDateIsMapped()
    {
        const int year = 2020;
        const int month = 4;
        const int day = 29;
        var expectedDate = new DateTime(year, month, day);

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        ArticleDate = new ArticleDate
                        { Year = year.ToString(), Month = month.ToString(), Day = day.ToString() }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(expectedDate, article.PublicationDate);
    }

    [TestMethod]
    public void CompileArticleFromResponses_PublishDateIsMappedFromHx()
    {
        const int year = 2020;
        const int month = 4;
        const int day = 29;
        var expectedDate = new DateTime(year, month, day);

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                PubmedData = new PubmedData
                {
                    History = new PubMedHistory
                    {
                        PubMedPubDates =
                        [
                            new PubMedPubDate
                            {
                                PubStatus = EUtilsConstants.PublishedHxStatusIdentifier,
                                Year = year.ToString(),
                                Month = month.ToString(),
                                Day = day.ToString()
                            }
                        ]
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(expectedDate, article.PublicationDate);
    }

    [TestMethod]
    public void CompileArticleFromResponses_PublishDateIsPreferredOverHx()
    {
        const int year = 2020;
        const int month = 4;
        const int day = 29;
        var expectedDate = new DateTime(year, month, day);
        const int badYear = 1990;

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                PubmedData = new PubmedData
                {
                    History = new PubMedHistory
                    {
                        PubMedPubDates =
                        [
                            new PubMedPubDate
                            {
                                PubStatus = EUtilsConstants.PublishedHxStatusIdentifier,
                                Year = badYear.ToString(),
                                Month = month.ToString(),
                                Day = day.ToString()
                            }
                        ]
                    }
                },
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        ArticleDate = new ArticleDate
                        { Year = year.ToString(), Month = month.ToString(), Day = day.ToString() }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(expectedDate, article.PublicationDate);
    }

    [TestMethod]
    public void CompileArticleFromResponses_BadPublishDatesMapToMinValue()
    {
        const string badYear = "bad year";
        const string badMonth = "bad month";
        const string badDay = "bad day";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                PubmedData = new PubmedData
                {
                    History = new PubMedHistory
                    {
                        PubMedPubDates =
                        [
                            new PubMedPubDate
                            {
                                PubStatus = EUtilsConstants.PublishedHxStatusIdentifier,
                                Year = badYear,
                                Month = badMonth,
                                Day = badDay
                            }
                        ]
                    }
                },
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        ArticleDate = new ArticleDate
                        { Year = badYear, Month = badMonth, Day = badDay }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.AreEqual(DateTime.MinValue, article.PublicationDate);
    }

    [TestMethod]
    public void CompileArticleFromResponses_AuthorIsAreMapped()
    {
        const string authorFirstName = "Mary";
        const string authorLastName = "Smith";
        const string authorInitials = "mes";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        AuthorList = new AuthorList
                        {
                            Authors =
                               [
                                   new Author
                                   {
                                       ForeName = authorFirstName,
                                       LastName = authorLastName,
                                       Initials = authorInitials
                                   }
                               ]
                        }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.IsNotNull(article.AuthorList);
        Assert.AreEqual(1, article.AuthorList.Count);

        var author = article.AuthorList[0];
        Assert.AreEqual(authorFirstName, author.FirstName);
        Assert.AreEqual(authorLastName, author.LastName);
        Assert.AreEqual(authorInitials, author.Initials);
    }

    [TestMethod]
    public void CompileArticleFromResponses_FirstAuthorIsFlaggedAsFirst()
    {
        const string firstAuthorSurname = "first author";
        const string secondAuthorSurname = "second author";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        AuthorList = new AuthorList
                        {
                            Authors =
                            [
                                new Author
                                {
                                    LastName = firstAuthorSurname
                                },
                                new Author
                                {
                                    LastName = secondAuthorSurname
                                }
                            ]
                        }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);
        Assert.IsNotNull(article.AuthorList);
        Assert.AreEqual(2, article.AuthorList.Count);

        foreach (var author in article.AuthorList)
        {
            Assert.AreEqual(author.First ? firstAuthorSurname : secondAuthorSurname, author.LastName);
        }
    }

    [TestMethod]
    public void CompileArticleFromResponses_JournalIsMapped()
    {
        const string journalName = "journal name";
        const string journalVolume = "Iss 42";
        const int journalMonth = 10;
        const int journalYear = 2020;

        var expectedDate = new DateTime(journalYear, journalMonth, 1);

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        Journal = new Journal
                        {
                            DynamicTitle = journalName,
                            JournalIssue = new JournalIssue
                            {
                                PubDate = new PubDate { Month = journalMonth.ToString(), Year = journalYear.ToString() },
                                Volume = journalVolume
                            }
                        }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);

        var publication = article.Publication;
        Assert.IsNotNull(publication);
        Assert.AreEqual(journalName, publication.JournalName);
        Assert.AreEqual(journalVolume, publication.Volume);
        Assert.AreEqual(expectedDate, publication.JournalDate);
    }

    [TestMethod]
    public void CompileArticleFromResponses_BadJournalDateNotMapped()
    {
        const string badMonth = "bad month";
        const string badYear = "bad year";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    Article = new EFetchArticle
                    {
                        Journal = new Journal
                        {
                            JournalIssue = new JournalIssue
                            {
                                PubDate = new PubDate { Month = badMonth, Year = badYear }
                            }
                        }
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);

        var publication = article.Publication;
        Assert.IsNotNull(publication);
        Assert.IsNull(publication.JournalDate);
    }

    [TestMethod]
    public void CompileArticleFromResponse_MinorMeshTermsAreMapped()
    {
        const string minorMeshTerm = "minor term";
        const string minorMeshTermWithQualifier = "minor with qualifier";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    MeshHeadingList = new MeshHeadingList
                    {
                        MeshHeadings = [
                            new MeshHeading
                            {
                                DescriptorName = new DescriptorName
                                {
                                    Name = minorMeshTerm,
                                    MajorTopicYn = "N"
                                }
                            },
                            new MeshHeading
                            {
                                DescriptorName = new DescriptorName
                                {
                                    Name = minorMeshTermWithQualifier,
                                    MajorTopicYn = "N"
                                },
                                QualifierNames =
                                    [
                                        new QualifierName
                                        {
                                            MajorTopicYn = "N"
                                        }
                                    ]
                            }
                        ]
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);

        var minorTerms = article.MinorTopicMeshHeadings;
        Assert.IsNotNull(minorTerms);
        Assert.AreEqual(2, minorTerms.Count);
        Assert.IsTrue(minorTerms.Contains(minorMeshTerm));
        Assert.IsTrue(minorTerms.Contains(minorMeshTermWithQualifier));
    }

    [TestMethod]
    public void CompileArticleFromResponse_MajorMeshTermsAreMapped()
    {
        const string majorTermInDescriptor = "major descriptor term";
        const string majorTermInQualifier = "major qualifier term";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle
            {
                MedlineCitation = new MedlineCitation
                {
                    MeshHeadingList = new MeshHeadingList
                    {
                        MeshHeadings = [
                            new MeshHeading
                            {
                                DescriptorName = new DescriptorName
                                {
                                    Name = majorTermInDescriptor,
                                    MajorTopicYn = "Y"
                                }
                            },
                            new MeshHeading
                            {
                                DescriptorName = new DescriptorName
                                {
                                    Name = majorTermInQualifier,
                                    MajorTopicYn = "N"
                                },
                                QualifierNames = [
                                    new QualifierName
                                    {
                                        MajorTopicYn = "Y"
                                    }
                                ]
                            }
                        ]
                    }
                }
            },
            new ELinkResult(), string.Empty);

        Assert.IsNotNull(article);

        var majorTerms = article.MajorTopicMeshHeadings;
        Assert.IsNotNull(majorTerms);
        Assert.AreEqual(2, majorTerms.Count);
        Assert.IsTrue(majorTerms.Contains(majorTermInDescriptor));
        Assert.IsTrue(majorTerms.Contains(majorTermInQualifier));
    }

    [TestMethod]
    public void CompileArticleFromResponse_CitedCountIsMapped()
    {
        const string linkId1 = "link1";
        const string linkId2 = "link2";

        var article = Utilities.CompileArticleFromResponses(string.Empty,
            new PubmedArticle(),
            new ELinkResult
            {
                LinkSet = new LinkSet
                {
                    LinkSetDb = new LinkSetDb
                    {
                        Links = [
                            new Link { Id = linkId1 },
                            new Link { Id = linkId2 }
                        ]
                    }
                }
            }, string.Empty);

        Assert.IsNotNull(article);
        var links = article.CitedBy;
        Assert.IsNotNull(links);
        Assert.AreEqual(2, links.Count);
        Assert.IsTrue(links.Contains(linkId1));
        Assert.IsTrue(links.Contains(linkId2));
    }

    #endregion CompileArticleFromResponses tests

    private static List<string> GetQueryParams(string queryString)
    {
        return queryString.Split("&").ToList();
    }

    private static string GetQueryParamValue(IEnumerable<string> queryParams, string paramKey)
    {
        var param = queryParams.FirstOrDefault(param => param.Contains(paramKey));

        return string.IsNullOrEmpty(param) ? string.Empty : param[param.IndexOf($"{paramKey}=", StringComparison.Ordinal)..];
    }
}