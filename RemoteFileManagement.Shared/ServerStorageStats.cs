using System;

namespace RemoteFileManagement.Shared
{
    /// <summary>
    /// Encapsulates storage capacity and file statistics from the server.
    /// Marked [Serializable] for .NET Remoting cross-domain value marshaling.
    /// </summary>
    [Serializable]
    public class ServerStorageStats
    {
        public int TotalFiles { get; set; }
        public int TotalDirectories { get; set; }
        public long TotalSizeBytes { get; set; }
        public string ServerMachineName { get; set; }
        public DateTime ServerTime { get; set; }
        public string StorageRootPath { get; set; }

        public string FormattedTotalSize
        {
            get
            {
                if (TotalSizeBytes < 1024) return string.Format("{0} B", TotalSizeBytes);
                if (TotalSizeBytes < 1024 * 1024) return string.Format("{0:0.0} KB", TotalSizeBytes / 1024.0);
                if (TotalSizeBytes < 1024 * 1024 * 1024) return string.Format("{0:0.0} MB", TotalSizeBytes / (1024.0 * 1024.0));
                return string.Format("{0:0.00} GB", TotalSizeBytes / (1024.0 * 1024.0 * 1024.0));
            }
        }
    }
}
