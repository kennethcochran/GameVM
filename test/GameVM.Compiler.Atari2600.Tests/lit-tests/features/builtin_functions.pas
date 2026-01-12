// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program BuiltinFunctions;
begin
    write('Hello');
    writeln('World');
end.

