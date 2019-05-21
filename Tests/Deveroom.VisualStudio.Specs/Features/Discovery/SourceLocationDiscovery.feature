Feature: Source Location Discovery

Scenario: Discover binding source location
	Given there is a small SpecFlow project
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line

Scenario: Discover binding source location from SpecFlow project with external bindings
	Given there is a small SpecFlow project with external bindings
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with source file containing "ExternalBindings"

Scenario Outline: Discover binding source location from SpecFlow project with async bindings
	Given there is a small SpecFlow project with async bindings
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples: 
	| label | framework     | 
	| V1    | net452        | 
	| V2    | netcoreapp2.1 | 

