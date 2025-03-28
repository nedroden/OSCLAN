using System.Diagnostics;

namespace Osclan.Compiler.Native;

/// <summary>
/// Represents the result of a shell command.
/// </summary>
/// <param name="ExitCode">The exit code.</param>
/// <param name="Stdout">The value of stdout.</param>
/// <param name="Stderr">The value of stderr.</param>
public record ShellCommandResult(int ExitCode, string Stdout, string Stderr);

/// <summary>
/// Represents a shell command that can be executed.
/// </summary>
/// <param name="command">The command name.</param>
/// <param name="arguments">The arguments passed.</param>
public class ShellCommand(string command, string arguments)
{
    private readonly string _command = command;
    private readonly string _arguments = arguments;

    /// <summary>
    /// Starts the shell command and returns the result.
    /// </summary>
    /// <returns>The result of the command.</returns>
    public ShellCommandResult Start()
    {
        var info = new ProcessStartInfo
        {
            FileName = _command,
            Arguments = _arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = info;

        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ShellCommandResult(process.ExitCode, stdout, stderr);
    }
}