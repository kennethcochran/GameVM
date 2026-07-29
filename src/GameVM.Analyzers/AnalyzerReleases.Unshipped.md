### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
GVM001 | Design | Error | Types in configured DOD namespaces must be value types (structs)
GVM002 | Design | Error | Structs in configured DOD namespaces must have explicit StructLayout
GVM003 | Performance | Error | LINQ usage prohibited in configured optimization namespaces
GVM004 | Design | Error | Raw integer used where BlockId expected in CFG APIs
GVM005 | Design | Warning | Switch on InstructionKind not exhaustive
GVM006 | Performance | Error | Virtual/interface dispatch prohibited in configured namespaces