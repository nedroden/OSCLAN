namespace Osclan.Compiler.Io.Abstractions;

/// <summary>
/// Abstraction layer around IO operations, to allow for compilation without writing to disk. Can be useful
/// in case the compiler is used in a web application, for example.
/// </summary>
public interface IIoService
{
    /// <summary>
    /// Reads a file (e.g. from disk).
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The contents of the file.</returns>
    string Read(string path);

    /// <summary>
    /// Saves an intermediate file to disk, if the compiler is configured to do so.
    /// </summary>
    /// <param name="filename">The name of the file (e.g., simple.osc_tokens.json).</param>
    /// <param name="content">The contents of the file.</param>
    void SaveIntermediateFile(string filename, string content);
}