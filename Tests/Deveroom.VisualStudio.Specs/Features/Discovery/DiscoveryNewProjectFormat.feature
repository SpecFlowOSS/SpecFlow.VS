Feature: Discovery - New project format

Scenario: Discover bindings from a SpecFlow project using the new project format
	Given there is a simple SpecFlow project for the latest version
	And the project uses the new project format
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line