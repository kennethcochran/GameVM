Feature: Control flow graph construction
  As a programmer
  I want the compiler to build the control-flow structure of my program as it compiles
  So that the loops and branches I write keep working exactly as written

  # Guard: wiring the control-flow graph into the pipeline must not
  # change what a program does. A loop that computes a product by
  # repeated multiplication must still compile to a working ROM.
  Scenario: A loop that computes a product keeps working
    Given the following Pascal program:
      """
      program Test;
      var x, p, i: Integer;
      begin
        x := 6;
        p := 1;
        i := 0;
        while i < 3 do
        begin
          p := p * x;
          i := i + 1;
        end;
      end.
      """
    When I compile the program
    Then control flow is compiled correctly
    And the output binary should be 4096 bytes long
