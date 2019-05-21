using System;
using Deveroom.VisualStudio.Diagonostics;
using EnvDTE;

namespace Deveroom.VisualStudio.VsEvents
{
    public class DocumentEventsListener : IDisposable
    {
        private readonly IDeveroomLogger _logger;
        private DocumentEvents _documentEvents;

        public event Action<Document> DocumentOpened;

        public DocumentEventsListener(IDeveroomLogger logger, DTE dte)
        {
            _logger = logger;
            _documentEvents = dte.Events.DocumentEvents;
            _documentEvents.DocumentOpened += DocumentEventsOnDocumentOpened;
        }

        private void DocumentEventsOnDocumentOpened(Document document)
        {
            _logger.LogVerbose($"{document.FullName}, {document.Type}");
            DocumentOpened?.Invoke(document);
        }

        public void Dispose()
        {
            if (_documentEvents != null)
            {
                _documentEvents.DocumentOpened -= DocumentEventsOnDocumentOpened;
                _documentEvents = null;
            }
        }
    }
}
