# v$vNext$ - $buildDate$

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