namespace Booster.Core;

[AttributeUsage(AttributeTargets.Method)]
public class OnConfigAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public class OnStartAttribute : Attribute;