using Booster.Core;

namespace Booster;

public partial class BoosterPlugins
{
    public Action<WebApplication> Test(WebApplicationBuilder builder)
    {
        Console.WriteLine("on builder configing");

        return app => Console.WriteLine("on application startup");
    }
}

public class Test
{
    private readonly string name = "my-name is config";

    [OnConfig]
    public static void OnConfig(IServiceCollection services)
    {
        services.AddSingleton(new A("this is a"));
        services.AddSingleton(new Test());
    }

    [OnStart]
    public static void OnStartup(A a)
    {
        Console.WriteLine($"i got a with name {a.Name}");
    }

    [OnStart]
    public void ScopedStartup(A a)
    {
        Console.WriteLine($"i'am {name}");
    }
}

public record A(string Name);