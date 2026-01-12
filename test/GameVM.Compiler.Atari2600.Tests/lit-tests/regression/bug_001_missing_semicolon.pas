// RUN: %compile --input %s --output %t
// Regression test for bug #001: Missing semicolon should be reported
// CHECK: Error or proper handling

program Bug001;
begin
    writeln('test')
    { Missing semicolon }
end.

