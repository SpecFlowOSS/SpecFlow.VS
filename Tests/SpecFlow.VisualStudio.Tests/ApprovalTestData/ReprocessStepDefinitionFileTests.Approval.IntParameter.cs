using System;
using TechTalk.SpecFlow;

namespace TestProject
{
    [Binding]
    public class Feature1StepDefinitions
    {
        [When(@"I press (.*)")]
        public void WhenIPress(int intParameter)
        {
            throw new PendingStepException();
        }
    }
}
