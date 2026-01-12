// RUN: %compile --input %s --output %t
// Regression test for bug #004: Constant folding not applied
// CHECK: Constants folded correctly

program Bug004;
var x: Integer;
begin
    x := 5 + 3;  { Should fold to 8 }
end.

