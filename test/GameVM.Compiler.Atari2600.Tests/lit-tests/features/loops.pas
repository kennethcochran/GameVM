// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 00 85 80

program Loops;
var i: Integer;
begin
    i := 0;
    while i < 10 do
    begin
        writeln(i);
        i := i + 1;
    end;
end.

