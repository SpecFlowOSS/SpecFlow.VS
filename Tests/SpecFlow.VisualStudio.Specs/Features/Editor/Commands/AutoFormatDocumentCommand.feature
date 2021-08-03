@focus
Feature: Auto format document command	

Rules:

* Feature file can be formatted with useful defaults
* Auto format should not add or remove empty lines
* Selected text of a feature file can be formatted
* Formatting rules can be configured with editorconfig
* Caret should stay in the same line where it was

#Rule: Feature file can be formatted with useful defaults

Scenario: Misformatted feature file is cleaned up
	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		Given the following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And foo
			When   bar
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		    Given the following numbers added
		        | number | reason        |
		        | 1      | first number  |
		        | 2      | second number |
		    And foo
		    When bar
		"""

#Rule: Caret should stay in the same line where it was

Scenario: Caret is moved to the end of the line
	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
			Feature: {caret}Addition
		Scenario: Add two numbers
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		Feature: Addition{caret}
		Scenario: Add two numbers
		"""