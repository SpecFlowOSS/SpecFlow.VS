Feature: Discovery SpecFlow version compatibility

Scenario Outline: Discover bindings from a simple SpecFlow project
	Given there is a simple SpecFlow project for <version>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples: 
	| case      | version |
	| latest    | v2.4.1  |
	| recent    | v2.4.0  |
	| line-v2.3 | v2.3.2  |
	| line-v2.2 | v2.2.1  |
	| line-v2.1 | v2.1.0  |
	| line-v2.0 | v2.0.0  |
	| line-v1.9 | v1.9.0  |
