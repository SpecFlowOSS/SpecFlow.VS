# Contributing to Deveroom

## Building the project

### Prerequisites

* Visual Studio 2017 or 2019 with workloads
  * .NET Desktop Deverlopment
  * Visual Studio extension development
  * .NET Core cross-platform development

* To be able to run all tests, you also need to
  * Run the tests in 64-bit mode (one test needs this)
    * For the VS test explorer this can be set from "Test / Test Settings / Processor Architecture for AnyCPU projects" in VS
  * Install .NET Core 2.2 (one test needs this)

## Build the project

* Before building the project you need to run the `Connectors\build.ps1` script that builds the SpecFlow connectors.

## Run Tests

* The tests need to run in x64. This is configured in the run settings file `.runsettings` in the sloution folder. To be able to pick up this settings file, you have to enable ["Autodetect the run settings file"](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2019#autodetect-the-run-settings-file). 

* The Specs project generates sample projects and caches them to speed up the test execution. The cache is by default in the system TEMP folder that is regularly cleaned up by Windows. If you are a regular contributor it is recommended to setup the cache in a folder that is not managed by Windows. You can do this by settign the `DEVEROOM_TEST_TEMP` environment variable. E.g. SET DEVEROOM_TEST_TEMP=C:\Temp
