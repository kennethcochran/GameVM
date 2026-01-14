Feature: Compilation Pipeline
  As a developer
  I want to compile Pascal programs to Atari 2600 bytecode
  So that I can create games for the Atari 2600

  Scenario: Compile simple Pascal program
    Given a Pascal program with a single write statement
    When I compile the program
    Then the compilation succeeds
    And the output binary contains valid 6502 instructions

  Scenario: Compile program with variables
    Given a Pascal program with variable declarations
    When I compile the program
    Then the compilation succeeds
    And variables are allocated to memory

  Scenario: Compile program with functions
    Given a Pascal program with function definitions
    When I compile the program
    Then the compilation succeeds
    And function calls are generated correctly

