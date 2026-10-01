using System;

namespace RemoteFileManagement.Shared
{
    /// <summary>
    /// Encapsulates the outcome of a remote file management operation.
    /// Marked [Serializable] for .NET Remoting serialization.
    /// </summary>
    [Serializable]
    public class FileOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorDetails { get; set; }
        public string AffectedPath { get; set; }

        public FileOperationResult()
        {
        }

        public FileOperationResult(bool success, string message, string errorDetails = null, string affectedPath = null)
        {
            Success = success;
            Message = message;
            ErrorDetails = errorDetails;
            AffectedPath = affectedPath;
        }

        public static FileOperationResult Ok(string message, string affectedPath = null)
        {
            return new FileOperationResult(true, message, null, affectedPath);
        }

        public static FileOperationResult Fail(string message, string errorDetails = null, string affectedPath = null)
        {
            return new FileOperationResult(false, message, errorDetails, affectedPath);
        }

        public override string ToString()
        {
            return Success ? string.Format("SUCCESS: {0}", Message) : string.Format("FAILURE: {0} ({1})", Message, ErrorDetails);
        }
    }
}
