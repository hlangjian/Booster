using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Booster;

public static class BoosterUtils
{
    public static bool HasAttribute<T>(this Assembly assembly) where T : Attribute
    {
        var attributes = assembly.GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0;
    }

    public static bool HasAttribute<T>(this MemberInfo memberInfo) where T : Attribute
    {
        var attributes = memberInfo.GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0;
    }

    public static bool HasAttribute<T>(this ParameterInfo parameterInfo) where T : Attribute
    {
        var attributes = parameterInfo.GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0;
    }

    public static MethodInfo GetMethod(this Type type, string name, Type[] parameters, Type[]? genericTypes = null)
    {
        var methodWithSameNames = type.GetMethods().Where(o => o.Name == name);
        var methodWithSameParameters = methodWithSameNames
            .Where(o => o.GetParametersWithoutClosure().Count() == parameters.Length)
            .Where(o => o.GetParametersWithoutClosure().Select((o, i) => (o.ParameterType, i)).All(o => o.ParameterType == parameters[o.i]));

        if (genericTypes is null) return methodWithSameParameters.First();

        var method = methodWithSameParameters.First().MakeGenericMethod(genericTypes);
        return method;
    }

    public static T BuildScopeDelegate<T>(this MethodInfo source, Delegate[] converters, IServiceCollection? services = null, IServiceProvider? provider = null, object? instance = null) where T : Delegate
    {
        var targetParameters = typeof(T).GetMethod("Invoke")!.GetParametersWithoutClosure().ToArray();
        var sourceParameters = source.GetParametersWithoutClosure().ToArray();

        var availableConverters = converters
            .Where(o => o.Method.ReturnType != typeof(void))
            .Where(o => o.Method.GetParametersWithoutClosure().Count() == 1)
            .ToArray();

        var targetParamsExps = targetParameters.Select(o => Expression.Parameter(o.ParameterType)).ToArray();
        var sourceParamsExps = new Expression[sourceParameters.Length];

        var getService = services is not null && provider is not null ? provider.GetType().GetMethod(nameof(provider.GetService))! : null;

        for (var sourceIndex = 0; sourceIndex < sourceParameters.Length; sourceIndex++)
        {
            for (var targetIndex = 0; targetIndex < targetParameters.Length; targetIndex++)
            {
                var sourceType = sourceParameters[sourceIndex].ParameterType;
                var targetType = targetParameters[targetIndex].ParameterType;

                if (sourceType.IsAssignableFrom(targetType))
                {
                    sourceParamsExps[sourceIndex] = targetParamsExps[targetIndex];
                    continue;
                }

                var converter = availableConverters
                    .Where(o => sourceType.IsAssignableFrom(o.Method.ReturnType))
                    .Where(o => targetType.IsAssignableTo(o.Method.GetParametersWithoutClosure().First().ParameterType))
                    .FirstOrDefault();

                if (converter is not null)
                {
                    sourceParamsExps[sourceIndex] = Expression.Invoke(Expression.Constant(converter), targetParamsExps[targetIndex]);
                    continue;
                }

                if (services is not null && provider is not null && getService is not null
                    && services.FirstOrDefault(o => o.ServiceType.IsAssignableTo(sourceType)) is ServiceDescriptor descriptor)
                {
                    if (descriptor.Lifetime == ServiceLifetime.Singleton) sourceParamsExps[sourceIndex] = Expression.Constant(provider.GetService(sourceType));
                    else sourceParamsExps[sourceIndex] = Expression.Call(Expression.Constant(provider), getService, Expression.Constant(sourceType));
                    continue;
                }

                if (sourceParameters[sourceIndex].IsOptional || Nullable.GetUnderlyingType(sourceType) is not null)
                {
                    sourceParamsExps[sourceIndex] = Expression.Constant(null);
                    continue;
                }

                throw new Exception($"cannot found type {sourceType}");
            }
        }

        var invokeMethod = typeof(MethodInfo).GetMethod(nameof(MethodInfo.Invoke), [typeof(object), typeof(object[])])!;
        var invoke = Expression.Call(Expression.Constant(source), invokeMethod, Expression.Constant(instance), Expression.NewArrayInit(typeof(object), sourceParamsExps));
        var lambda = Expression.Lambda<T>(invoke, targetParamsExps);
        return lambda.Compile();
    }

    public static T BuildScopeDelegate<T>(this Delegate source, Delegate[] converters, IServiceCollection? services = null, IServiceProvider? provider = null, object? instance = null) where T : Delegate
    {
        return BuildScopeDelegate<T>(source.Method, converters, services, provider, instance);
    }

    public static IEnumerable<ParameterInfo> GetParametersWithoutClosure(this MethodBase method)
    {
        return method.GetParameters().Where(p => p.ToString() != "System.Runtime.CompilerServices.Closure");
    }
}