using System.IO;
using Osclan.Compiler.Io.Abstractions;

namespace Osclan.Compiler.Io;

/// <summary>
/// Implements the <see cref="IIoService"/> interface by implementing read and write operations
/// for physical media.
/// </summary>
public class DiskService : IIoService
{
    private readonly CompilerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskService"/> class.
    /// </summary>
    /// <param name="options">The compilation options.</param>
    public DiskService(CompilerOptions options) =>
        _options = options;

    /// <summary>
    /// Reads a file from disk.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The contents of the file.</returns>
    public string Read(string path) =>
        File.ReadAllText(path);

    /// <summary>
    /// Writes an intermediate result file (e.g., tokens) to disk.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="content">The contents of the file.</param>
    /// <exception cref="IOException">Thrown when the directory name could not be determined.</exception>
    public void SaveIntermediateFile(string filename, string content)
    {
        if (!_options.GenerateIntermediateFiles)
        {
            return;
        }

        var path = Path.Combine(_options.TempFilePath, filename);
        var dirname = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirname))
        {
            Directory.CreateDirectory(dirname ?? throw new IOException("Could not create directory"));
        }

        File.WriteAllText(path, content);
    }
}