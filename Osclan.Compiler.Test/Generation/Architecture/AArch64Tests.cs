using System;
using System.Collections.Generic;
using Osclan.Compiler.Generation;
using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;
using Type = Osclan.Compiler.Symbols.Type;

namespace Osclan.Compiler.Test.Generation.Architecture;

public class AArch64Tests
{
    private readonly AArch64Strategy _sut = new(new Emitter());

    #region Memory deallocation
    [Fact]
    public void Test_Memory_Can_Be_Freed()
    {
        var ast = new AstNode
        {
            Type = AstNodeType.Root,
            Children = new List<AstNode>
            {
                new() { Type = AstNodeType.Allocation, TypeInformation = new Type(BuiltInType.String) { SizeInBytes = 5 } }
            }
        };

        _sut.GenerateIl(ast, new Dictionary<Guid, SymbolTable>());
        
        // TODO: Finish this test.
        Assert.True(true);
    }
    #endregion
}