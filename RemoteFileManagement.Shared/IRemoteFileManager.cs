using System;
using System.Collections.Generic;

namespace RemoteFileManagement.Shared
{
    /// <summary>
    /// Remote interface defining all file and directory management operations.
    /// The client communicates with the server via this interface using .NET Remoting.
    /// </summary>
    public interface IRemoteFileManager
    {
        /// <summary>
        /// Tests connectivity and responds with true if the server is active.
        /// </summary>
        bool Ping();

        /// <summary>
        /// Gets storage statistics from the server.
        /// </summary>
        ServerStorageStats GetStorageStats();

        /// <summary>
        /// Retrieves the list of files in a specific relative directory on the server.
        /// </summary>
        List<FileItem> GetFiles(string relativeDirectoryPath);

        /// <summary>
        /// Retrieves the list of subdirectories in a specific relative directory on the server.
        /// </summary>
        List<FileItem> GetDirectories(string relativeDirectoryPath);

        /// <summary>
        /// Retrieves both directories and files in a specific relative directory.
        /// </summary>
        List<FileItem> GetAllItems(string relativeDirectoryPath);

        /// <summary>
        /// Reads the text content of a file located on the server.
        /// </summary>
        string ReadFile(string relativeFilePath);

        /// <summary>
        /// Creates a new text file on the server with the specified content.
        /// </summary>
        FileOperationResult CreateFile(string relativeFilePath, string content);

        /// <summary>
        /// Writes/overwrites text content to an existing file on the server.
        /// </summary>
        FileOperationResult WriteFile(string relativeFilePath, string content);

        /// <summary>
        /// Appends text content to an existing file on the server.
        /// </summary>
        FileOperationResult AppendToFile(string relativeFilePath, string content);

        /// <summary>
        /// Updates the content of an existing text file on the server.
        /// </summary>
        FileOperationResult UpdateFile(string relativeFilePath, string content);

        /// <summary>
        /// Deletes a file from the server.
        /// </summary>
        FileOperationResult DeleteFile(string relativeFilePath);

        /// <summary>
        /// Renames a file on the server.
        /// </summary>
        FileOperationResult RenameFile(string relativeFilePath, string newFileName);

        /// <summary>
        /// Copies a file from one server location to another.
        /// </summary>
        FileOperationResult CopyFile(string sourceRelativePath, string destRelativePath);

        /// <summary>
        /// Moves a file from one server location to another.
        /// </summary>
        FileOperationResult MoveFile(string sourceRelativePath, string destRelativePath);

        /// <summary>
        /// Creates a new directory on the server.
        /// </summary>
        FileOperationResult CreateDirectory(string relativeDirectoryPath);

        /// <summary>
        /// Deletes a directory and its contents from the server.
        /// </summary>
        FileOperationResult DeleteDirectory(string relativeDirectoryPath);

        /// <summary>
        /// Renames a directory on the server.
        /// </summary>
        FileOperationResult RenameDirectory(string relativeDirectoryPath, string newDirectoryName);

        /// <summary>
        /// Uploads binary data from the client and saves it on the server.
        /// </summary>
        FileOperationResult UploadFile(string relativeFilePath, byte[] data);

        /// <summary>
        /// Downloads a file from the server as raw binary bytes.
        /// </summary>
        byte[] DownloadFile(string relativeFilePath);

        /// <summary>
        /// Searches for files matching a pattern within a directory (recursive).
        /// </summary>
        List<FileItem> SearchFiles(string searchPattern, string relativeDirectoryPath);

        /// <summary>
        /// Retrieves detailed metadata for a file or directory.
        /// </summary>
        FileItem GetFileInfo(string relativeFilePath);

        /// <summary>
        /// Retrieves pending real-time notifications from the server since the specified event ID.
        /// Enables server-to-client event pushing (file changes, admin broadcasts).
        /// </summary>
        List<ServerNotification> GetPendingNotifications(long lastEventId);

        /// <summary>
        /// Broadcasts an administrative message from the server to all connected clients.
        /// </summary>
        FileOperationResult BroadcastMessage(string message);
    }
}
