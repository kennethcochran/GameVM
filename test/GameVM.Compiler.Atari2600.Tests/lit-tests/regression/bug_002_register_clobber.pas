// RUN: %compile --input %s --output %t
// Regression test for bug #002: Register clobbering in nested calls
// CHECK: Registers preserved correctly

program Bug002;
function Helper: Integer;
begin
    Helper := 42;
end;
begin
    writeln(Helper);
end.

