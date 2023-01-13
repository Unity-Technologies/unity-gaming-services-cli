namespace Unity.Services.Cli.Common.Services;

public interface IServiceTypeList
{
    IReadOnlyList<Type> ServiceTypes { get; }
}
