# 📁 Remote File Management System — Using .NET Remoting

> **College Project** | Distributed Computing Systems | C# .NET Framework 4.8

---

## 🔰 What is This Project? (Simple Explanation)

Imagine you are sitting at **Computer A** (the Client), and there is a server computer — **Computer B** (the Server) — somewhere else on the network. You want to **manage files on Computer B without physically going there**.

This project does exactly that. Using a technology called **.NET Remoting**, the client computer can:

- 📂 Browse folders on the server
- 📄 Read, write, edit files on the server
- ⬆ Upload files **from client → server**
- ⬇ Download files **from server → client**
- 🔍 Search for files across the server
- 🗑 Delete, rename, move, copy files remotely
- 🔔 Receive **live push notifications** when anything changes on the server
- 📢 Get broadcast messages sent by the server administrator

**Key Point**: The client never directly touches the server's hard disk. Every action goes through a **network communication channel** — the client sends a request, the server executes it, and sends back the result. This is what makes it a *Distributed System*.

---

## 🧩 The Big Picture — How it Works

```
┌────────────────────────────────────┐
│     CLIENT (Your Computer)         │
│   - Windows GUI Application        │
│   - You click buttons, see files   │
│   - Sends requests over network    │
└────────────────┬───────────────────┘
                 │
                 │  📡 Network (TCP Port 9000)
                 │  [Requests & Responses travel here]
                 │
┌────────────────▼───────────────────┐
│     SERVER (Remote Computer)       │
│   - Console Application            │
│   - Receives client requests       │
│   - Reads/writes actual files      │
│   - Sends results back to client   │
└────────────────┬───────────────────┘
                 │
┌────────────────▼───────────────────┐
│     ServerStorage (Folder)         │
│   ├── Documents/                   │
│   ├── Images/                      │
│   └── welcome.txt                  │
└────────────────────────────────────┘
```

**The communication channel is .NET Remoting over TCP.** Think of it like a phone line — client talks, server listens, server responds.

---

## 🏗 Project Structure (4 Sub-Projects)

```
RemoteFileManagementSystem/
│
├── RemoteFileManagement.Shared/     ← "The Contract" (Rules both sides follow)
│   ├── IRemoteFileManager.cs        ← List of all operations available
│   ├── FileItem.cs                  ← What a file/folder looks like (name, size, date)
│   ├── FileOperationResult.cs       ← Standard reply format (success/failure + message)
│   └── ServerNotification.cs       ← Format of live push notifications
│
├── RemoteFileManagement.Server/     ← "The Worker" (Runs on the Server PC)
│   ├── ServerProgram.cs             ← Starts up, opens network port 9000
│   ├── RemoteFileManager.cs         ← Does the actual file work (read, write, delete...)
│   └── ServerStorage/               ← The sandboxed folder clients can access
│
├── RemoteFileManagement.Client/     ← "The Face" (Runs on Your PC)
│   ├── MainForm.cs                  ← Main GUI window with file browser
│   ├── FileEditorForm.cs            ← Text file editor/viewer
│   └── FolderSelectForm.cs          ← Folder picker for Copy/Move
│
└── RemoteFileManagement.Tests/      ← "The Checker" (Automated Tests)
    └── IntegrationTests.cs          ← 19 tests that verify everything works
```

### Why 4 projects?
- **Shared** — Both client and server need to agree on the same "language." This project holds that common language.
- **Server** — Lives on the server machine. Handles real file operations.
- **Client** — Lives on your machine. Gives you a visual interface.
- **Tests** — Automatically verifies every feature works correctly.

---

## ⚙️ Technologies Used

