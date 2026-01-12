// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program NestedBlocks;
var x: Integer;
begin
    if x > 0 then
    begin
        if x < 10 then
        begin
            writeln('between 0 and 10');
        end;
    end;
end.

