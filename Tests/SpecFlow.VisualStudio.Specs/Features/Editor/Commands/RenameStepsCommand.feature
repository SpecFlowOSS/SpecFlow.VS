@focus
Feature: RenameStepsCommand

Rule: Simple parameterless step definition can be renamed

Scenario: A simple step with a sigle usage is renamed from step definition (code side)
	Given there is a SpecFlow project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I press add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the initial binding discovery is performed
	And the "Rename Step" command is being invoked
	When I specify "I choose add" as renamed step
	Then the file "Addition.feature" should be updated to
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I choose add
		"""
	#And the following step definition snippets should be in the step definition class
	#	| type | regex        |
	#	| When | I choose add |
    Then the editor should be updated to
		"""
		using System;
		using TechTalk.SpecFlow;

		namespace MyProject
		{
		[Binding]
		public class CalculatorSteps
		{
			[When("I choose add")]
			public void WhenIPressAdd()
			{ 
			}
		}
		}
		"""

Rule: User should choose the step definition to rename in case there are multiple step definitions on the method


Scenario: Multiple step definitions declared for the method
	Given there is a SpecFlow project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[Given("I press add")]
			[When("I press add")]
			[When("I invoke add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the initial binding discovery is performed
	When I invoke the "Rename Step" command
	Then a jump list "Choose step definition to rename" is opened with the following items
		| step type | step definition |
		| Given     | I press add     |
		| When      | I press add     |
		| When      | I invoke add    |
	And invoking the first item from the jump list renames the "I press add" "Given" step definition

