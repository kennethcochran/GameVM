using System.Reflection;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

/// <summary>
/// Runtime enforcement of the strict value-type isolation requirement (task 1.10):
/// core DOD IR data structures must not store managed object references, class instances,
/// or strings — only structs, primitive types, and primitive arrays.
/// </summary>
public class ValueTypeIsolationTests
{
    // IR data structures governed by the isolation rule: the fixed-layout slab value types
    // (GameVM.Compiler.Core.IR.Slab) and the growable parallel-array buffers
    // (GameVM.Compiler.Core.IR.Buffers). Slab-processing managers/iterators
    // (GameVM.Compiler.Core.IR.SlabProcessing) are intentionally excluded: they are
    // controllers over arrays, not the IR payload the rule targets.
    private static readonly Type[] IrDataTypes =
    {
        typeof(CfgTable),
        typeof(SlabHeader),
        typeof(LocalSlotIndex),
        typeof(TlvEntry),
        typeof(InstructionMetadataFlags),
        typeof(DiagnosticJournal),
        typeof(DiagnosticEntry),
        typeof(HashedSymbolTable),
        typeof(SlabRelocator),
    };

    [Test]
    public void IrDataTypes_OnlyContainValueTypesOrPrimitiveArrays()
    {
        var violations = new System.Collections.Generic.List<string>();

        foreach (var type in IrDataTypes)
        {
            foreach (var field in GetInstanceFields(type))
            {
                if (IsAllowedFieldType(field.FieldType))
                    continue;

                violations.Add(
                    $"{type.Name}.{field.Name} : {field.FieldType.FullName} " +
                    $"({(field.FieldType.IsValueType ? "value" : "reference")} type not permitted)");
            }
        }

        Assert.That(violations, Is.Empty,
            "Value-type isolation violated:\n" + string.Join("\n", violations));
    }

    /// <summary>
    /// A field type is permitted iff it is a value type (struct/primitive) or a
    /// single-dimensional primitive array (uint[], int[], etc.). Strings, classes,
    /// generics (List/Dictionary), and arrays of references are rejected.
    /// </summary>
    private static bool IsAllowedFieldType(Type fieldType)
    {
        if (fieldType.IsValueType)
            return true;

        if (fieldType.IsArray)
        {
            var elem = fieldType.GetElementType()!;
            // Only primitive element arrays are allowed (uint[], int[], ...).
            return elem.IsPrimitive;
        }

        return false;
    }

    private static FieldInfo[] GetInstanceFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
