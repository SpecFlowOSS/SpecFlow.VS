Feature: Step Completion

Scenario: Offers step definitions of the scenario block at the caret
	Given there is a SpecFlow project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  When {caret}
		"""
	And the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item        |
		| I press add |

Scenario: Completes step at the caret position
	Given there is a SpecFlow project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I {caret}
		"""
	And the initial binding discovery is performed
	When I invoke the "Complete" command
	And commit the "I press add" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I press add
		"""

Scenario: Replaces step at the caret position
	Given there is a SpecFlow project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I {caret} press mulitply
		"""
	And the initial binding discovery is performed
	When I invoke the "Complete" command
	And commit the "I press add" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I press add
		"""

Scenario: Offers simple step definitions with parameter placeholders
	Given there is a SpecFlow project scope
	And the following step definitions in the project:
		| type  | regex                                               | param types |
		| Given | ^I have entered (.*) into the "([^"]*)" calculator$ | i\|s        |
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given {caret}
		"""
	And the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item                                                |
		| I have entered [int] into the "[string]" calculator |

Scenario: Offers complex step definitions as regex
	Given there is a SpecFlow project scope
	And the following step definitions in the project:
		| type  | regex                                       | param types |
		| Given | ^I use a (?:[step]+) with (\\d+) parameter$ | i           |
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given {caret}
		"""
	And the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item                                      |
		| I use a (?:[step]+) with (\\d+) parameter |
