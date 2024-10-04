using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Io;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler;

public class Compiler
{
    private readonly CompilerOptions _options;

    public Compiler(CompilerOptions options) =>
        _options = options;

    public void Run()
    {
        // Clean output directory
        if (Directory.Exists(_options.OutputPath))
        {
            Directory.Delete(_options.OutputPath, true);
        }

        Console.WriteLine("Starting compilation process...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Step 1 - Tokenization
        var tokenizer = new Tokenizer(_options, _options.OutputPath, _options.InputFile, new InputFileReader());
        var tokens = tokenizer.Tokenize();
        SaveIntermediateFile($"{_options.InputFileName}_tokens.json", SerializeState(tokens));

        // Step 2 - Syntactic analysis (parsing)
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        SaveIntermediateFile($"{_options.InputFileName}_ast_pre_analysis.json", SerializeState(ast));

        // Step 3 - Semantic analysis
        var analyzer = new Analyzer(ast);
        ast = analyzer.Analyze();
        SaveIntermediateFile($"{_options.InputFileName}_ast_post_analysis.json", SerializeState(ast));
        SaveIntermediateFile($"{_options.InputFileName}_symbol_tables.json", SerializeState(analyzer.ArchivedSymbolTables));

        // Step 4 - Optimization
        // todo

        // Step 5 - Code generation
        // todo

        stopwatch.Stop();
        Console.WriteLine($"Compilation finished in {stopwatch.ElapsedMilliseconds} ms.");
    }

    private static string SerializeState<T>(T ast) where T : class =>
        JsonSerializer.Serialize(ast, new JsonSerializerOptions { WriteIndented = true });

    private void SaveIntermediateFile(string filename, string content)
    {
        if (!_options.GenerateIntermediateFiles)
        {
            return;
        }

        var path = Path.Combine(_options.OutputPath, filename);
        var dirname = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirname))
        {
            Directory.CreateDirectory(dirname ?? throw new Exception("Could not create directory"));
        }

        File.WriteAllText(path, content);
    }
}