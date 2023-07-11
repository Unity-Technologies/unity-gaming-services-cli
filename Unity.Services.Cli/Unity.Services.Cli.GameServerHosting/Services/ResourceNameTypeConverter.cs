using Unity.Services.Multiplay.Authoring.Core.Assets;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Unity.Services.Cli.GameServerHosting.Services;

sealed class ResourceNameTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(IResourceName).IsAssignableFrom(type);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        var scalar = parser.Current as Scalar;
        parser.MoveNext();

        if (scalar == null)
        {
            throw new InvalidDataException("Failed to retrieve scalar value.");
        }

        var inst = Activator.CreateInstance(type);
        type.GetProperty(nameof(IResourceName.Name))?.SetValue(inst, scalar.Value);

        return inst;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var name = (IResourceName)value!;
        emitter.Emit(new Scalar(null, name.Name));
    }
}
