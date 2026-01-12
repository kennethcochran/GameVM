// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 00 85 80

program EdgeCases;
var x: Integer;
begin
    x := 0;
    if x = 0 then
        writeln('zero');
end.

