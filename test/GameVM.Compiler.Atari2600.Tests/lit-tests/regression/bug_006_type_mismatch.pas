// RUN: %compile --input %s --output %t
// Regression test for bug #006: Type mismatch not detected
// CHECK: Type error detected

program Bug006;
var x: Integer;
begin
    x := 'hello';  { Type mismatch }
end.

