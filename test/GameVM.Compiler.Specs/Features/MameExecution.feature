Feature: MAME Execution Validation
  As a developer
  I want to verify that compiled programs execute correctly in MAME
  So that I can ensure behavioral correctness on the target hardware

  Scenario: Basic MAME Execution
    Given the following Pascal program:
      """
      program MAMEValidation;
      var COLUBK: Integer;
      begin
          COLUBK := 10;
      end.
      """
    When I compile the program
    And I run the program in MAME
    Then MAME execution output should contain "--- GAMEVM MAME DUMP ---"
    And MAME execution output should contain "CPU state:"
    And MAME execution output should contain "TIA/RAM Dump:"

  Scenario: Constant Store Lands in RAM
    Given the following Pascal program:
      """
      program ConstantStore;
      var x: integer;
      begin
          x := 5;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to zero-page $80; the constant 5 must be in RAM at the sample frame
    Then MAME execution output should contain "$80: 05"

  Scenario: TIA Register Side Effects
    Given the following Pascal program:
      """
      program TIASideEffects;
      var COLUBK: Integer;
      begin
          COLUBK := 15;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # COLUBK ($09) is write-only, so we check the A register which held the value
    Then MAME execution output should contain "A: 15"
