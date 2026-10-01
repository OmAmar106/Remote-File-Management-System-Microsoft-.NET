using System;
using System.Collections.Generic;
using System.IO;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Server
{
    /// <summary>
    /// Remote object implementation of IRemoteFileManager.
    /// Inherits from MarshalByRefObject so that calls from the client across the .NET Remoting channel
    /// are dispatched to this server-side object instance.
    /// </summary>
    public class RemoteFileManager : MarshalByRefObject, IRemoteFileManager
    {
        private readonly string _storageRoot;
        private readonly string _storageRootWithSeparator;
        private FileSystemWatcher _watcher;

        private static readonly object _notificationLock = new object();
        private static readonly List<ServerNotification> _notifications = new List<ServerNotification>();
        private static long _nextEventId = 1;

        /// <summary>
        /// Initializes the RemoteFileManager with the designated ServerStorage directory.
        /// </summary>
        public RemoteFileManager()
        {
            // Default storage directory inside application folder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _storageRoot = Path.GetFullPath(Path.Combine(baseDir, "ServerStorage"));

            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }

            _storageRootWithSeparator = _storageRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? _storageRoot
                : _storageRoot + Path.DirectorySeparatorChar;

            EnsureSampleFilesCreated();
            SetupFileSystemWatcher();
        }

        /// <summary>
        /// Explicit constructor allowing custom storage directory path.
        /// </summary>
        public RemoteFileManager(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerStorage");
            }

            _storageRoot = Path.GetFullPath(storagePath);
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }

            _storageRootWithSeparator = _storageRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? _storageRoot
                : _storageRoot + Path.DirectorySeparatorChar;

            EnsureSampleFilesCreated();
            SetupFileSystemWatcher();
        }

        /// <summary>
        /// Pushes a real-time event notification to all connected clients.
        /// </summary>
        public static void PushNotification(string eventType, string message, string relativePath = "")
        {
            lock (_notificationLock)
            {
                long id = _nextEventId++;
                var notif = new ServerNotification(id, eventType, message, relativePath);
                _notifications.Add(notif);

                // Trim to recent 100 events
                if (_notifications.Count > 100)
                {
                    _notifications.RemoveAt(0);
                }
            }
        }

        private void SetupFileSystemWatcher()
        {
            try
            {
                if (_watcher != null) return;

                _watcher = new FileSystemWatcher(_storageRoot)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };

                _watcher.Created += (s, e) =>
                {
                    string rel = ToRelativePath(e.FullPath);
                    Log("PUSH_NEW", string.Format("Detected file/folder creation on server: '{0}'", rel), ConsoleColor.Green);
                    PushNotification("FILE_CREATED", string.Format("Item '{0}' was created on server.", rel), rel);
                };

                _watcher.Deleted += (s, e) =>
                {
                    string rel = ToRelativePath(e.FullPath);
                    Log("PUSH_DEL", string.Format("Detected file/folder deletion on server: '{0}'", rel), ConsoleColor.Magenta);
                    PushNotification("FILE_DELETED", string.Format("Item '{0}' was deleted from server.", rel), rel);
                };

                _watcher.Renamed += (s, e) =>
                {
                    string oldRel = ToRelativePath(e.OldFullPath);
                    string newRel = ToRelativePath(e.FullPath);
                    Log("PUSH_REN", string.Format("Detected rename on server: '{0}' -> '{1}'", oldRel, newRel), ConsoleColor.Yellow);
                    PushNotification("FILE_RENAMED", string.Format("Item '{0}' was renamed to '{1}' on server.", oldRel, newRel), newRel);
                };

                _watcher.EnableRaisingEvents = true;
                Log("SYSTEM", "Live FileSystemWatcher activated on ServerStorage.", ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                Log("WARN", "Could not start FileSystemWatcher: " + ex.Message, ConsoleColor.Yellow);
            }
        }

        /// <summary>
        /// Crucial for .NET Remoting: Returning null grants this remote object an infinite lease time.
        /// By default, .NET Remoting leases expire after 5 minutes of inactivity, causing disconnected object errors.
        /// Infinite lease ensures stable demonstration and viva presentations.
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region Path Security and Validation

        /// <summary>
        /// Resolves a client-supplied relative path against ServerStorage and strictly verifies
        /// that the path does NOT escape the ServerStorage directory (Preventing Path Traversal attacks).
        /// </summary>
        private string ResolveAndValidatePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return _storageRoot;
            }

            // Normalize path separators to standard Windows backslash
            string clean = relativePath.Trim().Replace('/', Path.DirectorySeparatorChar);

            // Strip leading slashes to prevent absolute root traversal
            while (clean.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                clean = clean.Substring(1);
            }

            // Combine and fully expand canonical path
            string fullPath = Path.GetFullPath(Path.Combine(_storageRoot, clean));

            // Verify fullPath starts with _storageRoot
            bool isRoot = fullPath.Equals(_storageRoot, StringComparison.OrdinalIgnoreCase);
            bool isInside = fullPath.StartsWith(_storageRootWithSeparator, StringComparison.OrdinalIgnoreCase);

            if (!isRoot && !isInside)
            {
                Log("SECURITY", string.Format("BLOCKED path traversal attempt: '{0}' -> '{1}'", relativePath, fullPath), ConsoleColor.Red);
                throw new UnauthorizedAccessException("Security Error: Access outside ServerStorage is strictly prohibited.");
            }

            return fullPath;
        }

        /// <summary>
        /// Converts a server full physical path to a clean relative path suitable for client display.
        /// </summary>
        private string ToRelativePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || fullPath.Equals(_storageRoot, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            if (fullPath.StartsWith(_storageRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                string rel = fullPath.Substring(_storageRootWithSeparator.Length);
                return rel.Replace(Path.DirectorySeparatorChar, '/');
            }

            return fullPath;
        }

        #endregion

        #region Diagnostic and Metadata Operations

        public bool Ping()
        {
            Log("CONNECT", "Client connection ping received successfully.", ConsoleColor.Green);
            return true;
        }

        public List<ServerNotification> GetPendingNotifications(long lastEventId)
        {
            lock (_notificationLock)
            {
                var list = new List<ServerNotification>();
                foreach (var n in _notifications)
                {
                    if (n.EventId > lastEventId)
                    {
                        list.Add(n);
                    }
                }
                return list;
            }
        }

        public FileOperationResult BroadcastMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return FileOperationResult.Fail("Broadcast message cannot be empty.");
            }

            PushNotification("SERVER_MESSAGE", message);
            Log("BROADCAST", string.Format("Administrative broadcast sent to clients: \"{0}\"", message), ConsoleColor.Yellow);
            return FileOperationResult.Ok("Message broadcasted successfully.");
        }

        public ServerStorageStats GetStorageStats()
        {
            try
            {
                Log("STATS", "Calculating server storage statistics.", ConsoleColor.Cyan);

                var dirInfo = new DirectoryInfo(_storageRoot);
                int fileCount = 0;
                int dirCount = 0;
                long totalBytes = 0;

                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    fileCount++;
                    totalBytes += file.Length;
                }

                foreach (var dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                {
                    dirCount++;
                }

                return new ServerStorageStats
                {
                    TotalFiles = fileCount,
                    TotalDirectories = dirCount,
                    TotalSizeBytes = totalBytes,
                    ServerMachineName = Environment.MachineName,
                    ServerTime = DateTime.Now,
                    StorageRootPath = _storageRoot
                };
            }
            catch (Exception ex)
            {
                Log("ERROR", "Failed to retrieve storage stats: " + ex.Message, ConsoleColor.Red);
                return new ServerStorageStats
                {
                    ServerMachineName = Environment.MachineName,
                    ServerTime = DateTime.Now,
                    StorageRootPath = _storageRoot
                };
            }
        }

        #endregion

        #region Directory Listing Operations

        public List<FileItem> GetFiles(string relativeDirectoryPath)
        {
            try
            {
                string targetDir = ResolveAndValidatePath(relativeDirectoryPath);
                Log("LIST_FILES", string.Format("Listing files in '{0}'", ToRelativePath(targetDir)), ConsoleColor.Gray);

                if (!Directory.Exists(targetDir))
                {
                    Log("WARN", string.Format("Directory not found: '{0}'", relativeDirectoryPath), ConsoleColor.Yellow);
                    return new List<FileItem>();
                }

                var result = new List<FileItem>();
                var dirInfo = new DirectoryInfo(targetDir);

                foreach (var fi in dirInfo.GetFiles())
                {
                    result.Add(new FileItem
                    {
                        Name = fi.Name,
                        RelativePath = ToRelativePath(fi.FullName),
                        IsDirectory = false,
                        Size = fi.Length,
                        Extension = fi.Extension,
                        CreationTime = fi.CreationTime,
                        LastWriteTime = fi.LastWriteTime
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Error in GetFiles('{0}'): {1}", relativeDirectoryPath, ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        public List<FileItem> GetDirectories(string relativeDirectoryPath)
        {
            try
            {
                string targetDir = ResolveAndValidatePath(relativeDirectoryPath);
                Log("LIST_DIRS", string.Format("Listing subdirectories in '{0}'", ToRelativePath(targetDir)), ConsoleColor.Gray);

                if (!Directory.Exists(targetDir))
                {
                    Log("WARN", string.Format("Directory not found: '{0}'", relativeDirectoryPath), ConsoleColor.Yellow);
                    return new List<FileItem>();
                }

                var result = new List<FileItem>();
                var dirInfo = new DirectoryInfo(targetDir);

                foreach (var di in dirInfo.GetDirectories())
                {
                    result.Add(new FileItem
                    {
                        Name = di.Name,
                        RelativePath = ToRelativePath(di.FullName),
                        IsDirectory = true,
                        Size = 0,
                        Extension = "",
                        CreationTime = di.CreationTime,
                        LastWriteTime = di.LastWriteTime
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Error in GetDirectories('{0}'): {1}", relativeDirectoryPath, ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        public List<FileItem> GetAllItems(string relativeDirectoryPath)
        {
            var combined = new List<FileItem>();
            combined.AddRange(GetDirectories(relativeDirectoryPath));
            combined.AddRange(GetFiles(relativeDirectoryPath));
            return combined;
        }

        #endregion

        #region Text File Operations

        public string ReadFile(string relativeFilePath)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("READ", string.Format("Reading text file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Cyan);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException(string.Format("File not found on server: {0}", relativeFilePath));
                }

                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to read file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        public FileOperationResult CreateFile(string relativeFilePath, string content)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("CREATE_FILE", string.Format("Creating new file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Green);

                if (File.Exists(fullPath))
                {
                    return FileOperationResult.Fail("File already exists on the server.", null, relativeFilePath);
                }

                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(fullPath, content ?? string.Empty);
                return FileOperationResult.Ok("File created successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to create file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to create file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public FileOperationResult WriteFile(string relativeFilePath, string content)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("WRITE_FILE", string.Format("Writing to file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Green);

                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(fullPath, content ?? string.Empty);
                return FileOperationResult.Ok("File written successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to write to file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to write to file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public FileOperationResult AppendToFile(string relativeFilePath, string content)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("APPEND_FILE", string.Format("Appending to file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Green);

                if (!File.Exists(fullPath))
                {
                    return FileOperationResult.Fail("Cannot append. File does not exist.", null, relativeFilePath);
                }

                File.AppendAllText(fullPath, content ?? string.Empty);
                return FileOperationResult.Ok("Content appended successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to append to file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to append to file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public FileOperationResult UpdateFile(string relativeFilePath, string content)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("UPDATE_FILE", string.Format("Updating file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Green);

                if (!File.Exists(fullPath))
                {
                    return FileOperationResult.Fail("Cannot update. File does not exist.", null, relativeFilePath);
                }

                File.WriteAllText(fullPath, content ?? string.Empty);
                return FileOperationResult.Ok("File updated successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to update file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to update file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        #endregion

        #region File Manipulation Operations

        public FileOperationResult DeleteFile(string relativeFilePath)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("DELETE_FILE", string.Format("Deleting file '{0}'", ToRelativePath(fullPath)), ConsoleColor.Magenta);

                if (!File.Exists(fullPath))
                {
                    return FileOperationResult.Fail("File does not exist.", null, relativeFilePath);
                }

                File.Delete(fullPath);
                return FileOperationResult.Ok("File deleted successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to delete file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to delete file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public FileOperationResult RenameFile(string relativeFilePath, string newFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newFileName))
                {
                    return FileOperationResult.Fail("New file name cannot be empty.");
                }

                if (newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return FileOperationResult.Fail("New file name contains invalid characters.");
                }

                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("RENAME_FILE", string.Format("Renaming file '{0}' to '{1}'", ToRelativePath(fullPath), newFileName), ConsoleColor.Yellow);

                if (!File.Exists(fullPath))
                {
                    return FileOperationResult.Fail("File does not exist.", null, relativeFilePath);
                }

                string directory = Path.GetDirectoryName(fullPath);
                string newFullPath = Path.Combine(directory, newFileName);

                // Re-verify new target path
                if (File.Exists(newFullPath))
                {
                    return FileOperationResult.Fail("A file with the new name already exists.");
                }

                File.Move(fullPath, newFullPath);
                return FileOperationResult.Ok("File renamed successfully.", ToRelativePath(newFullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to rename file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to rename file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public FileOperationResult CopyFile(string sourceRelativePath, string destRelativePath)
        {
            try
            {
                string srcFull = ResolveAndValidatePath(sourceRelativePath);
                string dstFull = ResolveAndValidatePath(destRelativePath);

                Log("COPY_FILE", string.Format("Copying file '{0}' to '{1}'", ToRelativePath(srcFull), ToRelativePath(dstFull)), ConsoleColor.Yellow);

                if (!File.Exists(srcFull))
                {
                    return FileOperationResult.Fail("Source file does not exist.", null, sourceRelativePath);
                }

                // If dest is a directory, append source file name
                if (Directory.Exists(dstFull))
                {
                    dstFull = Path.Combine(dstFull, Path.GetFileName(srcFull));
                }

                string destDir = Path.GetDirectoryName(dstFull);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(srcFull, dstFull, true);
                return FileOperationResult.Ok("File copied successfully.", ToRelativePath(dstFull));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to copy file: {0}", ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to copy file: " + ex.Message, ex.ToString());
            }
        }

        public FileOperationResult MoveFile(string sourceRelativePath, string destRelativePath)
        {
            try
            {
                string srcFull = ResolveAndValidatePath(sourceRelativePath);
                string dstFull = ResolveAndValidatePath(destRelativePath);

                Log("MOVE_FILE", string.Format("Moving file '{0}' to '{1}'", ToRelativePath(srcFull), ToRelativePath(dstFull)), ConsoleColor.Yellow);

                if (!File.Exists(srcFull))
                {
                    return FileOperationResult.Fail("Source file does not exist.", null, sourceRelativePath);
                }

                // If dest is a directory, append source file name
                if (Directory.Exists(dstFull))
                {
                    dstFull = Path.Combine(dstFull, Path.GetFileName(srcFull));
                }

                if (File.Exists(dstFull))
                {
                    return FileOperationResult.Fail("Destination file already exists.", null, destRelativePath);
                }

                string destDir = Path.GetDirectoryName(dstFull);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Move(srcFull, dstFull);
                return FileOperationResult.Ok("File moved successfully.", ToRelativePath(dstFull));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to move file: {0}", ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to move file: " + ex.Message, ex.ToString());
            }
        }

        #endregion

        #region Directory Manipulation Operations

        public FileOperationResult CreateDirectory(string relativeDirectoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativeDirectoryPath))
                {
                    return FileOperationResult.Fail("Directory name cannot be empty.");
                }

                string fullPath = ResolveAndValidatePath(relativeDirectoryPath);
                Log("CREATE_DIR", string.Format("Creating directory '{0}'", ToRelativePath(fullPath)), ConsoleColor.Green);

                if (Directory.Exists(fullPath))
                {
                    return FileOperationResult.Fail("Directory already exists.", null, relativeDirectoryPath);
                }

                Directory.CreateDirectory(fullPath);
                return FileOperationResult.Ok("Directory created successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to create directory '{0}': {1}", relativeDirectoryPath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to create directory: " + ex.Message, ex.ToString(), relativeDirectoryPath);
            }
        }

        public FileOperationResult DeleteDirectory(string relativeDirectoryPath)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeDirectoryPath);

                // Prevent deleting root ServerStorage
                if (fullPath.Equals(_storageRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return FileOperationResult.Fail("Cannot delete the root storage directory.");
                }

                Log("DELETE_DIR", string.Format("Deleting directory '{0}'", ToRelativePath(fullPath)), ConsoleColor.Magenta);

                if (!Directory.Exists(fullPath))
                {
                    return FileOperationResult.Fail("Directory does not exist.", null, relativeDirectoryPath);
                }

                Directory.Delete(fullPath, true);
                return FileOperationResult.Ok("Directory deleted successfully.", ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to delete directory '{0}': {1}", relativeDirectoryPath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to delete directory: " + ex.Message, ex.ToString(), relativeDirectoryPath);
            }
        }

        public FileOperationResult RenameDirectory(string relativeDirectoryPath, string newDirectoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newDirectoryName))
                {
                    return FileOperationResult.Fail("New directory name cannot be empty.");
                }

                if (newDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return FileOperationResult.Fail("Directory name contains invalid characters.");
                }

                string fullPath = ResolveAndValidatePath(relativeDirectoryPath);

                if (fullPath.Equals(_storageRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return FileOperationResult.Fail("Cannot rename the root storage directory.");
                }

                Log("RENAME_DIR", string.Format("Renaming directory '{0}' to '{1}'", ToRelativePath(fullPath), newDirectoryName), ConsoleColor.Yellow);

                if (!Directory.Exists(fullPath))
                {
                    return FileOperationResult.Fail("Directory does not exist.", null, relativeDirectoryPath);
                }

                string parent = Path.GetDirectoryName(fullPath);
                string newFullPath = Path.Combine(parent, newDirectoryName);

                if (Directory.Exists(newFullPath))
                {
                    return FileOperationResult.Fail("A directory with the new name already exists.");
                }

                Directory.Move(fullPath, newFullPath);
                return FileOperationResult.Ok("Directory renamed successfully.", ToRelativePath(newFullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to rename directory '{0}': {1}", relativeDirectoryPath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to rename directory: " + ex.Message, ex.ToString(), relativeDirectoryPath);
            }
        }

        #endregion

        #region Binary Transfer Operations (Upload & Download)

        public FileOperationResult UploadFile(string relativeFilePath, byte[] data)
        {
            try
            {
                if (data == null)
                {
                    return FileOperationResult.Fail("Upload payload data is null.");
                }

                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("UPLOAD", string.Format("Receiving upload: '{0}' ({1} bytes)", ToRelativePath(fullPath), data.Length), ConsoleColor.Cyan);

                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(fullPath, data);
                return FileOperationResult.Ok(string.Format("File '{0}' uploaded successfully ({1} bytes).", Path.GetFileName(fullPath), data.Length), ToRelativePath(fullPath));
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to upload file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                return FileOperationResult.Fail("Failed to upload file: " + ex.Message, ex.ToString(), relativeFilePath);
            }
        }

        public byte[] DownloadFile(string relativeFilePath)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("DOWNLOAD", string.Format("Serving download for '{0}'", ToRelativePath(fullPath)), ConsoleColor.Cyan);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException(string.Format("File not found on server: {0}", relativeFilePath));
                }

                byte[] bytes = File.ReadAllBytes(fullPath);
                Log("DOWNLOAD", string.Format("Transmitted '{0}' ({1} bytes)", ToRelativePath(fullPath), bytes.Length), ConsoleColor.DarkCyan);
                return bytes;
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to download file '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        #endregion

        #region Search and File Info

        public List<FileItem> SearchFiles(string searchPattern, string relativeDirectoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    searchPattern = "*";
                }

                // If user entered e.g. "report", match "*report*"
                if (!searchPattern.Contains("*") && !searchPattern.Contains("?"))
                {
                    searchPattern = "*" + searchPattern + "*";
                }

                string targetDir = ResolveAndValidatePath(relativeDirectoryPath);
                Log("SEARCH", string.Format("Searching for '{0}' in '{1}'", searchPattern, ToRelativePath(targetDir)), ConsoleColor.Yellow);

                var result = new List<FileItem>();
                if (!Directory.Exists(targetDir))
                {
                    return result;
                }

                var dirInfo = new DirectoryInfo(targetDir);

                // Search matching subdirectories
                foreach (var di in dirInfo.GetDirectories(searchPattern, SearchOption.AllDirectories))
                {
                    result.Add(new FileItem
                    {
                        Name = di.Name,
                        RelativePath = ToRelativePath(di.FullName),
                        IsDirectory = true,
                        Size = 0,
                        Extension = "",
                        CreationTime = di.CreationTime,
                        LastWriteTime = di.LastWriteTime
                    });
                }

                // Search matching files
                foreach (var fi in dirInfo.GetFiles(searchPattern, SearchOption.AllDirectories))
                {
                    result.Add(new FileItem
                    {
                        Name = fi.Name,
                        RelativePath = ToRelativePath(fi.FullName),
                        IsDirectory = false,
                        Size = fi.Length,
                        Extension = fi.Extension,
                        CreationTime = fi.CreationTime,
                        LastWriteTime = fi.LastWriteTime
                    });
                }

                Log("SEARCH", string.Format("Search matched {0} item(s).", result.Count), ConsoleColor.Gray);
                return result;
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Search failed: {0}", ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        public FileItem GetFileInfo(string relativeFilePath)
        {
            try
            {
                string fullPath = ResolveAndValidatePath(relativeFilePath);
                Log("INFO", string.Format("Retrieving metadata for '{0}'", ToRelativePath(fullPath)), ConsoleColor.Gray);

                if (File.Exists(fullPath))
                {
                    var fi = new FileInfo(fullPath);
                    return new FileItem
                    {
                        Name = fi.Name,
                        RelativePath = ToRelativePath(fi.FullName),
                        IsDirectory = false,
                        Size = fi.Length,
                        Extension = fi.Extension,
                        CreationTime = fi.CreationTime,
                        LastWriteTime = fi.LastWriteTime
                    };
                }
                else if (Directory.Exists(fullPath))
                {
                    var di = new DirectoryInfo(fullPath);
                    return new FileItem
                    {
                        Name = di.Name,
                        RelativePath = ToRelativePath(di.FullName),
                        IsDirectory = true,
                        Size = 0,
                        Extension = "",
                        CreationTime = di.CreationTime,
                        LastWriteTime = di.LastWriteTime
                    };
                }
                else
                {
                    throw new FileNotFoundException(string.Format("Path '{0}' does not exist on server.", relativeFilePath));
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", string.Format("Failed to get info for '{0}': {1}", relativeFilePath, ex.Message), ConsoleColor.Red);
                throw;
            }
        }

        #endregion

        #region Helper: Sample Files & Logging

        private void EnsureSampleFilesCreated()
        {
            try
            {
                string docsDir = Path.Combine(_storageRoot, "Documents");
                string imgDir = Path.Combine(_storageRoot, "Images");

                if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);
                if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                string welcomeFile = Path.Combine(_storageRoot, "welcome.txt");
                if (!File.Exists(welcomeFile))
                {
                    File.WriteAllText(welcomeFile,
                        "=====================================================\r\n" +
                        "  Welcome to Remote File Management System (.NET Remoting)\r\n" +
                        "=====================================================\r\n\r\n" +
                        "This server allows remote clients to manage files and folders\r\n" +
                        "over a high-performance TCP Remoting channel.\r\n\r\n" +
                        "Supported Features:\r\n" +
                        "- Create, Read, Write, Append, Update, Delete text files\r\n" +
                        "- Rename, Copy, and Move files remotely\r\n" +
                        "- Create, Delete, and Rename directories\r\n" +
                        "- Upload and Download any binary file format (.pdf, .jpg, .docx, .zip)\r\n" +
                        "- Recursive file search and metadata inspection\r\n" +
                        "- Strict path traversal security enforcement\r\n");
                }

                string notesFile = Path.Combine(docsDir, "notes.txt");
                if (!File.Exists(notesFile))
                {
                    File.WriteAllText(notesFile,
                        "PROJECT NOTES:\r\n" +
                        "- Technology: .NET Remoting (Legacy .NET Framework)\r\n" +
                        "- Protocol: TCP Channel with Binary Formatter\r\n" +
                        "- Object Lifetime: Infinite lease (InitializeLifetimeService)\r\n" +
                        "- Security: Sandboxed inside ServerStorage directory\r\n" +
                        "- Architecture: Client-Server with Shared Contracts Library\r\n");
                }

                string reportFile = Path.Combine(docsDir, "report.txt");
                if (!File.Exists(reportFile))
                {
                    File.WriteAllText(reportFile,
                        "COLLEGE PROJECT EVALUATION REPORT\r\n" +
                        "Course: Distributed Computing Systems\r\n" +
                        "Topic: Remote File Management System Using .NET Remoting\r\n" +
                        "Date: September 2026\r\n" +
                        "Status: All test cases passed.\r\n");
                }

                string imgSampleFile = Path.Combine(imgDir, "sample.txt");
                if (!File.Exists(imgSampleFile))
                {
                    File.WriteAllText(imgSampleFile,
                        "Image placeholder folder. You can upload real .jpg, .png, or .pdf files here!\r\n");
                }
            }
            catch (Exception ex)
            {
                Log("WARN", "Could not initialize sample files: " + ex.Message, ConsoleColor.Yellow);
            }
        }

        private static void Log(string tag, string message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[{0:HH:mm:ss}] ", DateTime.Now);

            Console.ForegroundColor = color;
            Console.Write("[{0}] ", tag.PadRight(11));

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);

            Console.ForegroundColor = old;
        }

        #endregion
    }
}
