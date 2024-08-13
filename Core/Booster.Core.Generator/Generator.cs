using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            (node, token) => node is MethodDeclarationSyntax method && method.Body != null,
            (ctx, token) =>
            {
                var method = ctx.TargetSymbol as IMethodSymbol;
                return new FunctionInfo(method!.Name, method.ContainingType.ToDisplayString(), method.IsStatic);
            }
        );

        var onStartups = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Booster.Core.OnStartAttribute",
            (node, token) => node is MethodDeclarationSyntax method && method.Body != null,
            (ctx, token) =>
            {
                var method = ctx.TargetSymbol as IMethodSymbol;
                return new FunctionInfo(method!.Name, method.ContainingType.ToDisplayString(), method.IsStatic);
            }
        );

        context.RegisterSourceOutput(onConfigs.Collect().Combine(onStartups.Collect()), (ctx, sources) =>
        {
            var (onConfigs, onStartups) = sources;

            var startups = onStartups.Select(o =>
            {
                if (o.IsStatic) return $"{o.TypeName}.{o.Name}";
                return $"app.Services.GetRequiredKeyedService<{o.TypeName}>(null).{o.Name}";
            }).ToArray();

            ctx.AddSource("CorePlugin.g.cs", SourceText.From(
                $$"""
                using Microsoft.AspNetCore.Builder;
                using Booster.Core;

                namespace Booster;

                public partial class BoosterPlugins {

                    public Action<WebApplication> CorePlugin(WebApplicationBuilder builder){

                        var boosterOnConfig = new BoosterOnConfig(builder);
                        boosterOnConfig.Run({{string.Join(",", onConfigs.Select(o => $"{o.TypeName}.{o.Name}"))}});

                        return app => {
                            var boosterOnStartup = new BoosterOnStartup(builder.Services, app);
                            boosterOnStartup.Run({{string.Join(",", startups)}});
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

public record FunctionInfo(string Name, string TypeName, bool IsStatic);