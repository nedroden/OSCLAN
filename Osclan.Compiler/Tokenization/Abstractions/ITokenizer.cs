using System.Collections.Generic;

namespace Osclan.Compiler.Tokenization.Abstractions;

public interface ITokenizer
{
    List<Token> Tokenize();
}