using System;

namespace Deveroom.VisualStudio
{
    internal static class ExceptionExtensions
    {
        public static string GetFlattenedMessage(this Exception ex)
        {
            var message = ex.Message;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                message += "->" + ex.Message;
            }

            return message;
        }
    }
}
