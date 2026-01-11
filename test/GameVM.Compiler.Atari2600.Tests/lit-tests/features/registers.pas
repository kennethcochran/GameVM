// RUN: %compile --input %s --output %t
// RUN: %mame-run %t | %FileCheck %s

// Verify RAM address $80 is set to 42 ($2A)
// CHECK: 0080: 2A

program RAMTest;
var
    MyVar: integer;
begin
    // For this test, we assume the compiler maps MyVar to $80
    // (We will verify this by manually looking at the dump if needed, 
    // but the emitter currently uses the address provided in assembly)
    MyVar := 42;
end.
