using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;


namespace Booster;

public abstract class Verticle
{
    public virtual void UseBuilder(IHostApplicationBuilder builder) { }
    public virtual void UseApplication(WebApplication app) { }
    public virtual int Priority() => BuildInPriority.Application;

    public static class BuildInPriority
    {
        public readonly static int Application = 0;
        public readonly static int Infra = 50;
    }
}