using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Booster.Core;

public interface IServiceFactoryContainer
{
    ServiceFactory[] ServiceFactories { get; }
}

public class ServiceUnit(ServiceFactory factory, IServiceContainer container)
{
    public Type Type => factory.Type;

    public object? Key => factory.Key;

    public object? Service => GetService();

    private object? instance;

    private bool inited;

    public object? GetService()
    {
        if (inited == false)
        {
            instance = factory.Producer(container);
            inited = true;
        }
        return instance;
    }
}

public interface IServiceContainer
{
    ServiceUnit[] Services { get; }

    T CreateDelegate<T>(Delegate @delegate);

    object? GetService(Type type, object? key = null);
}

public interface IServiceContainerBuilder : IServiceFactoryContainer
{
    void AddServiceFactory(Type type, object? key, Func<IServiceContainer, object?> producer);
}

public record ServiceFactory(Type Type, object? Key, Func<IServiceContainer, object?> Producer);

public record ServiceKey(Type Type, object? Key);

public class ServiceContainerBuilder(ServiceFactory[]? factories = null) : IServiceContainerBuilder
{
    public ServiceFactory[] ServiceFactories => [.. serviceFactories];

    private readonly List<ServiceFactory> serviceFactories = factories?.ToList() ?? [];

    public void AddServiceFactory(Type type, object? key, Func<IServiceContainer, object?> producer)
    {
        serviceFactories.Add(new ServiceFactory(type, key, producer));
    }
}

public class ServiceContainer : IServiceContainer
{
    public ServiceUnit[] Services { get; }

    public ServiceContainer(params IServiceContainer[] collections) : this(new ServiceContainerBuilder(), collections) { }

    public ServiceContainer(IServiceFactoryContainer factories, params IServiceContainer[] collections)
    {
        var serviceKeys = new HashSet<ServiceKey>();
        var servicesList = new List<ServiceUnit>();

        ServiceUnit[] units = [
            ..factories.ServiceFactories.Select(o => new ServiceUnit(o, this)).OrderBy(o => o.Type, TypeComparer.Default),
            .. collections.SelectMany(o => o.Services.OrderBy(o => o.Type, TypeComparer.Default))
        ];

        foreach (var unit in units.Reverse())
        {
            if (serviceKeys.Contains(new ServiceKey(unit.Type, unit.Key))) continue;
            servicesList.Add(unit);
        }

        Services = [.. servicesList];
    }

    public T CreateDelegate<T>(Delegate @delegate)
    {
        var source = @delegate.GetMethodInfo();

        var targetParameters = typeof(T).GetMethod("Invoke")!.GetParameterDescriptors();
        var sourceParameters = source.GetParameterDescriptors();

        var targetParamsExps = targetParameters.Select(o => Expression.Parameter(o.Type)).ToArray();

        var parameterFactories = sourceParameters.Select<ParameterDescriptor, Func<object?>>(o => () => GetService(o.Type, o.Key)).ToArray();

        for (var i = 0; i < parameterFactories.Length; i++)
        {
            if (parameterFactories[i] is null)
            {
                var parameter = sourceParameters[i];
                if (targetParameters.Any(o => parameter.Type.IsAssignableFrom(o.Type) && o.Key == parameter.Key)) continue;
                throw new ParameterProviderNotFoundException(parameter.Info);
            }
        }

        var sourceParamsExps = sourceParameters.Select<ParameterDescriptor, Expression>((o, i) =>
        {
            var index = targetParameters.FindIndex(p => o.Type.IsAssignableFrom(p.Type));
            if (index != -1) return targetParamsExps[index];

            var factory = Expression.Constant(parameterFactories[i]);
            var key = Expression.Constant(o.Key);
            return Expression.Convert(Expression.Invoke(factory), sourceParameters[i].Type);
        });

        var invoke = Expression.Invoke(Expression.Constant(@delegate), sourceParamsExps);
        return Expression.Lambda<T>(invoke, targetParamsExps).Compile();
    }

    public object? GetService(Type type, object? key = null)
    {
        return Services.Where(o =>
        {
            if (type.IsAssignableFrom(o.Type) == false) return false;
            if (o.Key is not null && key is not null) return o.Key.Equals(key);
            if (o.Key is null && key is null) return true;
            return false;
        })
        .FirstOrDefault()?.Service;
    }
}

public class SingletonServiceFactoryContainer : IServiceContainer
{

    public ServiceUnit[] Services { get; }

    private readonly ServiceContainer container;

    public SingletonServiceFactoryContainer(IServiceCollection services, IServiceProvider provider)
    {
        var factories = services
            .Where(o => o.Lifetime == ServiceLifetime.Singleton)
            .Select(service => new ServiceFactory(service.ServiceType, service.ServiceKey, _ => provider.GetService(service.ServiceType)))
            .ToArray();

        var containerBuilder = new ServiceContainerBuilder(factories);
        container = new ServiceContainer(containerBuilder);
        Services = container.Services;
    }

    public T CreateDelegate<T>(Delegate @delegate) => container.CreateDelegate<T>(@delegate);

    public object? GetService(Type type, object? key = null) => container.GetService(type, key);
}

public class ScopedServiceFactoryContainer : IServiceContainer
{
    public ServiceUnit[] Services { get; }

    private readonly ServiceContainer container;

    public ScopedServiceFactoryContainer(IServiceCollection services, IServiceProvider provider)
    {
        var factories = services
            .Where(o => o.Lifetime == ServiceLifetime.Scoped)
            .Select(service => new ServiceFactory(service.ServiceType, service.ServiceKey, _ => provider.GetService(service.ServiceType)))
            .ToArray();

        var containerBuilder = new ServiceContainerBuilder(factories);
        container = new ServiceContainer(containerBuilder);
        Services = container.Services;
    }

    public T CreateDelegate<T>(Delegate @delegate) => container.CreateDelegate<T>(@delegate);

    public object? GetService(Type type, object? key = null) => container.GetService(type, key);
}

public class TransientServiceFactoryContainer : IServiceContainer
{
    public ServiceUnit[] Services { get; }

    private readonly ServiceContainer container;

    public TransientServiceFactoryContainer(IServiceCollection services, IServiceProvider provider)
    {
        var factories = services
            .Where(o => o.Lifetime == ServiceLifetime.Transient)
            .Select(service => new ServiceFactory(service.ServiceType, service.ServiceKey, _ => provider.GetService(service.ServiceType)))
            .ToArray();

        var containerBuilder = new ServiceContainerBuilder(factories);
        container = new ServiceContainer(containerBuilder);
        Services = container.Services;
    }

    public T CreateDelegate<T>(Delegate @delegate) => container.CreateDelegate<T>(@delegate);

    public object? GetService(Type type, object? key = null) => container.GetService(type, key);
}

public class TypeComparer : IComparer<Type>
{
    public static TypeComparer Default { get; } = new TypeComparer();

    public int Compare(Type? x, Type? y)
    {
        if (x == null || y == null) return 0;
        if (x == y) return 0;
        if (x.IsAssignableFrom(y)) return 1;
        if (y.IsAssignableFrom(x)) return -1;
        return 0;
    }
}