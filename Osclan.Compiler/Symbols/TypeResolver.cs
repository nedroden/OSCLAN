using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Symbols;

// TODO: finish this (sep 29)
public class TypeResolver
{
    public Type GetType(SymbolTable symbolTable, AstNode node)
    {
        var type = new Type()
        {
            Name = node.RawType?.Name ?? string.Empty,
            UnmangledName = node.RawType?.Name ?? string.Empty,
            SizeInBytes = 0
        };

        if (string.IsNullOrWhiteSpace(type.Name))
        {
            if (node.Type != AstNodeType.Structure)
            {
                throw new CompilerException("Type name cannot be empty.");
            }

            // Since this is the type itself, use its node's value as the name
            type.Name = node.Value ?? string.Empty;
        }

        foreach (var child in node.Children)
        {
            var name = child.Value ?? throw new CompilerException("Child name cannot be empty");

            // Primitive types
            if (child.Type != AstNodeType.Structure && child.RawType?.Name != "struct")
            {
                var childType = child.RawType?.Name ?? throw new CompilerException("Type name cannot be empty.");
                var elementaryType = symbolTable.ResolveType(childType);
                elementaryType.SizeInBytes = child.RawType.Size;

                type.Fields.Add(Mangler.Mangle(name), elementaryType);
                type.SizeInBytes += child.RawType?.Size ?? elementaryType.SizeInBytes;

                continue;
            }

            var compositeChildType = GetType(symbolTable, child);
            type.Fields.Add(Mangler.Mangle(name), compositeChildType);

            type.IsArray = child.RawType is { Size: > 1 };

            // TODO: check if this works properly. Since it might not.
            type.SizeInBytes += type.IsArray
                ? child.RawType!.Size * compositeChildType.SizeInBytes
                : compositeChildType.SizeInBytes;
        }

        return type;
    }
}