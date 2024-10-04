using System;
using System.Collections.Generic;
using System.Linq;

namespace Osclan.Compiler.Parsing;

public class AstNode
{
    public AstNodeType Type { get; set; }

    public string TypeString => Enum.GetName(typeof(AstNodeType), Type) ?? "Unknown";

    /// <summary>
    /// The type of the value, if a variable. Determined during the analysis process.
    /// </summary>
    public Type? TypeInformation { get; set; }

    /// <summary>
    /// Preliminary information about the type, such as the name and offset.
    /// </summary>
    public RawType? RawType { get; set; }

    /// <summary>
    /// The value of a node, e.g., 'module' for a directive. In case of variables, this is the name of the variable itself,
    /// excluding any fields (meaning this equals 'list' if we have some field 'list::elements::whatever').
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Modifiers for the node, e.g., 'public' or 'private'.
    /// </summary>
    public List<Modifier> Modifiers { get; set; } = new();

    public List<string> ModifierStrings =>
        Modifiers.Select(modifier => Enum.GetName(typeof(Modifier), modifier) ?? "Unknown").ToList();

    public bool IsDereferenced { get; set; }

    /// <summary>
    /// For fields, this represents the path to the field, e.g., elements_1::name::first-name for some variable 'list'.
    /// </summary>
    public string? Path { get; set; }

    public List<PartialPath> PartialPaths =>
        Path?.Split("::").Select(partialPath =>
        {
            var parts = partialPath.Split('_');

            // Dynamic offsets (e.g., $1), will not serialize correctly. Hence, just ignore then for now.
            if (parts.Length > 1 && !uint.TryParse(parts[1], out _))
            {
                parts[1] = uint.MaxValue.ToString();
            }

            return new PartialPath(parts[0], parts.Length > 1 ? uint.Parse(parts[1]) : null);
        }).ToList() ?? [];

    /// <summary>
    /// Used by the analyzer to keep track of the symbol table that the variable/type was resolved in. This
    /// can then be used in the code generation process.
    /// </summary>
    public Guid? ResolvedIn { get; set; }

    public List<AstNode> Children { get; set; } = [];
}

public record PartialPath(string Name, uint? Offset);