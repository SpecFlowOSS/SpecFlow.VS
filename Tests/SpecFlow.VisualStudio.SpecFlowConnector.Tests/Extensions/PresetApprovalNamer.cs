// ReSharper disable once CheckNamespace
namespace ApprovalTests.Namers;

class PresetApprovalNamer : UnitTestFrameworkNamer
{
    public PresetApprovalNamer(string name)
    {
        Name = name;
    }

    public override string Name { get; }
}
