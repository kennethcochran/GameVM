Feature: Unresolved labels fail loudly in code generation
  As a developer
  I want compilation to fail loudly when generated code refers to a label that was never defined
  So that I never receive a ROM that silently jumps to the wrong place

  Scenario: Jump to a label that was never defined fails instead of jumping to the reset address
    Given low-level code that jumps to a label that is never defined
    When I generate the Atari 2600 ROM
    Then ROM generation fails with an unresolved label error

  Scenario: Call to a label that was never defined fails instead of jumping to the reset address
    Given low-level code that calls a label that is never defined
    When I generate the Atari 2600 ROM
    Then ROM generation fails with an unresolved label error

  Scenario: Branch to a label that was never defined fails instead of branching by a garbage offset
    Given low-level code that branches to a label that is never defined
    When I generate the Atari 2600 ROM
    Then ROM generation fails with an unresolved label error

  Scenario: Jump to a label that is defined still generates a ROM
    Given low-level code that jumps to a label that is defined
    When I generate the Atari 2600 ROM
    Then ROM generation succeeds
