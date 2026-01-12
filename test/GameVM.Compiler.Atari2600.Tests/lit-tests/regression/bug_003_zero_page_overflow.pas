// RUN: %compile --input %s --output %t
// Regression test for bug #003: Zero-page variable overflow
// CHECK: Proper memory allocation

program Bug003;
var
    var1, var2, var3, var4, var5: Integer;
begin
    var1 := 1;
    var2 := 2;
    var3 := 3;
    var4 := 4;
    var5 := 5;
end.

