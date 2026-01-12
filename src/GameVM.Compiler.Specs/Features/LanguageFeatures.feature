Feature: Pascal Language Features
  As a developer
  I want to use Pascal language features
  So that I can write expressive programs

  Scenario: Control flow statements
    Given a Pascal program with if/while/for statements
    When I compile the program
    Then the compilation succeeds
    And control flow is compiled correctly

  Scenario: Function calls
    Given a Pascal program with function calls
    When I compile the program
    Then the compilation succeeds
    And function calls are handled correctly

  Scenario: Built-in functions
    Given a Pascal program using write/writeln
    When I compile the program
    Then the compilation succeeds
    And built-in functions are called correctly

  Scenario: Variable scope
    Given a Pascal program with global and local variables
    When I compile the program
    Then the compilation succeeds
    And variable scope is handled correctly

