using System;
using TechTalk.SpecFlow;
namespace DeveroomSample.StepDefinitions
{
    public static class AutomationStub
    {
        public static void DoStep(params object[] stepArgs)
        {
		   Console.WriteLine("executing step...");
        }
	}
}
