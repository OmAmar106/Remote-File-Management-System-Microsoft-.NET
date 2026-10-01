# Academic Project Report

## Remote File Management System Using .NET Remoting

**Department of Computer Science & Engineering / Information Technology**  
**Course**: Distributed Computing Systems / Advanced Software Engineering  
**Academic Year**: 2026  

---

### Table of Contents
1. [Abstract](#1-abstract)
2. [Introduction](#2-introduction)
3. [Problem Statement](#3-problem-statement)
4. [Objectives](#4-objectives)
5. [Existing System](#5-existing-system)
6. [Proposed System](#6-proposed-system)
7. [System Architecture](#7-system-architecture)
8. [Modules Description](#8-modules-description)
9. [Functional Requirements](#9-functional-requirements)
10. [Non-Functional Requirements](#10-non-functional-requirements)
11. [Technologies & Tools](#11-technologies--tools)
12. [Implementation Details](#12-implementation-details)
13. [Testing Strategy & Results](#13-testing-strategy--results)
14. [User Interface Design & Screenshots Section](#14-user-interface-design--screenshots-section)
15. [Advantages](#15-advantages)
16. [Limitations](#16-limitations)
17. [Future Scope](#17-future-scope)
18. [Conclusion](#18-conclusion)

---

### 1. Abstract

Distributed computing systems allow distinct computing entities across a network to collaborate, share resources, and invoke procedures seamlessly as if they were executing on a single physical machine. This project, **"Remote File Management System Using .NET Remoting"**, provides an educational, secure, and robust client-server architecture enabling remote file manipulation, directory exploration, and real-time binary transfers.

The communication layer is engineered strictly around **Microsoft .NET Remoting**, utilizing a high-performance binary-formatted TCP channel. Unlike modern REST or HTTP-based paradigms where developers manually compose URL endpoints, serialization formats, and HTTP verbs, .NET Remoting demonstrates Remote Procedure Calls (RPC) in an object-oriented environment where clients communicate through strongly typed interfaces and transparent proxies.

The system incorporates comprehensive file management capabilities (Create, Read, Update, Append, Delete, Rename, Copy, Move, Upload, Download, Search, and Metadata Inspection) alongside an active defense system against path traversal attacks by sandboxing all operations within a dedicated `ServerStorage` root.

---

### 2. Introduction

File sharing and management are ubiquitous across enterprise networks and cloud platforms. However, developing an understanding of distributed mechanisms requires studying the evolution of remote method invocation paradigms. 

Microsoft .NET Remoting was introduced as part of the .NET Framework as a successor to Distributed Component Object Model (DCOM). It enables objects in different application domains (AppDomains), processes, or network-connected hosts to communicate with one another.

In this project, a Windows Forms desktop application acts as the client front-end. The user initiates file operations which are dispatched across a TCP socket to a standalone server console host. The server executes physical file system operations within its sandboxed directory and marshals results back to the client.

---

### 3. Problem Statement

Conventional distributed file sharing methods (such as Windows SMB network shares or FTP) often present significant drawbacks in educational and custom application settings:
1. **Direct Disk Access Vulnerability**: Exposing a physical directory via OS file sharing allows client machines direct access to the storage volume, increasing the risk of accidental deletion, ransomware infection, or unauthorized disk inspection.
2. **Lack of Procedural Control**: Basic file protocols lack custom programmatic interception for access logging, security validations, and real-time event notifications.
3. **Complexity of Modern Web Frameworks**: Modern frameworks (ASP.NET Core, gRPC, REST) abstract away foundational distributed computing concepts like object marshaling, transparent proxies, and object lifetimes, making them less suitable for studying classic RPC mechanics.

**The Solution**: Build a bespoke Remote File Management System where the client holds no physical handle to the server's disk and must interact exclusively through remote method invocation on an exposed interface.

---

### 4. Objectives

- **Implement Pure .NET Remoting**: Utilize `System.Runtime.Remoting`, `MarshalByRefObject`, and `TcpChannel`.
- **Strict Separation of Concerns**: Maintain three distinct assemblies: `Shared` (interface & models), `Server` (host & implementation), and `Client` (GUI presentation).
- **Comprehensive File System Operations**:
  - CRUD operations on text files (Create, Read, Update, Append, Delete).
  - Directory management (Create, Delete, Rename, Navigate, List).
  - File organization (Rename, Copy, Move).
  - Binary file transfer (Upload, Download of arbitrary file formats).
  - Discovery & Metadata (Recursive search and property inspection).
- **Security by Design**: Prevent path traversal attacks (e.g., `../../`) through strict canonical path normalization and sandbox enforcement.
- **Real-Time Operational Logging**: Provide color-coded, timestamped audit logs on the server console for all client requests.

---

### 5. Existing System

Existing file management systems in distributed environments typically rely on:
- **FTP (File Transfer Protocol)**: Simple file storage protocol, but relies on plain-text credentials by default and requires separate control and data connections.
- **SMB / CIFS (Windows File Sharing)**: Operating-system level file shares expose raw directories over SMB (port 445). Does not provide fine-grained programmatic control or application-level auditing.
- **REST APIs**: Require heavy serialization overhead (JSON/XML) and lacks stateful object identity.

#### Drawbacks of Existing Systems:
- Heavyweight server configuration requirements.
- Lack of pedagogical transparency for studying RPC.
- Potential security risks when granting raw OS-level file permissions.

---

### 6. Proposed System

The proposed system addresses these challenges by introducing an application-level distributed object broker:
1. **Remote Interface Pattern**: Operations are defined strictly in an interface contract (`IRemoteFileManager`).
2. **Transparent Proxy Invocation**: When the client calls a method on the proxy, the .NET Remoting infrastructure serializes the call parameters using the Binary Formatter, routes it across a dedicated TCP port (default 9000), invokes the server-side `MarshalByRefObject`, and returns the serializable result.
3. **No Direct Storage Exposure**: The client application never receives absolute server paths or network share UNC handles. All interactions use relative virtual paths inside `ServerStorage`.
4. **Resilient Lifetime Management**: Overrides default .NET Remoting lease timeouts to ensure uninterrupted demonstration sessions.

---

### 7. System Architecture

```
+-------------------------------------------------------------------------+
|                              CLIENT TIER                                |
|  - Windows Forms Desktop GUI (MainForm, FileEditorForm, InputDialog)    |
|  - Obtains TransparentProxy to IRemoteFileManager via Activator.GetObject|
|  - Client TCP Channel with BinaryClientFormatterSinkProvider            |
+------------------------------------+------------------------------------+
                                     |
                                     | .NET Remoting RPC Calls
                                     | (TCP Port 9000, Binary Formatter)
                                     v
+------------------------------------+------------------------------------+
|                              SERVER TIER                                |
|  - Console Application Host (ServerProgram.cs)                          |
|  - TcpServerChannel with BinaryServerFormatterSinkProvider (TypeFilter=Full)
|  - RemoteFileManager : MarshalByRefObject, IRemoteFileManager           |
|  - Infinite Lifetime Lease (InitializeLifetimeService returns null)     |
|  - Security Layer: Canonical path normalization & sandbox verification  |
|  - Timestamped Console Logger                                           |
+------------------------------------+------------------------------------+
                                     |
                                     | Physical File I/O
                                     v
+-------------------------------------------------------------------------+
|                             STORAGE TIER                                |
|  - Dedicated ServerStorage directory on Server host machine             |
|  - Subdirectories: /Documents, /Images, user-created folders            |
|  - Supports all file types: .txt, .pdf, .jpg, .png, .docx, .zip, etc.  |
+-------------------------------------------------------------------------+
```

---

### 8. Modules Description

The application is structured into four distinct modules:

#### Module 1: `RemoteFileManagement.Shared`
- **Purpose**: Defines shared types, interfaces, and data models compiled into a standalone class library referenced by both the client and server.
- **Key Artifacts**:
  - `IRemoteFileManager`: The remote service contract specifying all file and directory management methods.
  - `FileItem`: Serializable data model representing file or folder metadata (Name, RelativePath, IsDirectory, Size, FormattedSize, Extension, CreationTime, LastWriteTime).
  - `FileOperationResult`: Serializable response model containing `Success` (bool), `Message` (string), `ErrorDetails`, and `AffectedPath`.
  - `ServerStorageStats`: Serializable model for aggregate storage metrics (TotalFiles, TotalDirectories, TotalSizeBytes, ServerMachineName, ServerTime).

#### Module 2: `RemoteFileManagement.Server`
- **Purpose**: Standalone console application responsible for hosting the remote object and executing file operations.
- **Key Artifacts**:
  - `RemoteFileManager`: The server-side implementation of `IRemoteFileManager`. Inherits from `MarshalByRefObject` so that calls are dispatched to this instance rather than copied to the client.
  - `ServerProgram`: Configures and registers the `TcpChannel`, binds port 9000, registers the well-known service type as `Singleton`, and manages diagnostic output.
  - `Server.config`: XML configuration file for declarative Remoting channel setup.
  - `ServerStorage`: Physical directory containing server-hosted files.

#### Module 3: `RemoteFileManagement.Client`
- **Purpose**: Graphical user interface built with Windows Forms providing an intuitive file explorer experience.
- **Key Artifacts**:
  - `MainForm`: Main explorer window containing Connection Bar, Navigation Toolbar, Folder TreeView, File ListView, and Status Strip.
  - `FileEditorForm`: Text file editor dialog supporting Read, Write, Update, and Append operations.
  - `FolderSelectForm`: Remote folder selector used when copying or moving items between directories on the server.
  - `FilePropertiesForm`: Modal dialog showing complete file attributes and server location.
  - `InputDialog`: Clean prompt for collecting file/folder names.
  - `Client.config`: Declarative Remoting client channel configuration.

#### Module 4: `RemoteFileManagement.Tests`
- **Purpose**: Automated test harness validating all 19 functional and security test cases against a live running server process.

---

### 9. Functional Requirements

| Req ID | Description | Module |
|---|---|---|
| **FR-01** | The server must register a .NET Remoting TCP channel on a configurable port (default 9000). | Server |
| **FR-02** | The client must connect to the server using the host address and port, verifying connectivity via a `Ping()` remote invocation. | Client / Server |
| **FR-03** | The server must list all files and subdirectories located within any specified relative path inside `ServerStorage`. | Server |
| **FR-04** | The client must render remote directory structures in a hierarchical `TreeView` and display files in a multi-column `ListView`. | Client |
| **FR-05** | The client must allow creating new text files on the remote server with user-defined content. | Client / Server |
| **FR-06** | The client must allow reading the contents of a server text file and displaying it in a dedicated editor. | Client / Server |
| **FR-07** | The client must allow modifying and saving updated content back to the server (overwrite). | Client / Server |
| **FR-08** | The client must allow appending text to an existing remote file without altering preexisting data. | Client / Server |
| **FR-09** | The client must allow renaming files and directories on the server remotely. | Client / Server |
| **FR-10** | The client must allow copying server-side files to other server-side directories. | Client / Server |
| **FR-11** | The client must allow moving server-side files to other server-side directories. | Client / Server |
| **FR-12** | The client must allow permanently deleting files and directories from the server. | Client / Server |
| **FR-13** | The client must allow uploading any local client file (binary or text) as raw bytes (`byte[]`) to the server. | Client / Server |
| **FR-14** | The client must allow downloading any remote server file as raw bytes (`byte[]`) and saving it locally. | Client / Server |
| **FR-15** | The client must support recursive searching for files matching a keyword or pattern across the server storage. | Client / Server |
| **FR-16** | The server must block any attempt to escape `ServerStorage` using path traversal sequences (e.g. `../../`). | Server (Security) |
| **FR-17** | The server must log all operations with timestamps and categorical tags in the console. | Server |

---

### 10. Non-Functional Requirements

- **Performance**: Method invocations and small file transfers execute with sub-millisecond latencies across local loops.
- **Reliability & Availability**: Overriding `InitializeLifetimeService()` ensures the remote object does not expire during active sessions.
- **Security**: Strict path sandboxing prevents unauthorized access to the host file system.
- **Usability**: Clean Windows Forms GUI utilizing modern typography (Segoe UI) and intuitive controls (TreeView, ListView, Toolbar).
- **Maintainability & Portability**: Zero external third-party package dependencies. Compiles cleanly using standard .NET Framework 4.8 tooling.

---

### 11. Technologies & Tools

- **Programming Language**: C# (Version 7.3+)
- **Runtime Environment**: Microsoft .NET Framework 4.8
- **Core Namespace**: `System.Runtime.Remoting`, `System.Runtime.Remoting.Channels.Tcp`
- **Formatters**: `System.Runtime.Serialization.Formatters.Binary`
- **User Interface**: `System.Windows.Forms`, `System.Drawing`
- **Compiler & Build Tools**: Visual Studio 2022 / 2026 Community, MSBuild version 18.x

---

### 12. Implementation Details

#### 12.1 The Remote Contract
The interface `IRemoteFileManager` acts as the shared boundary:
```csharp
public interface IRemoteFileManager
{
    bool Ping();
    ServerStorageStats GetStorageStats();
    List<FileItem> GetFiles(string relativeDirectoryPath);
    List<FileItem> GetDirectories(string relativeDirectoryPath);
    string ReadFile(string relativeFilePath);
    FileOperationResult CreateFile(string relativeFilePath, string content);
    FileOperationResult WriteFile(string relativeFilePath, string content);
    FileOperationResult AppendToFile(string relativeFilePath, string content);
    FileOperationResult UpdateFile(string relativeFilePath, string content);
    FileOperationResult DeleteFile(string relativeFilePath);
    FileOperationResult RenameFile(string relativeFilePath, string newFileName);
    FileOperationResult CopyFile(string sourceRelativePath, string destRelativePath);
    FileOperationResult MoveFile(string sourceRelativePath, string destRelativePath);
    FileOperationResult CreateDirectory(string relativeDirectoryPath);
    FileOperationResult DeleteDirectory(string relativeDirectoryPath);
    FileOperationResult RenameDirectory(string relativeDirectoryPath, string newDirectoryName);
    FileOperationResult UploadFile(string relativeFilePath, byte[] data);
    byte[] DownloadFile(string relativeFilePath);
    List<FileItem> SearchFiles(string searchPattern, string relativeDirectoryPath);
    FileItem GetFileInfo(string relativeFilePath);
}
```

#### 12.2 Marshaling by Reference and Object Lifetime
`RemoteFileManager` inherits from `MarshalByRefObject`:
```csharp
public class RemoteFileManager : MarshalByRefObject, IRemoteFileManager
{
    // Override lifetime lease to infinite
    public override object InitializeLifetimeService()
    {
        return null;
    }
    // ...
}
```

#### 12.3 Path Traversal Protection
All relative paths from the client pass through `ResolveAndValidatePath`:
```csharp
private string ResolveAndValidatePath(string relativePath)
{
    if (string.IsNullOrWhiteSpace(relativePath)) return _storageRoot;

    string clean = relativePath.Trim().Replace('/', Path.DirectorySeparatorChar);
    while (clean.StartsWith(Path.DirectorySeparatorChar.ToString()))
        clean = clean.Substring(1);

    string fullPath = Path.GetFullPath(Path.Combine(_storageRoot, clean));

    bool isRoot = fullPath.Equals(_storageRoot, StringComparison.OrdinalIgnoreCase);
    bool isInside = fullPath.StartsWith(_storageRootWithSeparator, StringComparison.OrdinalIgnoreCase);

    if (!isRoot && !isInside)
    {
        Log("SECURITY", string.Format("BLOCKED path traversal attempt: '{0}'", relativePath), ConsoleColor.Red);
        throw new UnauthorizedAccessException("Security Error: Access outside ServerStorage is strictly prohibited.");
    }

    return fullPath;
}
```

---

### 13. Testing Strategy & Results

The project was validated using a dual testing approach:
1. **Interactive Manual Testing**: Operating the Windows Forms client GUI against the running server.
2. **Automated Live Integration Testing**: A dedicated C# test suite (`RemoteFileManagement.Tests.exe`) executing 19 test cases against the live TCP Remoting server process.

#### Test Execution Summary:
- **Total Test Cases Executed**: 19
- **Test Cases Passed**: 19
- **Test Cases Failed**: 0
- **Pass Rate**: 100%

All operations—including file creation, reading, updating, appending, renaming, copying, moving, binary uploading, binary downloading, directory manipulation, path traversal attack rejection, and missing file error handling—passed with 100% accuracy.

---

### 14. User Interface Design & Screenshots Section

#### 14.1 Main Explorer Window
The main window contains:
- **Top Connection Panel**: Allows specifying Host (e.g. `localhost`), Port (e.g. `9000`), Connect/Disconnect buttons, and live connection status indicator.
- **Navigation Bar**: Quick navigation buttons (Root, Up, Refresh), current path breadcrumb display, and keyword search box.
- **Left Panel (TreeView)**: Displays the server's directory hierarchy.
- **Right Panel (ListView)**: Displays items in Details mode with columns: *Name*, *Type*, *Size*, *Date Modified*.
- **Toolbar**: Quick action buttons for New File, New Folder, Upload, Download, Read, Edit, Rename, Copy, Move, Delete, and Properties.

```
+----------------------------------------------------------------------------------------------------+
|  Remote File Management System (.NET Remoting)                                            - [X]    |
+----------------------------------------------------------------------------------------------------+
|  Server Host: [ localhost     ] Port: [ 9000 ]  [⚡ Connect] [Disconnect]  ● Connected ✓           |
+----------------------------------------------------------------------------------------------------+
|  [🏠 Root] [⬆ Up] [🔄 Refresh]  Location: [ /Documents                               ] [🔍 Find]    |
+------------------------------------+---------------------------------------------------------------+
|  Server Folders                    |  Name              Type              Size         Date Modified   |
|  -----------------                 |  ------------------------------------------------------------ |
|  [-] ServerStorage (/)             |  📁 Archives       File Folder       --           2026-09-24 22:40|
|      [+] Documents                 |  📄 notes.txt      Text Document     250 B        2026-09-24 22:42|
|      [+] Images                    |  📄 report.txt     Text Document     185 B        2026-09-24 22:45|
|                                    |  📕 syllabus.pdf   PDF Document      1.4 MB       2026-09-24 22:48|
+------------------------------------+---------------------------------------------------------------+
|  Ready. Selected: notes.txt (250 B)               | 1 folder(s), 3 file(s) | tcp://localhost:9000  |
+----------------------------------------------------------------------------------------------------+
```

#### 14.2 Text File Editor Window
The editor dialog allows reading text files, making live edits, appending lines, toggling word wrap, and saving directly back to the server over .NET Remoting.

---

### 15. Advantages

1. **True Distributed Object Paradigm**: Directly illustrates Remote Procedure Call (RPC) principles without web/REST abstractions.
2. **High Transport Performance**: Binary formatting across raw TCP sockets incurs minimal serialization overhead compared to JSON over HTTP.
3. **Robust Security Boundaries**: Sandboxed file system operations prevent path escape vulnerabilities.
4. **Binary & Text File Agnostic**: Handles arbitrary binary formats (.pdf, .jpg, .zip, .docx) via byte array streams alongside dedicated text editor tools.
5. **Clean Architecture**: Strong decoupling across Shared, Server, and Client layers.

---

### 16. Limitations

1. **Firewall / Network Traversal**: Raw TCP channels on port 9000 require open firewall ports and do not traverse HTTP proxy servers as easily as WebSockets or HTTPS.
2. **Platform Dependency**: .NET Remoting is specific to the .NET Framework on Windows platforms.
3. **In-Memory Byte Array Buffers**: Binary upload/download loads the entire file into a `byte[]` payload, making it best suited for files under ~200 MB rather than multi-gigabyte video files.

---

### 17. Future Scope

1. **Chunked Streamed Transfers**: Implement chunked stream buffers to transfer multi-gigabyte files with low memory footprint.
2. **User Authentication & Permissions**: Integrate username/password authentication with Role-Based Access Control (RBAC).
3. **Transport Layer Security (TLS/SSL)**: Encrypt the TCP Remoting channel using SSL certificates.
4. **File Versioning & Revision History**: Automatically keep timestamped backup revisions on the server when files are updated.

---

### 18. Conclusion

The **Remote File Management System Using .NET Remoting** project successfully fulfills all academic, technological, and architectural objectives. It provides a complete, working demonstration of distributed systems programming in C# and .NET Framework 4.8.

By enforcing strict separation between client-side presentation and server-side execution, implementing rigorous path traversal protection, supporting full file manipulation operations and real file transfer, the system serves as an exemplary college project and an educational tool for mastering distributed object-oriented computing.
