// RUN: %compile --input %s --output %t
// Regression test for bug #010: Performance regression in compilation
// CHECK: Compilation completes in reasonable time

program Bug010;
var
    i, j, k: Integer;
begin
    for i := 1 to 100 do
    begin
        for j := 1 to 100 do
        begin
            k := i + j;
        end;
    end;
end.

