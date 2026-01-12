// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program ROMGeneration;
begin
    // Simple program to test ROM generation
    writeln('Hello');
end.

