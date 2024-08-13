namespace Booster;

public partial class BoosterPlugins
{
    public Action<WebApplication> Test(WebApplicationBuilder builder)
    {
        Console.WriteLine("on builder configing");

        return app => Console.WriteLine("on application startup");
    }
}