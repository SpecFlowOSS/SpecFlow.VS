Feature: Discovery - SpecFlow v3 Support

Scenario Outline: Discover bindings from a SpecFlow v3 project
	Given there is a simple SpecFlow project for v3.0.169-beta
	And the project format is <project format>
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| label             | framework     | project format |
	| .NET classic      | net452        | classic        |
	| .NET new prj      | net452        | new            |
	| .NET Core         | netcoreapp2.1 | new            |
	| .NET Core (older) | netcoreapp2.0 | new            |
	| .NET Core (newer) | netcoreapp2.2 | new            |

Scenario Outline: Discover bindings from projects using an older SpecFlow v3
	Given there is a simple SpecFlow project for v3.0.161-beta
	And the project format is <project format>
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| label        | framework     | project format |
	| .NET classic | net452        | classic        |
	| .NET new prj | net452        | new            |
	| .NET Core    | netcoreapp2.1 | new            |

