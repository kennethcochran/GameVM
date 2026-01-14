Feature: Error Handling
  As a developer
  I want clear error messages when compilation fails
  So that I can fix my code quickly

  Scenario: Undefined variable error
    Given a Pascal program with an undefined variable reference
    When I compile the program
    Then the compilation fails
    And the error message contains "undefined variable"

  Scenario: Type mismatch error
    Given a Pascal program with a type mismatch
    When I compile the program
    Then the compilation fails
    And the error message indicates the type mismatch

  Scenario: Syntax error
    Given a Pascal program with a syntax error
    When I compile the program
    Then the compilation fails
    And the error message indicates the syntax error location

  Scenario: Multiple errors reported
    Given a Pascal program with multiple errors
    When I compile the program
    Then the compilation fails
    And all errors are reported

