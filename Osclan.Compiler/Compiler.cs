using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Osclan.Analytics.Abstractions;
using Osclan.Compiler.Analysis.Abstractions;
using Osclan.Compiler.Assembler.Abstractions;
using Osclan.Compiler.Generation.Abstractions;
using Osclan.Compiler.Io.Abstractions;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization.Abstractions;

namespace Osclan.Compiler;

public class Compiler(
    CompilerOptions options,
    ITokenizer tokenizer,
    IParser parser,
    IAnalyzer analyzer,
    IGenerator generator,
    IAssembler assembler,
    IIoService ioService)
{
    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        WriteIndented = true
    };

    public void Run()
    {
        // Clean output directory
        if (Directory.Exists(options.TempFilePath))
        {
            Directory.Delete(options.TempFilePath, true);
        }

        Console.WriteLine("Starting compilation process...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Step 1 - Tokenization
        var tokens = tokenizer.Tokenize();
        ioService.SaveIntermediateFile($"{options.InputFileName}_tokens.json", SerializeState(tokens));

        // Step 2 - Syntactic analysis (parsing)
        var ast = parser.Parse(tokens);
        ioService.SaveIntermediateFile($"{options.InputFileName}_ast_pre_analysis.json", SerializeState(ast));

        // Step 3 - Semantic analysis
        var analyzerResult = analyzer.Analyze(ast);
        ast = analyzerResult.Root;
        ioService.SaveIntermediateFile($"{options.InputFileName}_ast_post_analysis.json", SerializeState(ast));
        ioService.SaveIntermediateFile($"{options.InputFileName}_symbol_tables.json", SerializeState(analyzer.ArchivedSymbolTables));

        // Step 4 - Optimization

        // Step 5 - Code generation
        var il = generator.GenerateIl(ast, analyzerResult.SymbolTables);
        ioService.SaveIntermediateFile($"{options.InputFileName}.s", il);

        // Step 5b - Include native libs
        ioService.CopyNativeLibs(options.TempFilePath);

        // Step 6 - Assembler and linker
        assembler.Assemble(Path.Combine(options.TempFilePath, options.InputFileName.Replace(".s", string.Empty)), options.OutputPath);

        stopwatch.Stop();
        Console.WriteLine($"Compilation finished in {stopwatch.ElapsedMilliseconds} ms.");
    }

    private static string SerializeState<T>(T ast) where T : class =>
        JsonSerializer.Serialize(ast, SerializationOptions);
}