using CommandLine;

namespace Osclan.Compiler
{
    class Program
    {
        static void Main(string[] args) =>
            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed(o => new Compiler(o).Run());
    }
}