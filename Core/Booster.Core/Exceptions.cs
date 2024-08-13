using System.Reflection;

namespace Booster.Core;

public class ServiceFactoryNotFoundException(Type type, object? key)
    : Exception($"Service {type} with key {key ?? "no-key"} not found");

public class IncompatibleServiceTypeException(Type requiredType, Type resolvedType)
    : Exception($"Requied service type {requiredType} but got {resolvedType}");

public class IncompatibleParameterTypeException(Type requiredType, Type resolvedType)
    : Exception($"Requied paramter type {requiredType} but got {resolvedType}");

public class ParameterProviderNotFoundException(ParameterInfo parameter)
    : Exception($"Cannot found parameter \"{parameter.Name}\" provider");