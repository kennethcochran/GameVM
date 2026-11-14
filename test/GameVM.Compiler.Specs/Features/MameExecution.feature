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

  Scenario: Read Variable and Subtract
    Given the following Pascal program:
      """
      program ReadSubtract;
      var x: integer;
      begin
          x := 5;
          x := x - 1;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to zero-page $80; after x := 5; x := x - 1, RAM must hold 4
    Then MAME execution output should contain "$80: 04"

  Scenario: Branch on Non-Zero (<> arm executes)
    Given the following Pascal program:
      """
      program BranchOnNonZero;
      var x: integer; y: integer;
      begin
          x := 5;
          if x <> 0 then y := 1;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to $80, y to $81; after x := 5; x <> 0 is true → y := 1
    Then MAME execution output should contain "$80: 05"
    And MAME execution output should contain "$81: 01"

  Scenario: Branch on Zero (= arm executes)
    Given the following Pascal program:
      """
      program BranchOnZero;
      var x: integer; y: integer;
      begin
          x := 0;
          if x = 0 then y := 1;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to $80, y to $81; after x := 0; x = 0 is true → y := 1
    Then MAME execution output should contain "$80: 00"
    And MAME execution output should contain "$81: 01"


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

  Scenario: Wedge Loop Terminates
    Given the following Pascal program:
      """
      program WedgeLoop;
      var x: integer;
      begin
          x := 5;
          while x <> 0 do
            begin
              x := x - 1;
            end;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to $80; after the loop x should be 0. The "GAMEVM PROGRAM
    # HALTED" marker proves the program reached its JMP * halt loop
    # (termination); no hardcoded PC address needed.
    Then MAME execution output should contain "GAMEVM PROGRAM HALTED"
    And MAME execution output should contain "$80: 00"

  Scenario: Case Of Dispatches To The Matching Branch
    Given the following Pascal program:
      """
      program CaseOf;
      var x: integer;
      begin
          x := 2;
          case x of
              1: x := 10;
              2: x := 20;
              3: x := 30;
          else
              x := 40
          end;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # x allocates to $80; the selector is 2, so the second arm runs and x
    # becomes 20 ($14) — the other arms and the else must not run. The
    # "GAMEVM PROGRAM HALTED" marker proves the program reached its JMP *
    # halt loop (termination); no hardcoded PC address needed.
    Then MAME execution output should contain "GAMEVM PROGRAM HALTED"
    And MAME execution output should contain "$80: 14"

  Scenario: Case Of Falls Through To Else For An Unmatched Selector
    Given the following Pascal program:
      """
      program CaseElse;
      var x: integer;
      begin
          x := 9;
          case x of
              1: x := 10;
              2: x := 20;
              3: x := 30;
          else
              x := 40
          end;
      end.
      """
    When I compile the program
    And I run the program in MAME
    # No label matches 9, so the else arm runs and x becomes 40 ($28).
    Then MAME execution output should contain "GAMEVM PROGRAM HALTED"
    And MAME execution output should contain "$80: 28"
