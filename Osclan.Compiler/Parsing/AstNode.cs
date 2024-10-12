using System;
using System.Collections.Generic;
using System.Linq;

namespace Osclan.Compiler.Parsing;

/// <summary>
/// Represents a node in the abstract syntax tree.
/// </summary>
public class AstNode
{
    /// <summary>
    /// The type of the node.
    /// </summary>
    public AstNodeType Type { get; set; }

    /// <summary>
    /// String representation of the node type, for intermediate serialization purposes.
    /// </summary>
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
    public List<Modifier> Modifiers { get; set; } = [];

    /// <summary>
    /// String representations of the modifiers, for intermediate serialization purposes.
    /// </summary>
    public List<string> ModifierStrings =>
        Modifiers.Select(modifier => Enum.GetName(typeof(Modifier), modifier) ?? "Unknown").ToList();

    /// <summary>
    /// Whether or not the node represents a dereferenced pointer.
    /// </summary>
    public bool IsDereferenced { get; set; }

    /// <summary>
    /// For fields, this represents the path to the field, e.g., elements_1::name::first-name for some variable 'list'.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Used by the analyzer to keep track of the symbol table that the variable/type was resolved in. This
    /// can then be used in the code generation process.
    /// </summary>
    public Guid? ResolvedIn { get; set; }

    /// <summary>
    /// The children of the node.
    /// </summary>
    public List<AstNode> Children { get; set; } = [];

    /// <summary>
    /// Meta information, used during semantic analysis to keep track of things like the procedure name.
    /// NOTE: I know this is an ugly solution, but it is a temporary one, until I find a better way to do this.
    /// And if I don't: remember nothing is as permanent as a temporary solution.
    /// </summary>
    public Dictionary<string, string> Meta { get; set; } = [];
}