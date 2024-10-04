using System.Diagnostics;

namespace Osclan.Compiler.Native;

public record ShellCommandResult(int ExitCode, string Stdout, string Stderr);

public class ShellCommand
{
    private readonly string _command;
    private readonly string _arguments;

    public ShellCommand(string command, string arguments)
    {
        _command = command;
        _arguments = arguments;
    }

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

        using var process = new Process { StartInfo = info };

        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ShellCommandResult(process.ExitCode, stdout, stderr);
    }
}