using System;
using System.Linq;
using System.Reflection;
using GameVM.Compiler.Core.DOD;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests.DOD;

/// <summary>
/// Runtime enforcement of the strict value-type isolation requirement (task 1.10):
/// core DOD IR data structures must not store managed object references, class instances,
/// or strings — only structs, primitive types, and primitive arrays.
/// </summary>
public class ValueTypeIsolationTests
{
    // IR data structures governed by the isolation rule. Managers/utilities that own
    // arena/chunk arrays (ArenaAllocator, SlabIterator, CfgConstructionPass,
    // SlabCompactionUtility, SlabRelocator, HashedSymbolTable) are intentionally excluded:
    // they are controllers over arrays, not the IR payload the rule targets.
    private static readonly Type[] IrDataTypes =
    {
        typeof(CfgTable),
        typeof(SlabHeader),
        typeof(LocalSlotIndex),
        typeof(DiagnosticJournal),
        typeof(DiagnosticEntry),
        typeof(TlvEntry),
        typeof(Instruction),
        typeof(InstructionFlags),
        typeof(InstructionMetadataFlags),
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
