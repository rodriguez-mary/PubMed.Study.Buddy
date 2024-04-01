using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

internal class Utilities
{
    private readonly Dictionary<string, MeshTerm> _termsByTreeNumber = [];

    public Utilities(IReadOnlyDictionary<string, MeshTerm> meshTerms)
    {
        foreach (var meshTerm in meshTerms.Values)
        {
            foreach (var treeNumber in meshTerm.TreeNumber)
            {
                _termsByTreeNumber.TryAdd(treeNumber, meshTerm);
            }
        }
    }

    /// <summary>
    /// Determine the title based on the article mesh terms
    /// </summary>
    public string DetermineClusterName(List<Article> articles)
    {
        var treeNumbers = GetSharedMeshTreeNumbers(articles);

        // map the tree number to terms
        var terms = new List<string>();
        foreach (var treeNumber in treeNumbers)
        {
            if (_termsByTreeNumber.TryGetValue(treeNumber, out var meshTerm) && !terms.Contains(meshTerm.DescriptorName))
                terms.Add(meshTerm.DescriptorName);
        }

        return string.Join(";", terms);
    }

    private static HashSet<string> GetSharedMeshTreeNumbers(List<Article> articles)
    {
        var articleMeshTerms = new List<List<string>>();

        // get the lowest common denominator for all the mesh terms
        // we can use the decimals, as that represents its place in the taxonomy
        foreach (var article in articles)
        {
            if (article.MajorTopicMeshHeadings == null) continue;

            var meshTerms = new List<string>();

            foreach (var meshHeading in article.MajorTopicMeshHeadings)
            {
                foreach (var treeNumber in meshHeading.TreeNumber)
                {
                    var decimals = treeNumber.Split(".");
                    for (var i = 0; i < decimals.Length; i++)
                    {
                        var dec = decimals[..(i + 1)];
                        meshTerms.Add(string.Join(".", dec));
                    }
                }
            }

            articleMeshTerms.Add(meshTerms);
        }

        // at this point we have a dictionary of each article's different mesh term paths
        // now we need to get the intersection of all those lists and collapse the values
        var sharedTerms = Intersect(articleMeshTerms);
        return Collapse(sharedTerms);
    }

    /// <summary>
    /// Returns a list of values that are shared between the lists.
    /// </summary>
    private static HashSet<string> Intersect(IReadOnlyCollection<List<string>> listOfLists)
    {
        var sharedValues = new HashSet<string>(listOfLists.First());

        foreach (var list in listOfLists.Skip(1))
        {
            sharedValues.IntersectWith(list);
        }

        return sharedValues;
    }

    /// <summary>
    /// Collapse the list of mesh term paths so that it only contains the longest path of any term.
    /// Ex: if the path contains 1.2.3 and 1.2, then only 1.2.3 will be returned as 1.2 is fully contained within 1.2.3
    /// </summary>
    private static HashSet<string> Collapse(HashSet<string> meshTermPaths)
    {
        var collapsedValues = new HashSet<string>(meshTermPaths);

        foreach (var termToFind in meshTermPaths)
        {
            foreach (var termToSearch in meshTermPaths)
            {
                // if termToFind is a substring get it outta here
                if (termToFind == termToSearch || !termToSearch.Contains(termToFind)) continue;

                collapsedValues.Remove(termToFind);
                break; // it's been removed, so no more need to search for it in other terms
            }
        }

        return collapsedValues;
    }

    // cluster data

    // generate a set of flashcards proportional to each articles impact score in its cluster
    // the impact score should be normed in the cluster--so if there is only one article in the cluster
    // it should get a lot of flashcards even if the impact score is low
}