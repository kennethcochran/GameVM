// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 0a 85 09

program HelloWorld;
var
    COLUBK: integer;
begin
    COLUBK := 10;
end.
