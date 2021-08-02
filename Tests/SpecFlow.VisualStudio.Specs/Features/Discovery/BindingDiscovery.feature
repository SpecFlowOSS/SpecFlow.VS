Feature: Binding Discovery

Scenario: Discover bindings from a simple latest SpecFlow project
	Given there is a simple SpecFlow project for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions

Scenario: Discover bindings from SpecFlow project with plugin
	Given there is a simple SpecFlow project with plugin for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with regex "there should be a step from a plugin"

Scenario: Discover bindings from SpecFlow project with external bindings
	Given there is a simple SpecFlow project with external bindings for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with regex "there should be a step from an external assembly"
