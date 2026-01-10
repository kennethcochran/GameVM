// RUN: %compile %s -o %t
// RUN: %t | FileCheck %s
// CHECK: Hello, Atari 2600!

program HelloWorld;
begin
    WriteLn('Hello, Atari 2600!');
end.
