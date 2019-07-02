# vNext

New features:

* Provide documentation in autocompletion of keywords (Issue #3)

Improvements:

* Step completion should use word containment for filtering the list (Issue #28)
* Display warning on config load error
* Improved error handling for code-behind generation

Bug fixes:

* Editor: No syntax coloring when the file ends with doc-string open (Issue #6)
* Default language is not detected for SpecFlow v3 from specflow.json config file

# v1.3.0 - 2019-06-20

This is the first open-source release from GitHub: https://github.com/specsolutions/deveroom-visualstudio

New features:

* Higlight DataTable headers (Issue #31)

Bug fixes:

* Unable to discover step definitions for SpecFlow v3.0.220 (Issue #38)
* Table autoformat removes unfinished cells at the end (Issue #40)
* Editor should tolerate backslash at the end of the line in a DataTable (Issue #39)
* Regenerate Feature.cs file comes back as error if there is a space in the project path (Issue #34)
* Deveroom might call BeforeTestRun event during binding discovery with SpecFlow v3 on .NET Framework (Issue #27)

# v1.2.1 - 2019-04-24

## Deveroom is going to be open-sourced on GitHub!

See more information at: http://gasparnagy.com/2019/04/deveroom-goes-open-source/

There are a few preparation steps ahead but they will not take more than a few weeks. Stay tuned on our [GitHub project](https://github.com/specsolutions/deveroom-visualstudio) and our twitter channel ([@SpecSol_BDD](https://twitter.com/SpecSol_BDD)) to get the latest updates.

Important changes: 

* The "deveroom-feedback" project, where we have collected the Deveroom issues has been renamed to https://github.com/specsolutions/deveroom-visualstudio, with all the existing issues. Please use the new link (https://github.com/specsolutions/deveroom-visualstudio/issues) for providing feedback from now on!

Bug fixes:

* Deveroom might call BeforeTestRun event during binding discovery (Issue #27)

# v1.2.0 - 2019-04-12

New features:

* Find Step Definition Usages (Ctrl B,F) - Can be invoked from the context menu of a binding class. Supports finding usages of 
  a step definition of a shared step definition project; allows showing the results in the output window as well.
* Collapsable outlining for feature files
* Regenerate All Feature File Code-behind command (on the context menu of the project)
* Filter auto-completion list according to entered phrase (Issue #16)

Bug fixes:

* Deveroom fails to discover step definitions when App.config does not contain a <specFlow> node (Issue #24)
* Error while building the project - Unable to copy file "obj\Debug\..." to "bin\Debug\..." (Issue #26)
* Dialogs are not in the center of the screen (Issue #17)
* No info links provided in the VS extension details (Issue #18)
* Improve error diagnostics

# v1.1.2 - 2019-04-05

Bug fixes:

* Unable to jump to async step definitions (Issue #23)

# v1.1.1 - 2019-03-22

Bug fixes:

* "Add new feature file" causes assembly load error (Issue #20, reminder for myself: never do last minute changes after regression testing)

# v1.1.0 - 2019-03-21

New features:

* Visual Studio 2019 support (tested with VS2019 RC3)

Bug fixes:

* Improve error diagnostics

# v1.0.4 - 2019-03-12

New features:

* Welcome and What's new screen

# v1.0.3 - 2019-03-11

Bug fixes:

* Improve error diagnostics
* Improve .NET Core binding discovery
* .NET Core Bindings: Bindings not discovered on slow machines (Issue #11)
* .NET Core Bindings: Project dependency load issue (Issue #5)

# v1.0.2 - 2019-03-07

Bug fixes:

* Ask user to upgrade Visual Studio or report error on initialization errors (Issue #9)
* Deveroom init: FileNotFoundException: Could not load file or assembly System.Net.Http (Issue #8)
* Improve error diagnostics
* Escaping regex character \ is displayed when using auto completion in a .feature file. (Issue #2)
* Binding: Error: CreatePersistentTrackingPosition: Exception: System.ArgumentException: Must be non-negative. (Issue #10)

# v1.0.1 - 2019-02-27

Bug fixes:

* CreatePersistentTrackingPosition Exception / Step navigation error
* .NET Core Bindings: Unable to load BoDi.dll (temporary fix)

# v1.0.0 - 2019-02-20

* Initial release