using System;
using System.Collections.ObjectModel;
using System.Linq;
using SpecFlow.VisualStudio.UI.ViewModels.WizardDialogs;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class WelcomeDialogViewModel : WizardViewModel
    {
        public const string WELCOME_TEXT = @"
# Welcome to Deveroom

Deveroom is a feature file editing tool in Visual Studio with special support for SpecFlow projects. 
Our goal is to support the SpecFlow and BDD community with a good and sustainable Visual Studio integration. 
Currently Deveroom supports the **core editing and navigation features** and these will always be *free*. 
For more information please check [our website](https://www.specsolutions.eu/services/deveroom/).

Here are the most important features supported currently. 

* **Support for all SpecFlow versions and all project types**, including SpecFlow v3 on .NET Core and SDK-style projects
* **Gherkin syntax coloring and keyword completion** with new colors, context-sensitive keyword completion and highlighting syntax errors
* **Step definition matching** with *Navigate to step definition*, *Find step definition usages* and *Define Step wizard* (you need to build the project)
* **Hassle-free 'Add new feature file'** that skips design-time code generation setting when unnecessary and works with .NET Core projects as well
";

        public const string HOWTOUSE_TEXT = @"
# How to Use Deveroom

Using Deveroom is easy. Just open your feature files and start editing. There are a few things 
to note especially if you used the *SpecFlow for Visual Studio* extension before.

* **Build your project** to get up-to-date step definition matching, *Define Step wizard* and navigation.
* **Use 'Define Steps...'** from the context menu of the feature file to get started with automating undefined 
steps (colored as coral). You can use the *Ctrl+B,D* keyboard shortcut as well.
* **Use Ctrl+Space to trigger step completion** in the feature file after a step keyword.
";

        public const string TROUBLESHOOTING_TEXT = @"
# Troubleshooting Tips

Deveroom is still new, so there might be some glitches, but **we are eager to hear about your feedback**.

* If you are in trouble, you should first check the **Deveroom pane of the Output Window**. 
You can open it by choosing *View / Output* from the Visual Studio menu and switch 
the *Show output* from dropdown to *Deveroom*.

* You can find even more trace information in the **log file** in the [%LOCALAPPDATA%\Deveroom](file://%LOCALAPPDATA%\Deveroom) folder.

* Please **register your suggestions and issues** in our [issue tracker on GitHub](https://github.com/specsolutions/deveroom-visualstudio/issues).
";

        public WelcomeDialogViewModel() : base("Close", "Welcome to Deveroom",
            new MarkDownWizardPageViewModel("Welcome") { Text = WELCOME_TEXT },
            new MarkDownWizardPageViewModel("How to use") { Text = HOWTOUSE_TEXT },
            new MarkDownWizardPageViewModel("Troubleshooting") { Text = TROUBLESHOOTING_TEXT })
        {
        }

#if DEBUG
        public static WelcomeDialogViewModel DesignData = new WelcomeDialogViewModel();
#endif
    }
}
