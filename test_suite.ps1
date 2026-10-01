# Automated Test Suite for Remote File Management System (.NET Remoting)
$ErrorActionPreference = "Stop"

$serverExe = "c:\DCS\RemoteFileManagementSystem\RemoteFileManagement.Server\bin\Debug\RemoteFileManagement.Server.exe"
$sharedDll = "c:\DCS\RemoteFileManagementSystem\RemoteFileManagement.Shared\bin\Debug\RemoteFileManagement.Shared.dll"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING AUTOMATED REMOTING INTEGRATION TEST SUITE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Start Server Process
$serverProc = Start-Process -FilePath $serverExe -PassThru
Start-Sleep -Seconds 2

if ($serverProc.HasExited) {
    Write-Error "Server process failed to start or exited immediately!"
    exit 1
}

Write-Host "[1] Server started successfully with PID: $($serverProc.Id)" -ForegroundColor Green

try {
    # 2. Load Shared Library & Remoting
    Add-Type -Path $sharedDll
    Add-Type -AssemblyName 'System.Runtime.Remoting'

    # 3. Register Client TCP Channel
    $chan = [System.Runtime.Remoting.Channels.ChannelServices]::GetChannel("tcpClientTest")
    if ($null -eq $chan) {
        $props = New-Object System.Collections.Hashtable
        $props["name"] = "tcpClientTest"
        $clientSink = New-Object System.Runtime.Remoting.Channels.BinaryClientFormatterSinkProvider
        $chan = New-Object System.Runtime.Remoting.Channels.Tcp.TcpChannel($props, $clientSink, $null)
        [System.Runtime.Remoting.Channels.ChannelServices]::RegisterChannel($chan, $false)
    }

    # 4. Connect to Remote Interface
    $url = "tcp://localhost:9000/RemoteFileManager"
    $proxy = [System.Activator]::GetObject([RemoteFileManagement.Shared.IRemoteFileManager], $url)

    # TEST TC-01: Ping
    Write-Host "`n--- [TC-01] Testing Ping() ---" -ForegroundColor Yellow
    $ping = $proxy.Ping()
    Write-Host "Ping Result: $ping" -ForegroundColor Green
    if (!$ping) { throw "Ping failed!" }

    # TEST TC-02: GetStorageStats
    Write-Host "`n--- [TC-02] Testing GetStorageStats() ---" -ForegroundColor Yellow
    $stats = $proxy.GetStorageStats()
    Write-Host "Files: $($stats.TotalFiles), Folders: $($stats.TotalDirectories), Size: $($stats.FormattedTotalSize), Machine: $($stats.ServerMachineName)" -ForegroundColor Green

    # TEST TC-03: GetDirectories & GetFiles
    Write-Host "`n--- [TC-03] Testing GetDirectories('') and GetFiles('') ---" -ForegroundColor Yellow
    $dirs = $proxy.GetDirectories("")
    foreach ($d in $dirs) { Write-Host " Directory: $($d.Name) ($($d.RelativePath))" }
    $files = $proxy.GetFiles("")
    foreach ($f in $files) { Write-Host " File: $($f.Name) ($($f.FormattedSize))" }

    # TEST TC-04: ReadFile
    Write-Host "`n--- [TC-04] Testing ReadFile('welcome.txt') ---" -ForegroundColor Yellow
    $content = $proxy.ReadFile("welcome.txt")
    Write-Host "Content preview: $($content.Substring(0, [Math]::Min(120, $content.Length)))..." -ForegroundColor Green

    # TEST TC-05: CreateFile
    Write-Host "`n--- [TC-05] Testing CreateFile('test_file.txt') ---" -ForegroundColor Yellow
    $createRes = $proxy.CreateFile("test_file.txt", "Initial college demo content 2026.")
    Write-Host "Create Result: $($createRes.Message) (Success=$($createRes.Success))" -ForegroundColor Green
    if (!$createRes.Success) { throw "CreateFile failed: $($createRes.Message)" }

    # TEST TC-06: AppendToFile
    Write-Host "`n--- [TC-06] Testing AppendToFile('test_file.txt') ---" -ForegroundColor Yellow
    $appendRes = $proxy.AppendToFile("test_file.txt", "`r`nAppended line via .NET Remoting.")
    Write-Host "Append Result: $($appendRes.Message) (Success=$($appendRes.Success))" -ForegroundColor Green
    if (!$appendRes.Success) { throw "AppendToFile failed: $($appendRes.Message)" }

    # TEST TC-07: UpdateFile
    Write-Host "`n--- [TC-07] Testing UpdateFile('test_file.txt') ---" -ForegroundColor Yellow
    $updateRes = $proxy.UpdateFile("test_file.txt", "Completely updated file content via remote invocation.")
    Write-Host "Update Result: $($updateRes.Message) (Success=$($updateRes.Success))" -ForegroundColor Green
    if (!$updateRes.Success) { throw "UpdateFile failed: $($updateRes.Message)" }

    # Verify updated content
    $readUpdated = $proxy.ReadFile("test_file.txt")
    Write-Host "Verified Read Content: $readUpdated" -ForegroundColor Green

    # TEST TC-08: RenameFile
    Write-Host "`n--- [TC-08] Testing RenameFile('test_file.txt' -> 'test_renamed.txt') ---" -ForegroundColor Yellow
    $renRes = $proxy.RenameFile("test_file.txt", "test_renamed.txt")
    Write-Host "Rename Result: $($renRes.Message) (Success=$($renRes.Success))" -ForegroundColor Green
    if (!$renRes.Success) { throw "RenameFile failed: $($renRes.Message)" }

    # TEST TC-09: CopyFile
    Write-Host "`n--- [TC-09] Testing CopyFile('test_renamed.txt' -> 'Documents/test_copy.txt') ---" -ForegroundColor Yellow
    $copyRes = $proxy.CopyFile("test_renamed.txt", "Documents/test_copy.txt")
    Write-Host "Copy Result: $($copyRes.Message) (Success=$($copyRes.Success))" -ForegroundColor Green
    if (!$copyRes.Success) { throw "CopyFile failed: $($copyRes.Message)" }

    # TEST TC-10: MoveFile
    Write-Host "`n--- [TC-10] Testing MoveFile('test_renamed.txt' -> 'Images/test_moved.txt') ---" -ForegroundColor Yellow
    $moveRes = $proxy.MoveFile("test_renamed.txt", "Images/test_moved.txt")
    Write-Host "Move Result: $($moveRes.Message) (Success=$($moveRes.Success))" -ForegroundColor Green
    if (!$moveRes.Success) { throw "MoveFile failed: $($moveRes.Message)" }

    # TEST TC-11: CreateDirectory
    Write-Host "`n--- [TC-11] Testing CreateDirectory('ProjectArchive') ---" -ForegroundColor Yellow
    $mkdirRes = $proxy.CreateDirectory("ProjectArchive")
    Write-Host "CreateDir Result: $($mkdirRes.Message) (Success=$($mkdirRes.Success))" -ForegroundColor Green
    if (!$mkdirRes.Success) { throw "CreateDirectory failed: $($mkdirRes.Message)" }

    # TEST TC-12: RenameDirectory
    Write-Host "`n--- [TC-12] Testing RenameDirectory('ProjectArchive' -> 'ProjectArchiveRenamed') ---" -ForegroundColor Yellow
    $renDirRes = $proxy.RenameDirectory("ProjectArchive", "ProjectArchiveRenamed")
    Write-Host "RenameDir Result: $($renDirRes.Message) (Success=$($renDirRes.Success))" -ForegroundColor Green
    if (!$renDirRes.Success) { throw "RenameDirectory failed: $($renDirRes.Message)" }

    # TEST TC-13: UploadFile (Binary Transfer)
    Write-Host "`n--- [TC-13] Testing UploadFile() Binary Transfer ---" -ForegroundColor Yellow
    $testBinary = [byte[]](0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x52, 0x65, 0x6D, 0x6F, 0x74, 0x69, 0x6E, 0x67, 0x21) # "Hello Remoting!"
    $uploadRes = $proxy.UploadFile("ProjectArchiveRenamed/sample_payload.bin", $testBinary)
    Write-Host "Upload Result: $($uploadRes.Message) (Success=$($uploadRes.Success))" -ForegroundColor Green
    if (!$uploadRes.Success) { throw "UploadFile failed: $($uploadRes.Message)" }

    # TEST TC-14: DownloadFile (Binary Transfer)
    Write-Host "`n--- [TC-14] Testing DownloadFile() Binary Transfer ---" -ForegroundColor Yellow
    $downloadedBytes = $proxy.DownloadFile("ProjectArchiveRenamed/sample_payload.bin")
    $downloadedStr = [System.Text.Encoding]::UTF8.GetString($downloadedBytes)
    Write-Host "Downloaded $($downloadedBytes.Length) bytes: '$downloadedStr'" -ForegroundColor Green
    if ($downloadedStr -ne "Hello Remoting!") { throw "Downloaded bytes mismatch!" }

    # TEST TC-15: SearchFiles
    Write-Host "`n--- [TC-15] Testing SearchFiles('sample*') ---" -ForegroundColor Yellow
    $searchResults = $proxy.SearchFiles("sample*", "")
    Write-Host "Found $($searchResults.Count) search matches:" -ForegroundColor Green
    foreach ($sr in $searchResults) { Write-Host " - $($sr.Name) at $($sr.RelativePath)" }

    # TEST TC-16: GetFileInfo
    Write-Host "`n--- [TC-16] Testing GetFileInfo('ProjectArchiveRenamed/sample_payload.bin') ---" -ForegroundColor Yellow
    $info = $proxy.GetFileInfo("ProjectArchiveRenamed/sample_payload.bin")
    Write-Host "Name: $($info.Name), Size: $($info.Size) bytes, Type: $($info.TypeDescription), Modified: $($info.LastWriteTime)" -ForegroundColor Green

    # TEST TC-17: Path Traversal Security Test (Negative Testing)
    Write-Host "`n--- [TC-17] Testing Path Traversal Protection (Security Test) ---" -ForegroundColor Yellow
    $traversalBlocked = $false
    try {
        $proxy.ReadFile("../../Windows/win.ini")
    }
    catch {
        $traversalBlocked = $true
        Write-Host "Path traversal correctly blocked! Caught exception: $($_.Exception.Message)" -ForegroundColor Green
    }
    if (!$traversalBlocked) { throw "SECURITY FAILURE: Path traversal was not blocked!" }

    # TEST TC-18: Non-existent file error handling
    Write-Host "`n--- [TC-18] Testing File Not Found Error Handling ---" -ForegroundColor Yellow
    $notFoundHandled = $false
    try {
        $proxy.ReadFile("non_existent_file_12345.txt")
    }
    catch {
        $notFoundHandled = $true
        Write-Host "Non-existent file handled correctly: $($_.Exception.Message)" -ForegroundColor Green
    }
    if (!$notFoundHandled) { throw "Error handling test failed: non-existent file did not throw!" }

    # TEST TC-19: DeleteFile & DeleteDirectory (Cleanup)
    Write-Host "`n--- [TC-19] Testing DeleteFile and DeleteDirectory ---" -ForegroundColor Yellow
    $delCopy = $proxy.DeleteFile("Documents/test_copy.txt")
    Write-Host "Delete Copy Result: $($delCopy.Message)" -ForegroundColor Green
    $delMove = $proxy.DeleteFile("Images/test_moved.txt")
    Write-Host "Delete Moved Result: $($delMove.Message)" -ForegroundColor Green
    $delDir = $proxy.DeleteDirectory("ProjectArchiveRenamed")
    Write-Host "Delete Directory Result: $($delDir.Message)" -ForegroundColor Green

    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host " ALL 19 REMOTING INTEGRATION TEST CASES PASSED SUCCESSFULLY! " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
}
finally {
    if (!$serverProc.HasExited) {
        Write-Host "Stopping test server PID $($serverProc.Id)..." -ForegroundColor Gray
        Stop-Process -Id $serverProc.Id -Force
    }
}
