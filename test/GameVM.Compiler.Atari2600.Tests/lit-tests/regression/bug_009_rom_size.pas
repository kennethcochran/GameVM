// RUN: %compile --input %s --output %t
// Regression test for bug #009: ROM size exceeded without warning
// CHECK: ROM size validated

program Bug009;
var
    i: Integer;
begin
    for i := 1 to 1000 do
    begin
        writeln(i);
    end;
end.

