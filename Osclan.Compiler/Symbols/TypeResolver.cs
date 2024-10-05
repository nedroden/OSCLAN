using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Symbols;

public class TypeService
{
    /// <summary>
    /// Checks if the assignment is valid and, if so, whether or not it could lead to possible data loss.
    /// </summary>
    /// <param name="from">The source data.</param>
    /// <param name="to">The destination data (= variable).</param>
    /// <returns></returns>
    public TypeCompatibility VerifyAssignmentCompatibility(Type from, Type to)
    {
        if (from.Name == Mangler.Mangle("string") && (to.Name == Mangler.Mangle("int") || to.Name == Mangler.Mangle("uint")))
        {
            return TypeCompatibility.Illegal;
        }

        if (from.SizeInBytes > to.SizeInBytes)
        {
            return TypeCompatibility.LossOfInformation;
        }

        return TypeCompatibility.Ok;
    }

    /// <summary>
    /// Resolves the type of a given node.
    /// </summary>
    /// <param name="symbolTable">The symbol table to look for type declarations.</param>
    /// <param name="node">The node to resolve the type of.</param>
    /// <returns>The found type.</returns>
    /// <exception cref="CompilerException">Thrown when a node name is empty.</exception>
    public Type GetType(SymbolTable symbolTable, AstNode node)
    {
        var type = new Type(node.RawType?.Name ?? string.Empty)
        {
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

            compositeChildType.IsArray = child.RawType is { Size: > 1 };

            // TODO: check if this works properly. Since it might not.
            compositeChildType.SizeInBytes += compositeChildType.IsArray
                ? child.RawType!.Size * compositeChildType.SizeInBytes
                : compositeChildType.SizeInBytes;

            type.Fields.Add(Mangler.Mangle(name), compositeChildType);
        }

        return type;
    }

    public void IndexType(Type type)
    {
        uint offset = 0;
        type.AddressOffset = 0;

        foreach (var field in type.Fields)
        {
            IndexType(field.Value, offset);
        }
    }

    public uint IndexType(Type type, uint totalOffset)
    {
        type.AddressOffset = totalOffset;

        foreach (var field in type.Fields)
        {
            field.Value.AddressOffset = totalOffset;
            totalOffset += field.Value.SizeInBytes;

            if (field.Value.Fields.Count > 0)
            {
                totalOffset = IndexType(field.Value, totalOffset);
            }
        }

        return totalOffset;
    }
}