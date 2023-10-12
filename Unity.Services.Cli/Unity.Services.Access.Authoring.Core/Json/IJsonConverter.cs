namespace Unity.Services.Access.Authoring.Core.Json
{
    public interface IJsonConverter
    {
        T DeserializeObject<T>(string value, bool matchCamelCaseFieldName = false);
        string SerializeObject<T>(T obj);
    }
}
