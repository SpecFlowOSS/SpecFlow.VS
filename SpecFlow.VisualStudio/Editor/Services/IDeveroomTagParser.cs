namespace SpecFlow.VisualStudio.Editor.Services;

public interface IDeveroomTagParser
{
    ICollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry,
        DeveroomConfiguration configuration);
}