| What | How |
|---|---|
| **Language** | C# |
| **Framework** | .NET Framework 4.8 |
| **Communication** | .NET Remoting (Microsoft's distributed object technology) |
| **Network Protocol** | TCP (like a direct wired phone call, reliable and ordered) |
| **Data Format** | Binary Serialization (data sent as compact binary bytes, not text) |
| **Client UI** | Windows Forms (WinForms) — standard Windows GUI |
| **Server** | Console Application (command-line window with live logs) |

> ⚠ **Note:** This project intentionally uses **legacy .NET Remoting** as required by the course. It does NOT use REST API, Web API, ASP.NET, gRPC, or any modern web framework.

---

## 🔧 How .NET Remoting Works — Step by Step

This is the core technical concept of the project.

### 1. Shared Interface (`IRemoteFileManager`)
Think of this as a **menu in a restaurant**. The menu lists all available dishes (operations), but the kitchen (server) is the one that actually makes them.

Both client and server share this menu so they speak the same language.

```csharp
// Client "orders" from this menu. Server "cooks" these methods.
public interface IRemoteFileManager
{
    bool Ping();
    List<FileItem> GetFiles(string path);
    string ReadFile(string path);
    FileOperationResult CreateFile(string path, string content);
    byte[] DownloadFile(string path);
    // ... and many more
}
```

### 2. Remote Object (`MarshalByRefObject`)
The server's `RemoteFileManager` class extends `MarshalByRefObject`. This is the magic keyword that tells .NET:

> "This object lives on the server. When someone calls a method on it from another machine, **don't copy it** — instead, send the call over the network to the real object."

```csharp
// This class lives on the server. Clients call it remotely.
public class RemoteFileManager : MarshalByRefObject, IRemoteFileManager
{
    public string ReadFile(string path) 
    {
        return File.ReadAllText(path); // Real file read happens HERE, on server
    }
}
```

### 3. TCP Channel (The Communication Highway)
The server opens up **TCP Port 9000** and waits for connections. When a client connects, they can call server methods as if they were local function calls.

```xml
<!-- Server configuration: Open port 9000, accept binary data -->
<channel ref="tcp" port="9000">
  <serverProviders>
    <formatter ref="binary" typeFilterLevel="Full" />
  </serverProviders>
</channel>
```

### 4. Transparent Proxy (Client Magic)
When the client calls `Activator.GetObject(...)`, it gets back what looks like a normal C# object. But it's actually a **proxy** — a stand-in. When you call any method on it, the .NET framework secretly:
1. Packs up your method call and parameters into bytes
2. Sends them over TCP to the server
3. Server executes the real method
4. Server sends back the result in bytes
5. Client receives and unpacks the result

```csharp
// Client gets a "fake" object — actually a network proxy
IRemoteFileManager proxy = (IRemoteFileManager)
    Activator.GetObject(typeof(IRemoteFileManager), "tcp://localhost:9000/RemoteFileManager");

// This LOOKS like a local call but it travels over the network!
List<FileItem> files = proxy.GetFiles("Documents");
```

### 5. Infinite Lease Lifetime
By default, .NET Remoting disconnects remote objects after 5 minutes of inactivity. We override this to prevent disconnection errors during demonstrations:

```csharp
public override object InitializeLifetimeService()
{
    return null; // null = never expire, stay connected forever
}
```

---

## 🔔 Real-Time Push Notifications (Server → Client)

This is a key feature that demonstrates **bidirectional communication**.

### How it works:
1. The **server** runs a `FileSystemWatcher` — a built-in .NET class that watches the `ServerStorage` folder for any changes (new files, deleted files, renames).
2. Whenever anything changes, the server puts a notification into a queue.
3. The **client** has a timer that polls (asks) the server every **1 second**: *"Do you have any new notifications for me?"*
4. If yes, the server sends them. The client then:
   - **Automatically refreshes** the file list (no need to manually click Refresh)
   - Shows a `🔔 Live Sync` alert in the bottom status bar
   - **For admin broadcast messages**: Shows a popup dialog and saves to a log file

### What triggers notifications:
| Event | What client sees |
|---|---|
| File created on server | File list auto-refreshes, status shows `🔔 Live Sync: file.txt created` |
| File deleted on server | File list auto-refreshes immediately |
| File renamed on server | File list auto-refreshes immediately |
| Admin broadcasts message | **Popup dialog** appears + saved to `client_broadcasts_log.txt` |

### Server Admin Commands (in Server console window):
| Key | Action |
|---|---|
| `M` | Type and send a broadcast message to ALL connected clients |
| `S` | Display server storage statistics |
| `O` | Open the ServerStorage folder in Windows Explorer |
| `C` | Clear the console log |
| `Q` | Shut down the server |

---

## 📋 Full Feature List

### File Operations
| Feature | Description |
|---|---|
| 📄 Create File | Create a new empty or pre-filled text file on server |
| 📖 Read File | View a text file stored on the server |
| ✏ Edit / Update File | Modify text file content and save back to server |
| ➕ Append to File | Add text to the end without overwriting |
| 🏷 Rename File | Rename a file remotely |
| 📋 Copy File | Copy a file to another server folder |
| ✂ Move File | Move a file from one server folder to another |
| 🗑 Delete File | Permanently delete a file from the server |

### Folder Operations
| Feature | Description |
|---|---|
| 📁 Create Folder | Create a new directory on the server |
| 🗑 Delete Folder | Delete an entire folder and its contents |
| 🏷 Rename Folder | Rename a directory remotely |
| 📂 Browse Folders | Navigate the server folder tree in the left panel |

### Transfer Operations
| Feature | Description |
|---|---|
| ⬆ Upload | Transfer **any file** (PDF, image, zip, etc.) from your PC to the server |
| ⬇ Download | Transfer any file from server to your PC |

### Utility Features
| Feature | Description |
|---|---|
| 🔍 Search | Recursive search across entire server storage (supports wildcards like `*.txt`) |
| ℹ Properties | View full file metadata (size, dates, path) |
| 📊 Server Stats | View total files count and storage size used on server |
| 🔔 Live Sync | Auto-refresh when server files change (no manual refresh needed) |
| 📢 Broadcast Log | All server messages logged to `client_broadcasts_log.txt`, viewable in Notepad |

---

## 🔐 Security — Path Traversal Protection

A real-world concern in distributed systems is a **path traversal attack** — where a malicious client tries to access files outside their allowed area by sending paths like `../../Windows/System32/`.

This project defends against it:
1. All client-supplied paths are **sanitized** (cleaned of `..` tricks).
2. The resolved physical path is checked to ensure it starts with the `ServerStorage` directory path.
3. Any violation immediately throws an `UnauthorizedAccessException`, logs a red `[SECURITY]` warning on the server, and **rejects the request**.

```
Client sends: "../../Windows/win.ini"
Server detects: resolved path is OUTSIDE ServerStorage
Server responds: UnauthorizedAccessException — ACCESS DENIED
```

---

## ✅ Automated Testing — 19 Test Cases

The project includes a full automated integration test suite (`run-tests.bat`) that:
1. Starts a real server in the background
2. Connects a test client via .NET Remoting
3. Runs all 19 tests in sequence
4. Prints PASSED/FAILED for each
5. Stops the server

| # | Test Case |
|---|---|
| TC-01 | Ping (connection verification) |
| TC-02 | GetStorageStats (server metrics) |
| TC-03 | GetDirectories & GetFiles |
| TC-04 | ReadFile |
| TC-05 | CreateFile |
| TC-06 | AppendToFile |
| TC-07 | UpdateFile + verified read-back |
| TC-08 | RenameFile |
| TC-09 | CopyFile |
| TC-10 | MoveFile |
| TC-11 | CreateDirectory |
| TC-12 | RenameDirectory |
| TC-13 | UploadFile (binary bytes) |
| TC-14 | DownloadFile (binary bytes) |
| TC-15 | SearchFiles (wildcard) |
| TC-16 | GetFileInfo (metadata) |
| TC-17 | **Security: Path Traversal Attack** (correctly blocked) |
| TC-18 | **Error Handling: File Not Found** |
| TC-19 | DeleteFile + DeleteDirectory |

---

## 🚀 How to Run the Project

### Step 1 — Build
Double-click `build.bat`

### Step 2 — Start Server
Double-click `run-server.bat`
> A console window opens showing server logs

### Step 3 — Start Client
Double-click `run-client.bat`
> A Windows GUI opens. Click **⚡ Connect**.

### Step 4 — (Optional) Run Tests
Double-click `run-tests.bat`
> All 19 tests should show **PASSED**

---

## 🛠 System Requirements

- **OS**: Windows 10 / 11
- **Runtime**: .NET Framework 4.8 (already installed on Windows 10 1903+ and Windows 11)
- **To Build**: Visual Studio 2019/2022/2026 Community, or standalone MSBuild

---

## 🧪 Troubleshooting

| Problem | Solution |
|---|---|
| Client says "Unable to connect" | Make sure `run-server.bat` is running first |
| Build fails with "file locked" | Close the running Server/Client apps, then rebuild |
| Port 9000 blocked | Allow it through Windows Firewall when prompted |
| "Access outside ServerStorage" | Do not use `..` in file paths; stay within the storage root |

---

## 📌 Key .NET Concepts Used

| .NET Feature | What it Does in This Project |
|---|---|
| `MarshalByRefObject` | Makes the server object callable from across the network |
| `IRemoteFileManager` interface | Defines the common "contract" both sides agree on |
| `TcpChannel` | The network highway data travels through |
| `BinaryFormatter` | Packs C# objects into bytes for network transmission |
| `Activator.GetObject` | Client creates a remote reference (proxy) to the server object |
| `FileSystemWatcher` | Server watches for file changes to push notifications |
| `[Serializable]` attribute | Marks data classes that can be sent across the network |
| `InitializeLifetimeService` → `null` | Keeps the server object alive indefinitely (no timeout) |
| `WellKnownObjectMode.Singleton` | Only ONE server object instance serves ALL clients |
| `System.Windows.Forms.Timer` | Client polls server every 1 second for new notifications |

---

*Built with C# and .NET Framework 4.8 — College Project Demonstration*
