using System;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Osclan.Analytics.Extensions;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Analysis.Abstractions;
using Osclan.Compiler.Assembler.Abstractions;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation;
using Osclan.Compiler.Generation.Abstractions;
using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Generation.Architecture.AArch64;
using Osclan.Compiler.Io;
using Osclan.Compiler.Io.Abstractions;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization;
using Osclan.Compiler.Tokenization.Abstractions;

namespace Osclan.Compiler;

internal static class Program
{
    private static void Main(string[] args)
    {
        var serviceCollection = new ServiceCollection()
            .LoadConfiguration(args)
            .AddOsclanAnalytics()
            .ConfigureServices();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        try
        {
            serviceProvider.GetRequiredService<Compiler>().Run();
        }
        catch (SourceException e)
        {
            Console.Error.WriteLine($"Compilation error: {e.Message}");
        }
    }

    private static IServiceCollection LoadConfiguration(this IServiceCollection services, string[] args)
    {
        Parser.Default
            .ParseArguments<CompilerOptions>(args)
            .WithParsed(o => services.AddSingleton(o));

        return services;
    }

    private static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenizer, Tokenizer>();
        services.AddScoped<IParser, Parsing.Parser>();
        services.AddScoped<IAnalyzer, Analyzer>();

        services.AddScoped<Emitter>();
        services.AddScoped<IGenerationStrategy, AArch64Strategy>();
        services.AddScoped<IGenerator, Generator>();

        services.AddScoped<IAssembler, Assembler.Assembler>();
        services.AddScoped<IIoService, DiskService>();

        services.AddScoped<Compiler>();

        return services;
    }
}