using Aglomera;
using System.Text;

namespace PubMed.Study.Buddy.Domains.Cluster.Hierarchical.Models;

public class DataPoint : IEquatable<DataPoint>, IDissimilarityMetric<DataPoint>, IComparable<DataPoint>
{
    #region Constructors

    public DataPoint(string id, double[] value)
    {
        this.ID = id;
        this.Value = value;
    }

    public DataPoint()
    {
    }

    #endregion Constructors

    #region Properties & Indexers

    public string ID { get; }

    public double[] Value { get; }

    #endregion Properties & Indexers

    #region Public Methods

    public override bool Equals(object obj) => obj is DataPoint && this.Equals((DataPoint)obj);

    public override int GetHashCode() => this.ID.GetHashCode();

    public override string ToString() => this.ID;

    #endregion Public Methods

    #region Public Methods

    public static DataPoint GetCentroid(Cluster<DataPoint> cluster)
    {
        if (cluster.Count == 1) return cluster.First();

        // gets sum for all variables
        var id = new StringBuilder();
        var sums = new double[cluster.First().Value.Length];
        foreach (var dataPoint in cluster)
        {
            id.Append(dataPoint.ID);
            for (var i = 0; i < sums.Length; i++)
                sums[i] += dataPoint.Value[i];
        }

        // gets average of all variables (centroid)
        for (var i = 0; i < sums.Length; i++)
            sums[i] /= cluster.Count;
        return new DataPoint(id.ToString(), sums);
    }

    public static DataPoint GetMedoid(Cluster<DataPoint> cluster) => cluster.GetMedoid(new DataPoint());

    public static bool operator ==(DataPoint left, DataPoint right) => left.Equals(right);

    public static bool operator !=(DataPoint left, DataPoint right) => !left.Equals(right);

    public double DistanceTo(DataPoint other)
    {
        var sum2 = 0d;
        var length = Math.Min(this.Value.Length, other.Value.Length);
        for (var idx1 = 0; idx1 < length; ++idx1)
        {
            var delta = this.Value[idx1] - other.Value[idx1];
            sum2 += delta * delta;
        }

        return Math.Sqrt(sum2);
    }

    public int CompareTo(DataPoint other) => string.Compare(this.ID, other.ID, StringComparison.Ordinal);

    public double Calculate(DataPoint instance1, DataPoint instance2) => instance1.DistanceTo(instance2);

    public bool Equals(DataPoint other) => string.Equals(this.ID, other.ID);

    #endregion Public Methods
}