using Newtonsoft.Json;

namespace Unity.Services.Cli.Authoring.Model;

public sealed class ImportExportEntry<T>
{
    public long Id { get; }
    public string Name { get; }
    public T Value { get; }
    public Exception? Exception { get; private set; }

    public ImportExportEntry(long id, string name, T value)
    {
        Id = id;
        Name = name;
        Value = value;
    }

    internal ImportExportItem ToImportExportItem(ImportExportAction action)
    {
        return new ImportExportItem(Name, action, Exception == null);
    }

    public void Fail(Exception e)
    {
        Exception = e;
    }

    public override string ToString()
    {
        return Name;
    }

    bool Equals(ImportExportEntry<T> other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ImportExportEntry<T>)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

