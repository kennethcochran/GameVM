Feature: Code Optimization
  As a developer
  I want my code to be optimized
  So that the generated ROM is smaller and faster

  Scenario: Dead code elimination
    Given a Pascal program with unreachable code
    When I compile with optimization enabled
    Then the generated binary is smaller
    And no dead code instructions appear in output

  Scenario: Constant folding
    Given a Pascal program with constant expressions
    When I compile with optimization enabled
    Then constant expressions are folded
    And the binary contains the computed values

  Scenario: Common subexpression elimination
    Given a Pascal program with duplicate expressions
    When I compile with aggressive optimization
    Then common subexpressions are eliminated
    And the binary is optimized

