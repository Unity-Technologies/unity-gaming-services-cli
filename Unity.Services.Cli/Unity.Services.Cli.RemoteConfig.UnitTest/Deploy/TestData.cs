namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[Serializable]
class TestData : IEquatable<TestData>
{
    public string Name = "";

    public int Number;

    public bool Flag;

    public bool Equals(TestData? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && Number == other.Number
            && Flag == other.Flag;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((TestData)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ Number;
            hashCode = (hashCode * 397) ^ Flag.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(TestData? left, TestData? right) => Equals(left, right);

    public static bool operator !=(TestData? left, TestData? right) => !Equals(left, right);
}
