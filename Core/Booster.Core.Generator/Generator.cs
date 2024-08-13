using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Booster.Core.Generator;

[Generator(LanguageNames.CSharp)]
public class Class1 : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GeneratePluginListFile);
        context.RegisterPostInitializationOutput(GenerateEntryFile);

        var onConfigs = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Booster.Core.OnConfigAttribute",
            (node, token) => true,
            (ctx, token) => ctx.TargetSymbol
        );

        var onStartups = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Booster.Core.OnStartAttribute",
            (node, token) => true,
            (ctx, token) => ctx.TargetSymbol
        );

        context.RegisterSourceOutput(onConfigs.Collect().Combine(onStartups.Collect()), (ctx, sources) =>
        {
            var (onConfigs, onStartups) = sources;

            ctx.AddSource("CorePlugin.g.cs", SourceText.From(
                $$"""
                using Microsoft.AspNetCore.Builder;

                namespace Booster;

                public partial class BoosterPlugins {

                    public Action<WebApplication> CorePlugin(WebApplicationBuilder builder){
                        Action<WebApplicationBuilder>[] onConfigHooks = [{{string.Join(",", onConfigs.Select(o => $"{o.ContainingType.ToDisplayString()}.{o.Name}"))}}];
                        foreach(var hook in onConfigHooks) hook(builder);
                        return app => {
                            Action<WebApplication>[] onStartupHooks = [{{string.Join(",", onStartups.Select(o => $"{o.ContainingType.ToDisplayString()}.{o.Name}"))}}];
                            foreach(var hook in onStartupHooks) hook(app);
                        };
                    }
                }
                """,
                Encoding.UTF8
            ));
        });
    }

    private void GeneratePluginListFile(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource("BoosterPlugins.g.cs", SourceText.From(
            $$"""
            using Microsoft.AspNetCore.Builder;
            using System.Diagnostics.CodeAnalysis;
            using System.Reflection;

            namespace Booster;
            
            public partial class BoosterPlugins {
                
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
                public Action<WebApplication>[] UseBuilder(WebApplicationBuilder builder) =>
                    GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(o => o.Name != "UseBuilder")
                    .Select(o => Delegate.CreateDelegate(typeof(Func<WebApplicationBuilder, Action<WebApplication>>), null, o))
                    .Cast<Func<WebApplicationBuilder, Action<WebApplication>>>()
                    .Select(plugin => plugin(builder))
                    .ToArray();
            }
            """,
            Encoding.UTF8
        ));
    }

    private void GenerateEntryFile(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource("Program.g.cs", SourceText.From(
            """
            using Booster;
            using Microsoft.AspNetCore.Builder;

            var builder = WebApplication.CreateBuilder(args);
            var plugins = new BoosterPlugins();
            var hooks = plugins.UseBuilder(builder);

            var app = builder.Build();
            foreach(var hook in hooks) hook(app);
            app.Run();

            public partial class Program {}
            """,
            Encoding.UTF8
        ));
    }
}