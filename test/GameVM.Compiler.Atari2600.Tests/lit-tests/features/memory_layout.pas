// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 2a 85 80

program MemoryLayout;
var
    zeroPageVar: Integer;
    COLUBK: Integer;
begin
    zeroPageVar := 42;
    COLUBK := 10;
end.

