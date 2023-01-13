using Microsoft.Extensions.DependencyInjection;

namespace Unity.Services.Cli.Common.Services;

public class ServiceTypesBridge: IServiceProviderFactory<IServiceCollection>, IServiceTypeList
{
    public IReadOnlyList<Type> ServiceTypes =>
        m_ServiceTypes ?? throw new InvalidOperationException("can not get services before provider is built");

    IReadOnlyList<Type>? m_ServiceTypes;

    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        m_ServiceTypes = containerBuilder.Select(d => d.ServiceType).ToList();
        return containerBuilder.BuildServiceProvider();
    }
}
