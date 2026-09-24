using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

/// <summary>
/// Converts schedule entries between a list of <see cref="SchedulerEntry"/> objects and a JSON object keyed by schedule name.
/// </summary>
class ScheduleEntriesConverter : JsonConverter<IReadOnlyList<SchedulerEntry>>
{
    public override IReadOnlyList<SchedulerEntry> ReadJson(
        JsonReader reader,
        Type objectType,
        IReadOnlyList<SchedulerEntry>? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var entries = new List<SchedulerEntry>();
        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonSerializationException("Schedule configs must be a JSON object.");

        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
        {
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonSerializationException("Schedule configs must contain named JSON objects.");

            var name = (string)reader.Value!;
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Schedule '{name}' must be a JSON object.");

            var entry = serializer.Deserialize<SchedulerEntry>(reader)
                ?? throw new JsonSerializationException($"Schedule '{name}' must be a JSON object.");
            entry.Name = name;
            entries.Add(entry);
        }

        if (reader.TokenType != JsonToken.EndObject)
            throw new JsonSerializationException("Unexpected end while reading schedule configs.");

        return entries;
    }

    public override void WriteJson(
        JsonWriter writer,
        IReadOnlyList<SchedulerEntry>? value,
        JsonSerializer serializer)
    {
        var configs = new JObject();
        foreach (var entry in value ?? Array.Empty<SchedulerEntry>())
        {
            if (entry is null)
                throw new JsonSerializationException("A schedule cannot be null");
            if (string.IsNullOrWhiteSpace(entry.Name))
                throw new JsonSerializationException("A schedule must have a name.");
            if (configs.ContainsKey(entry.Name))
                throw new JsonSerializationException($"Schedule name '{entry.Name}' is duplicated.");

            var body = JObject.FromObject(entry, serializer);
            body.Remove(nameof(SchedulerEntry.Id));
            body.Remove(nameof(SchedulerEntry.Name));
            configs.Add(entry.Name, body);
        }

        configs.WriteTo(writer);
    }
}
