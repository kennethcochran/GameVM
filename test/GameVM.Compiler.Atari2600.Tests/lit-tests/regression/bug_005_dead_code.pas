// RUN: %compile --input %s --output %t
// Regression test for bug #005: Dead code not eliminated
// CHECK: Dead code removed

program Bug005;
var x: Integer;
begin
    x := 1;
    { Dead code after return would be here }
end.

