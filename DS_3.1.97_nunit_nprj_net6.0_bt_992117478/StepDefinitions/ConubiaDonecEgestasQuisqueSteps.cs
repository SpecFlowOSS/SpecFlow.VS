using System;
using TechTalk.SpecFlow;
namespace DeveroomSample.StepDefinitions
{
    [Binding]
    public class ConubiaDonecEgestasQuisqueSteps
    {
        [Then(@"lorem dictum (\d+) accumsan")]
        public void ThenAnteCursusJustoId(int p0)
        {
           AutomationStub.DoStep(p0);
        }

        [When(@"nulla nec dui (\d+)")]
        public void WhenSemDonecCommodoEt(int p0)
        {
           AutomationStub.DoStep(p0);
        }

        [Given(@"id nulla (\d+)")]
        public void GivenConubiaDonecDiamEleifend(int p0)
        {
           AutomationStub.DoStep(p0);
        }

    }
}
