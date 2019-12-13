Feature: Discovery - Platform Compatibility

Scenario Outline: Discover bindings from a SpecFlow v3 project in different .NET Core platforms
	Given there is a simple SpecFlow project for v3.*
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| framework     |
	| netcoreapp2.0 |
	| netcoreapp2.1 |
	| netcoreapp2.2 |
	| netcoreapp3.0 |
@quarantine
Examples: .NET Core 3.1
	This configuration is in quarantine until we figure out how 
	the test project can be built in .NET Core 3.1. 
	See related build pipleline: https://dev.azure.com/specsolutions/deveroom-visualstudio/_build?definitionId=7
	| framework     |
	| netcoreapp3.1 |
