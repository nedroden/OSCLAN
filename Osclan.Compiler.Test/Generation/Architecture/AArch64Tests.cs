using System;
using System.Collections.Generic;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation;
using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;
using Type = Osclan.Compiler.Symbols.Type;

namespace Osclan.Compiler.Test.Generation.Architecture;

public class AArch64Tests
{
    // private readonly AArch64Strategy _sut = new(new Emitter());
    //
    // #region Memory deallocation
    // [Fact]
    // public void Test_Memory_Can_Be_Freed()
    // {
    //     var ast = new AstNode
    //     {
    //         Type = AstNodeType.Root,
    //         Children = new List<AstNode>
    //         {
    //             new() { Value = "some-var", Type = AstNodeType.Variable, TypeInformation = new Type(BuiltInType.String) { SizeInBytes = 5 } },
    //             new() { Value = "some-var", Type = AstNodeType.Deallocation }
    //         }
    //     };
    //
    //     var il = _sut.GenerateIl(ast, new Dictionary<Guid, SymbolTable>());
    //
    //     // Assert that the IL contains the munmap syscall (= 73)
    //     Assert.Contains("x16, #73", il);
    // }
    //
    // [Fact]
    // public void Test_Non_Pointer_Cannot_Be_Used_As_Operand()
    // {
    //     var variable = new AstNode { Value = "some-var", Type = AstNodeType.Variable, TypeInformation = new Type(BuiltInType.Uint) };
    //     var ast = new AstNode
    //     {
    //         Type = AstNodeType.Root,
    //         Children = new List<AstNode>
    //         {
    //             variable,
    //             new() { Value = "some-var", Type = AstNodeType.Deallocation }
    //         }
    //     };
    //
    //     var symbolTable = new SymbolTable(0);
    //     symbolTable.AddVariable(variable);
    //
    //     Assert.Throws<SourceException>(() => _sut.GenerateIl(ast, new Dictionary<Guid, SymbolTable>()));
    // }
    // #endregion
}