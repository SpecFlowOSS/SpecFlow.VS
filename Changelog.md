﻿# v$vNext$ - $buildDate$

* Fix: (#40) Typing performance issue
* Fix: Some commands (e.g. comment / uncomment / format table) is executed with delay
* Fix: Steps are reported as undefined for newly opened project and for non-project feature files

# v2021.4.5 - 2021-12-30

* Fix: (#51) (#52) (#54) Fix System.IO.FileNotFoundException: Could not load file or assembly 'System.Threading.Channels, Version=4.0.2.0, Add missing System.Threading.Channels.dll to the package

# v2021.4.1 - 2021-12-28

* Fix: Logging performance improvements

# v2021.3.1 - 2021-12-16

* Fix: Discovery performance improvements

# v2021.2.1 - 2021-12-01

* Fix: (#23) SpecFlow templates come first in the "Add New Item" dialog
* Feature: (#11) Update binding registry after define step

# v2021.1.4 - 2021-11-25

* Fix: (#29) Disable postbuild events from connectors
* Fix: Add missing semicolon to generated CalculatorStepDefinitions.cs template

# v2021.1.2 - 2021-11-24

* Fix: (#20) 'GetDeveroomTagForCaret: Snapshot version mismatch' when tables are auto formatted
* Fix: (#18) InvokeDiscovery: The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled
* Feature: Update SpecFlow Project Templates for .NET 6 language features VS2022
* Fix: Resolve some compiler warnings caused by inconsistent nuget references
* Fix: Warning about non-SpecFlow project is shown after every build

# v2021.0.201 - 2021-11-15

* Feature: (#17) Add item template for Hooks

# v2021.0.199 - 2021-10-29

* Fix: (#14) Remove implicit convetion of DateTime.MinValue (local) to DateTimeOffset (UTC) resulting in ArgumentOutOfRangeException

# v2021.0.198 - 2021-10-15

* Fix: Fix possible deadlock in StubAnalyticsTransmitter
* Fix: (#10) Use loaded content in StepDefinitionUsageFinder if the file is open already
* Feature: (#9) Rebuild stepDefinitions after rename step

# v2021.0.195 - 2021-10-08

* Feature: Support .NET6

# v2021.0.191 - 2021-10-06

* Fix: Support floating versions of SpecFlow package references
* Fix: (#6) "Could not load file or assembly" by adding NugetCacheAssemblyResolver

# v2021.0.187 - 2021-09-24

* Fix: Could not load file or assembly error when opening ASP.NET MVC test projects.

# v2021.0.185 - 2021-09-21

* Initial release based on v1.6.3 of the [Deveroom for SpecFlow](https://github.com/specsolutions/deveroom-visualstudio) Visual Studio extension.
