using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Analysis.Abstractions;
using Osclan.Compiler.Assembler.Abstractions;
using Osclan.Compiler.Generation;
using Osclan.Compiler.Generation.Abstractions;
using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Io;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization;
using Osclan.Compiler.Tokenization.Abstractions;

namespace Osclan.Compiler;

public class Compiler
{
    private readonly CompilerOptions _options;

    private readonly ITokenizer _tokenizer;
    private readonly IParser _parser;
    private readonly IAnalyzer _analyzer;
    private readonly IGenerator _generator;
    private readonly IAssembler _assembler;

    public Compiler(CompilerOptions options) : this(options,
        new Tokenizer(options, options.TempFilePath, options.InputFile, new InputFileReader()),
        new Parser(),
        new Analyzer(),
        new Generator(new AArch64Strategy(new Emitter())),
        new Assembler.Assembler())
    { }

    public Compiler(
        CompilerOptions options, ITokenizer tokenizer, IParser parser,
        IAnalyzer analyzer, IGenerator generator, IAssembler assembler)
    {
        _options = options;
        _tokenizer = tokenizer;
        _parser = parser;
        _analyzer = analyzer;
        _generator = generator;
        _assembler = assembler;
    }

    public void Run()
    {
        // Clean output directory
        if (Directory.Exists(_options.TempFilePath))
        {
            Directory.Delete(_options.TempFilePath, true);
        }

        Console.WriteLine("Starting compilation process...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Step 1 - Tokenization
        var tokens = _tokenizer.Tokenize();
        SaveIntermediateFile($"{_options.InputFileName}_tokens.json", SerializeState(tokens));

        // Step 2 - Syntactic analysis (parsing)
        var ast = _parser.Parse(tokens);
        SaveIntermediateFile($"{_options.InputFileName}_ast_pre_analysis.json", SerializeState(ast));

        // Step 3 - Semantic analysis
        ast = _analyzer.Analyze(ast);
        SaveIntermediateFile($"{_options.InputFileName}_ast_post_analysis.json", SerializeState(ast));
        SaveIntermediateFile($"{_options.InputFileName}_symbol_tables.json", SerializeState(_analyzer.ArchivedSymbolTables));

        // Step 4 - Optimization
        // todo

        // Step 5 - Code generation
        var il = _generator.GenerateIl(ast);
        SaveIntermediateFile($"{_options.InputFileName}.s", il);

        // Step 6 - Assembler and linker
        _assembler.Assemble(Path.Combine(_options.TempFilePath, _options.InputFileName.Replace(".s", string.Empty)), _options.OutputPath);

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

        var path = Path.Combine(_options.TempFilePath, filename);
        var dirname = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirname))
        {
            Directory.CreateDirectory(dirname ?? throw new Exception("Could not create directory"));
        }

        File.WriteAllText(path, content);
    }
}