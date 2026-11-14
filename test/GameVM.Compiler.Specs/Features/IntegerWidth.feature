Feature: Integer values wider than 8 bits
  As a programmer
  I want integer values above 255 to work correctly
  So that I can count, add, and store numbers bigger than a single byte

  Scenario: Storing and reading back a value greater than 255
    Given the following Pascal program:
      """
      program Probe;
      var x, y: Integer;
      begin
        x := 300;
        y := x;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "A9 2C 85 80 A9 01 85 81"
    And the output binary should contain the hex sequence "A5 80 85 82 A5 81 85 83"

  Scenario: Adding two values above 255 carries into the high byte
    Given the following Pascal program:
      """
      program Probe;
      var x, y, z: Integer;
      begin
        x := 300;
        y := 500;
        z := x + y;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "A5 80 18 65 82 85 84 A5 81 65 83 85 85"
