using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Booster;

public class Application(string[] args)
{
    public WebApplicationBuilder Builder = WebApplication.CreateBuilder(args);

    public async Task StartAsync() => await CreateApplication().StartAsync();
    public void Start() => CreateApplication().Start();

    private readonly List<Verticle> verticles = [];

    public WebApplication CreateApplication()
    {
        var app = Builder.Build();
        return app;
    }

    public Application Use<T>() where T : Verticle
    {
        verticles.Add(Activator.CreateInstance<T>());
        return this;
    }
}