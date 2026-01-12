// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 2a 85 80

program TypeConversion;
var x: Real;
begin
    x := 42;  { Integer to Real }
end.

