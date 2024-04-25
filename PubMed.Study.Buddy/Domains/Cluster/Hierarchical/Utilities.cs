using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical;

internal class Utilities
{
    private readonly Dictionary<string, MeshTerm> _termsByTreeNumber = [];

    public Utilities(IReadOnlyDictionary<string, MeshTerm> meshTerms)
    {
        foreach (var meshTerm in meshTerms.Values)
        {
            foreach (var treeNumber in meshTerm.TreeNumbers)
            {
                _termsByTreeNumber.TryAdd(treeNumber, meshTerm);
            }
        }
    }

    #region functions for generating mesh term distance matrix

    private const int MaxDistance = 1000;

    /// <summary>
    /// This creates a preprocessed distance matrix with all the mesh terms we care about.
    /// </summary>
    public static int[,] CreateDistanceMatrix(IReadOnlyList<MeshTerm> meshTerms)
    {
        var size = meshTerms.Count;
        var matrix = new int[size, size];

        for (var i = 0; i < meshTerms.Count; i++)
        {
            for (var j = i + 1; j < meshTerms.Count; j++)
            {
                var distance = MinimumDistance(meshTerms[i].TreeNumbers, meshTerms[j].TreeNumbers);
                matrix[i, j] = distance;
                matrix[j, i] = distance;
            }
        }

        return matrix;
    }

    /// <summary>
    /// Determines the minimum distance from one list of mesh terms to another list of mesh terms.
    /// This should represent the FEWEST steps needed to traverse from any one member of one group
    /// to any one member of the other group.
    /// </summary>
    private static int MinimumDistance(IReadOnlyList<string> treeNumbers1, IReadOnlyList<string> treeNumbers2)
    {
        var minDistance = MaxDistance;

        for (var i = 0; i < treeNumbers1.Count; i++)
        {
            for (var j = 0; j < treeNumbers2.Count; j++)
            {
                var distance = Distance(treeNumbers1[i], treeNumbers2[j]);
                if (distance < minDistance) minDistance = distance;
                if (minDistance == 0) break;
            }
            if (minDistance == 0) break;
        }

        return minDistance;
    }

    /// <summary>
    /// Calculates the distance from one Mesh term to another.
    /// </summary>
    private static int Distance(string treeNumber1, string treeNumber2)
    {
        var decimals1 = treeNumber1.Split(".");
        var decimals2 = treeNumber2.Split(".");

        var maxLength = Math.Max(decimals1.Length, decimals2.Length);

        Array.Resize(ref decimals1, maxLength);
        Array.Resize(ref decimals2, maxLength);

        var distanceCount = MaxDistance;
        for (var i = 0; i < maxLength; i++)
        {
            // we march through the decimals until we find one that doesn't match
            if (decimals1[i] == decimals2[i]) continue;

            // if it doesn't match then the distance to traverse the tree is the number of
            // decimals that are off because each decimal divider represents another branch of the tree
            // unless we're at the first decimal--that is the root of the tree. if they don't match then
            // the terms are in entirely different trees
            if (i != 0) distanceCount = maxLength - i;
            break;
        }

        return distanceCount;
    }

    #endregion functions for generating mesh term distance matrix

    #region functions to generate a name for article difference-matrix clustered articles

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
                foreach (var treeNumber in meshHeading.TreeNumbers)
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

    #endregion functions to generate a name for article difference-matrix clustered articles
}