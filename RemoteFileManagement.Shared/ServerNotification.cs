using System;

namespace RemoteFileManagement.Shared
{
    /// <summary>
    /// Represents a real-time event notification pushed from the Server to the Client.
    /// Marked [Serializable] for .NET Remoting cross-domain value marshaling.
    /// </summary>
    [Serializable]
    public class ServerNotification
    {
        public long EventId { get; set; }
        public string EventType { get; set; } // "FILE_CREATED", "FILE_CHANGED", "FILE_DELETED", "FILE_RENAMED", "SERVER_MESSAGE"
        public string Message { get; set; }
        public string RelativePath { get; set; }
        public DateTime Timestamp { get; set; }

        public ServerNotification()
        {
            Timestamp = DateTime.Now;
        }

        public ServerNotification(long eventId, string eventType, string message, string relativePath = "")
        {
            EventId = eventId;
            EventType = eventType;
            Message = message;
            RelativePath = relativePath ?? "";
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("[{0:HH:mm:ss}] [{1}] {2}", Timestamp, EventType, Message);
        }
    }
}
