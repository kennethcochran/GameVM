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
