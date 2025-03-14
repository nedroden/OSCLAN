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
using Osclan.Compiler.Io.Abstractions;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization;
using Osclan.Compiler.Tokenization.Abstractions;

namespace Osclan.Compiler;

public class Compiler
{
    private readonly CompilerOptions _options;

    private readonly IIoService _ioService;

    private readonly ITokenizer _tokenizer;
    private readonly IParser _parser;
    private readonly IAnalyzer _analyzer;
    private readonly IGenerator _generator;
    private readonly IAssembler _assembler;

    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        WriteIndented = true
    };

    public Compiler(CompilerOptions options) : this(options, new DiskService(options)) { }

    public Compiler(CompilerOptions options, IIoService ioService) : this(options,
        new Tokenizer(options, options.TempFilePath, options.InputFile, ioService),
        new Parser(),
        new Analyzer(),
        new Generator(new AArch64Strategy(new Emitter())),
        new Assembler.Assembler(),
        ioService)
    { }

    public Compiler(
        CompilerOptions options, ITokenizer tokenizer, IParser parser,
        IAnalyzer analyzer, IGenerator generator, IAssembler assembler,
        IIoService ioService)
    {
        _options = options;
        _tokenizer = tokenizer;
        _parser = parser;
        _analyzer = analyzer;
        _generator = generator;
        _assembler = assembler;

        _ioService = ioService;
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
        _ioService.SaveIntermediateFile($"{_options.InputFileName}_tokens.json", SerializeState(tokens));

        // Step 2 - Syntactic analysis (parsing)
        var ast = _parser.Parse(tokens);
        _ioService.SaveIntermediateFile($"{_options.InputFileName}_ast_pre_analysis.json", SerializeState(ast));

        // Step 3 - Semantic analysis
        var analyzerResult = _analyzer.Analyze(ast);
        ast = analyzerResult.Root;
        _ioService.SaveIntermediateFile($"{_options.InputFileName}_ast_post_analysis.json", SerializeState(ast));
        _ioService.SaveIntermediateFile($"{_options.InputFileName}_symbol_tables.json", SerializeState(_analyzer.ArchivedSymbolTables));

        // Step 4 - Optimization

        // Step 5 - Code generation
        var il = _generator.GenerateIl(ast, analyzerResult.SymbolTables);
        _ioService.SaveIntermediateFile($"{_options.InputFileName}.s", il);

        // Step 5b - Include native libs
        _ioService.CopyNativeLibs(_options.TempFilePath);

        // Step 6 - Assembler and linker
        _assembler.Assemble(Path.Combine(_options.TempFilePath, _options.InputFileName.Replace(".s", string.Empty)), _options.OutputPath);

        stopwatch.Stop();
        Console.WriteLine($"Compilation finished in {stopwatch.ElapsedMilliseconds} ms.");
    }

    private static string SerializeState<T>(T ast) where T : class =>
        JsonSerializer.Serialize(ast, SerializationOptions);
}