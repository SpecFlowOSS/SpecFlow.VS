Feature: Discovery SpecFlow version compatibility

Scenario Outline: Discover bindings from a simple SpecFlow project
	Given there is a simple SpecFlow project for <version>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples: 
	| case          | version       |
	| beta          | v3.1.44-beta  |
	| latest        | v3.0.220      |
	| recent        | v3.0.213      |
	| first v3      | v3.0.188      |
	| first v3 beta | v3.0.161-beta |
	| line-v2.4     | v2.4.1        |
	| line-v2.3     | v2.3.2        |
	| line-v2.2     | v2.2.1        |
	| line-v2.1     | v2.1.0        |
	| line-v2.0     | v2.0.0        |
	| line-v1.9     | v1.9.0        |

Scenario Outline: Discover bindings from different SpecFlow v3 versions for .NET Core
	Given there is a simple SpecFlow project for <version>
	And the project format is new
	And the target framework is netcoreapp2.1
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| version       |
	| v3.1.44-beta  |
	| v3.0.161-beta |
	| v3.0.188      |
	| v3.0.213      |
	| v3.0.220      |
