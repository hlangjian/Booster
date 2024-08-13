namespace Booster.Core;

public static class ServiceContainerBuilderExtension
{
    public static void AddKeyedService(this IServiceContainerBuilder builder, Type type, object? key, Func<IServiceContainer, object?> producer)
    {
        builder.AddServiceFactory(type, key, producer);
    }

    public static void AddKeyedService(this IServiceContainerBuilder builder, Type type, object? key, object? service = null)
    {
        builder.AddKeyedService(type, key, container => service);
    }

    public static void AddKeyedService<T>(this IServiceContainerBuilder builder, object? key, Func<IServiceContainer, object?> producer)
    {
        builder.AddKeyedService(typeof(T), key, producer);
    }

    public static void AddKeyedService<T>(this IServiceContainerBuilder builder, object? key, object? service = null)
    {
        builder.AddKeyedService(typeof(T), key, service);
    }

    public static void AddKeyedService<TInterface, TImplement>(this IServiceContainerBuilder builder, object? key, Func<IServiceContainer, TImplement> producer)
        where TImplement : TInterface
    {
        builder.AddKeyedService(typeof(TInterface), key, producer);
    }

    public static void AddService(this IServiceContainerBuilder builder, Type type, Func<IServiceContainer, object?> producer)
    {
        builder.AddServiceFactory(type, null, producer);
    }

    public static void AddService(this IServiceContainerBuilder builder, Type type, object? service = null)
    {
        builder.AddService(type, container => service);
    }

    public static void AddService<T>(this IServiceContainerBuilder builder, Func<IServiceContainer, object?> producer)
    {
        builder.AddService(typeof(T), producer);
    }

    public static void AddService<T>(this IServiceContainerBuilder builder, object? service = null)
    {
        builder.AddService(typeof(T), service);
    }

    public static void AddService<TInterface, TImplement>(this IServiceContainerBuilder builder, Func<IServiceContainer, TImplement> producer)
        where TImplement : TInterface
    {
        builder.AddService(typeof(TInterface), producer);
    }
}