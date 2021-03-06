Feature: Discovery - Handling special projects

Scenario: Discover Unicode bindings from SpecFlow project
	Given there is a simple SpecFlow project with unicode bindings for the latest version
	And the project is built
	When the binding discovery performed
	Then there is a step definition with Unicode regex
	And the step definitions contain source file and line

Scenario Outline: Discover bindings from SpecFlow project with platform target
	Given there is a simple SpecFlow project with platform target "<target>" for the latest version
	And the project is built
	And the project is configured to use "<target>" connector
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples: 
	| target |
	| x86    |
	| x64    |
