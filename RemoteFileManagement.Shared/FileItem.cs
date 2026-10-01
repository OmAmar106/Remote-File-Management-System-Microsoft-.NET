using System;

namespace RemoteFileManagement.Shared
{
    /// <summary>
    /// Represents file and folder metadata transferred between Server and Client via .NET Remoting.
    /// Marked [Serializable] so the .NET Remoting binary formatter can serialize it across processes.
    /// </summary>
    [Serializable]
    public class FileItem
    {
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }

        public string FormattedSize
        {
            get
            {
                if (IsDirectory) return "--";
                if (Size < 1024) return string.Format("{0} B", Size);
                if (Size < 1024 * 1024) return string.Format("{0:0.0} KB", Size / 1024.0);
                if (Size < 1024 * 1024 * 1024) return string.Format("{0:0.0} MB", Size / (1024.0 * 1024.0));
                return string.Format("{0:0.00} GB", Size / (1024.0 * 1024.0 * 1024.0));
            }
        }

        public string TypeDescription
        {
            get
            {
                if (IsDirectory) return "File Folder";
                string ext = (Extension ?? string.Empty).ToLowerInvariant();
                switch (ext)
                {
                    case ".txt": return "Text Document";
                    case ".pdf": return "PDF Document";
                    case ".doc":
                    case ".docx": return "Word Document";
                    case ".xls":
                    case ".xlsx": return "Excel Spreadsheet";
                    case ".jpg":
                    case ".jpeg": return "JPEG Image";
                    case ".png": return "PNG Image";
                    case ".gif": return "GIF Image";
                    case ".bmp": return "Bitmap Image";
                    case ".zip":
                    case ".rar":
                    case ".7z": return "Compressed Archive";
                    case ".cs": return "C# Source File";
                    case ".xml": return "XML Document";
                    case ".json": return "JSON File";
                    case ".config": return "Configuration File";
                    case ".html":
                    case ".htm": return "HTML Document";
                    case ".css": return "CSS Stylesheet";
                    case ".js": return "JavaScript File";
                    case ".csv": return "CSV Data File";
                    default:
                        return string.IsNullOrEmpty(ext) ? "File" : ext.ToUpper().TrimStart('.') + " File";
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, IsDirectory ? "DIR" : FormattedSize);
        }
    }
}
