// RUN: %compile --input %s --output %t
// Regression test for bug #007: Loop optimization not applied
// CHECK: Loop optimized

program Bug007;
var i: Integer;
begin
    for i := 1 to 5 do
    begin
        writeln(i);
    end;
end.

