using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Classification;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class StubEditorFormatMap : IEditorFormatMap
    {
        public ResourceDictionary GetProperties(string key)
        {
            return new ResourceDictionary();
        }

        public void AddProperties(string key, ResourceDictionary properties)
        {
            throw new NotImplementedException();
        }

        public void SetProperties(string key, ResourceDictionary properties)
        {
            throw new NotImplementedException();
        }

        public void BeginBatchUpdate()
        {
            throw new NotImplementedException();
        }

        public void EndBatchUpdate()
        {
            throw new NotImplementedException();
        }

        public bool IsInBatchUpdate
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<FormatItemsEventArgs> FormatMappingChanged;
    }
}
