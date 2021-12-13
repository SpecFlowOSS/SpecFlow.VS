﻿namespace SpecFlow.VisualStudio.ProjectSystem;

public class NullDeveroomErrorListServices : IDeveroomErrorListServices
{
    public void ClearErrors(DeveroomUserErrorCategory category)
    {
    }

    public void AddErrors(IEnumerable<DeveroomUserError> errors)
    {
    }
}
