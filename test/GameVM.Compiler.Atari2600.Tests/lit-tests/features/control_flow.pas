// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program ControlFlow;
var x: Integer;
begin
    if x > 0 then
        writeln('positive')
    else
        writeln('non-positive');
end.

