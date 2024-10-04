using System.IO;

namespace Osclan.Compiler.Io;

public interface IInputFileReader
{
    string Read(string path);
}

public class InputFileReader : IInputFileReader
{
    public string Read(string path) =>
        File.ReadAllText(path);
}