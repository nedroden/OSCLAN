using System.Collections.Generic;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Parsing.Abstractions;

public interface IParser
{
    AstNode Parse(List<Token> tokens);
}