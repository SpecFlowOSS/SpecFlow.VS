Feature: Auto format document command	

Rules:

* Feature file can be formatted with useful defaults
* Auto format should not add or remove empty lines
	* Should not change descriptions, comments
* Selected text of a feature file can be formatted
* Formatting rules can be configured with editorconfig
* Caret should stay in the same line where it was

Rule: Feature file can be formatted with useful defaults

Scenario: Misformatted feature file is cleaned up

	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
					@featureTag    @US1
		 Feature: Addition
		     Rule: Calculator functions

		  Background:
		Given I have entered 50 into the calculator

		  @focus
		  @WIP              @US1.1
		  Scenario: Add two numbers
		Given the following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And foo
			When   bar
		
			Scenario Outline: Add multiple
				 Given I have entered <number1> to the calculator
				    And I have entered <number2> to the calculator
					When I check the output
					```
					1+2
					  3

 close
					```

			  @optimal
			Examples:
				| number1 | number2 |
				| 1 | 2|
			Examples: negative numbers
				| number1 | number2 |
				| -101 | -59              |
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		@featureTag @US1
		Feature: Addition
		Rule: Calculator functions

		Background:
		    Given I have entered 50 into the calculator

		@focus
		@WIP @US1.1
		Scenario: Add two numbers
		    Given the following numbers added
		        | number | reason        |
		        | 1      | first number  |
		        | 2      | second number |
		    And foo
		    When bar

		Scenario Outline: Add multiple
		    Given I have entered <number1> to the calculator
		    And I have entered <number2> to the calculator
		    When I check the output
		        ```
		        1+2
		          3
		        
		        close
		        ```

		@optimal
		Examples:
		    | number1 | number2 |
		    | 1       | 2       |
		Examples: negative numbers
		    | number1 | number2 |
		    | -101    | -59     |
		"""

Rule: Caret should stay in the same line where it was

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

Rule: Selected text of a feature file can be formatted

Scenario: Selected part of feature file is formatted
	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		Given t{sel}he following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And {/sel}foo
		   When   bar
		"""
	When I invoke the "Auto Format Selection" command
	Then the editor should be updated to
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		    Given the following numbers added
		        | number | reason        |
		        | 1      | first number  |
		        | 2      | second number |
		    And foo
		   When   bar
		"""

Scenario: Caret line of feature file is formatted
	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		Given t{caret}he following numbers added
		   When   bar
		"""
	When I invoke the "Auto Format Selection" command
	Then the editor should be updated to
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		    Given the following numbers added
		   When   bar
		"""

Rule: Auto format should not add or remove empty lines

Scenario: Formatting of Descriptions and Comments are not changed

	Given there is a SpecFlow project scope
	And the following feature file in the editor
		"""
		Feature: Addition
			this is an indented description

		#not working since 2000
		Scenario: Add two numbers
		  Todo: handle negative numbers

		    Given I have entered 50 into the calculator
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		Feature: Addition
			this is an indented description

		#not working since 2000
		Scenario: Add two numbers
		  Todo: handle negative numbers

		    Given I have entered 50 into the calculator
		"""