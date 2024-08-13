using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Booster.Core;

public static class ReflectionExtension
{
    public static ParameterDescriptor GetDescriptor(this ParameterInfo parameter)
    {
        var key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
        var nullable = Nullable.GetUnderlyingType(parameter.ParameterType) != null;
        return new ParameterDescriptor(parameter.ParameterType, key, nullable, parameter);
    }

    public static List<ParameterDescriptor> GetParameterDescriptors(this MethodInfo method)
    {
        return method.GetParameters()
            .Where(p => p.ToString() != "System.Runtime.CompilerServices.Closure")
            .Select(o => o.GetDescriptor())
            .ToList();
    }
}

public record ParameterDescriptor(Type Type, object? Key, bool Nullable, ParameterInfo Info);