using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Osclan.Analytics;
using Osclan.Analytics.Abstractions;
using Osclan.Compiler.Io.Abstractions;

namespace Osclan.Compiler.Io;

/// <summary>
/// Implements the <see cref="IIoService"/> interface by implementing read and write operations
/// for physical media.
/// </summary>
public class DiskService(CompilerOptions options, IAnalyticsClientFactory analyticsClientFactory) : IIoService
{
    private readonly AnalyticsClient<DiskService> _analyticsClient = analyticsClientFactory.CreateClient<DiskService>();
    
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
        if (!options.GenerateIntermediateFiles)
        {
            return;
        }

        var path = Path.Combine(options.TempFilePath, filename);
        var dirname = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirname))
        {
            Directory.CreateDirectory(dirname ?? throw new IOException("Could not create directory"));
        }

        File.WriteAllText(path, content);
        _analyticsClient.LogEvent($"Intermediate file saved: {path}");
    }

    /// <summary>
    /// Copies the native libraries to the output directory.
    /// </summary>
    /// <param name="path">The output path.</param>
    public void CopyNativeLibs(string path)
    {
        var resourceNames = Array.FindAll(Assembly.GetExecutingAssembly().GetManifestResourceNames(), element => element.Contains("_native.s"));

        foreach (var resourceName in resourceNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                return;
            }

            var streamReader = new StreamReader(stream, Encoding.UTF8);
            var contents = streamReader.ReadToEnd();

            // Get the filename with its extension, e.g., 'aarch64_native.s'
            var targetName = string.Join('.', resourceName.Split('.').Reverse().Take(2).Reverse());

            File.WriteAllText($"{path}/{targetName}", contents);
        }
        
        _analyticsClient.LogEvent($"Native libraries copied to {path}");
    }
}