// RUN: %compile --input %s --output %t
// Regression test for bug #008: Stack overflow in deep recursion
// CHECK: Stack handled correctly

program Bug008;
function Recursive(n: Integer): Integer;
begin
    if n <= 0 then
        Recursive := 0
    else
        Recursive := Recursive(n - 1) + 1;
end;
begin
    writeln(Recursive(10));
end.

