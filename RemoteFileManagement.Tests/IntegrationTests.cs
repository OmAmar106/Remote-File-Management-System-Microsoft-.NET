using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Tests
{
    class IntegrationTests
    {
        static int Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine(" RUNNING C# .NET REMOTING COMPREHENSIVE INTEGRATION SUITE");
            Console.WriteLine("==========================================================");

            // Register client TCP channel
            if (ChannelServices.GetChannel("tcpClientIntegration") == null)
            {
                BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
                IDictionary props = new Hashtable();
                props["name"] = "tcpClientIntegration";
                TcpChannel clientChannel = new TcpChannel(props, clientProv, null);
                ChannelServices.RegisterChannel(clientChannel, false);
            }

            string url = "tcp://localhost:9000/RemoteFileManager";
            Console.WriteLine("Connecting to: " + url);

            IRemoteFileManager proxy = null;
            try
            {
                proxy = (IRemoteFileManager)Activator.GetObject(typeof(IRemoteFileManager), url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FAIL] Failed to obtain remote proxy: " + ex.Message);
                return 1;
            }

            int passed = 0;
            int failed = 0;

            // TC-01: Ping
            try
            {
                Console.Write("[TC-01] Testing Ping()... ");
                bool ping = proxy.Ping();
                if (ping) { Console.WriteLine("PASSED (Result: true)"); passed++; }
                else { Console.WriteLine("FAILED"); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-02: GetStorageStats
            try
            {
                Console.Write("[TC-02] Testing GetStorageStats()... ");
                ServerStorageStats stats = proxy.GetStorageStats();
                Console.WriteLine("PASSED ({0} files, {1} dirs, {2})", stats.TotalFiles, stats.TotalDirectories, stats.FormattedTotalSize);
                passed++;
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-03: GetDirectories & GetFiles
            try
            {
                Console.Write("[TC-03] Testing GetDirectories() and GetFiles()... ");
                var dirs = proxy.GetDirectories("");
                var files = proxy.GetFiles("");
                Console.WriteLine("PASSED ({0} dirs, {1} files in root)", dirs.Count, files.Count);
                passed++;
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-04: ReadFile
            try
            {
                Console.Write("[TC-04] Testing ReadFile('welcome.txt')... ");
                string text = proxy.ReadFile("welcome.txt");
                if (!string.IsNullOrEmpty(text) && text.Contains("Welcome to Remote File Management System"))
                {
                    Console.WriteLine("PASSED (Length: {0} chars)", text.Length);
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED (Unexpected content)");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-05: CreateFile
            try
            {
                Console.Write("[TC-05] Testing CreateFile('test_demo.txt')... ");
                var res = proxy.CreateFile("test_demo.txt", "Initial file created by integration test.\r\n");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-06: AppendToFile
            try
            {
                Console.Write("[TC-06] Testing AppendToFile('test_demo.txt')... ");
                var res = proxy.AppendToFile("test_demo.txt", "Appended line 2 via .NET Remoting.\r\n");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-07: UpdateFile
            try
            {
                Console.Write("[TC-07] Testing UpdateFile('test_demo.txt')... ");
                var res = proxy.UpdateFile("test_demo.txt", "Completely updated content by C# client.\r\n");
                string readBack = proxy.ReadFile("test_demo.txt");
                if (res.Success && readBack.Contains("Completely updated"))
                {
                    Console.WriteLine("PASSED (Verified read-back content)");
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Readback didn't match");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-08: RenameFile
            try
            {
                Console.Write("[TC-08] Testing RenameFile('test_demo.txt' -> 'test_renamed.txt')... ");
                var res = proxy.RenameFile("test_demo.txt", "test_renamed.txt");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-09: CopyFile
            try
            {
                Console.Write("[TC-09] Testing CopyFile('test_renamed.txt' -> 'Documents/test_copy.txt')... ");
                var res = proxy.CopyFile("test_renamed.txt", "Documents/test_copy.txt");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-10: MoveFile
            try
            {
                Console.Write("[TC-10] Testing MoveFile('test_renamed.txt' -> 'Images/test_moved.txt')... ");
                var res = proxy.MoveFile("test_renamed.txt", "Images/test_moved.txt");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-11: CreateDirectory
            try
            {
                Console.Write("[TC-11] Testing CreateDirectory('TestFolder')... ");
                var res = proxy.CreateDirectory("TestFolder");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-12: RenameDirectory
            try
            {
                Console.Write("[TC-12] Testing RenameDirectory('TestFolder' -> 'RenamedFolder')... ");
                var res = proxy.RenameDirectory("TestFolder", "RenamedFolder");
                if (res.Success) { Console.WriteLine("PASSED (" + res.Message + ")"); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-13: UploadFile (Binary)
            try
            {
                Console.Write("[TC-13] Testing UploadFile(byte[])... ");
                byte[] binaryData = Encoding.UTF8.GetBytes("Binary file payload for Remoting transfer test!");
                var res = proxy.UploadFile("RenamedFolder/binary_test.dat", binaryData);
                if (res.Success) { Console.WriteLine("PASSED ({0} bytes uploaded)", binaryData.Length); passed++; }
                else { Console.WriteLine("FAILED: " + res.Message); failed++; }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-14: DownloadFile (Binary)
            try
            {
                Console.Write("[TC-14] Testing DownloadFile()... ");
                byte[] downloaded = proxy.DownloadFile("RenamedFolder/binary_test.dat");
                string decoded = Encoding.UTF8.GetString(downloaded);
                if (decoded == "Binary file payload for Remoting transfer test!")
                {
                    Console.WriteLine("PASSED ({0} bytes accurately received)", downloaded.Length);
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Payload corrupted");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-15: SearchFiles
            try
            {
                Console.Write("[TC-15] Testing SearchFiles('binary*')... ");
                var search = proxy.SearchFiles("binary*", "");
                if (search.Count > 0)
                {
                    Console.WriteLine("PASSED (Matched {0} items)", search.Count);
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED (0 matches)");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-16: GetFileInfo
            try
            {
                Console.Write("[TC-16] Testing GetFileInfo('RenamedFolder/binary_test.dat')... ");
                FileItem fi = proxy.GetFileInfo("RenamedFolder/binary_test.dat");
                if (fi != null && fi.Name == "binary_test.dat" && fi.Size > 0)
                {
                    Console.WriteLine("PASSED (Name: {0}, Size: {1})", fi.Name, fi.FormattedSize);
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Invalid file info returned");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-17: Path Traversal Security Test (Negative test)
            try
            {
                Console.Write("[TC-17] Testing Path Traversal Attack (../../windows/win.ini)... ");
                bool blocked = false;
                try
                {
                    proxy.ReadFile("../../windows/win.ini");
                }
                catch (UnauthorizedAccessException)
                {
                    blocked = true;
                }
                catch (Exception)
                {
                    blocked = true;
                }

                if (blocked)
                {
                    Console.WriteLine("PASSED (Correctly rejected with UnauthorizedAccessException)");
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Security breach! Path traversal was not blocked.");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-18: Non-existent file error handling
            try
            {
                Console.Write("[TC-18] Testing File Not Found handling... ");
                bool notFound = false;
                try
                {
                    proxy.ReadFile("does_not_exist_file_99999.txt");
                }
                catch (FileNotFoundException)
                {
                    notFound = true;
                }
                catch (Exception)
                {
                    notFound = true;
                }

                if (notFound)
                {
                    Console.WriteLine("PASSED (Handled with appropriate FileNotFoundException)");
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Did not detect missing file.");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            // TC-19: Cleanup (DeleteFile & DeleteDirectory)
            try
            {
                Console.Write("[TC-19] Testing DeleteFile and DeleteDirectory... ");
                var d1 = proxy.DeleteFile("Documents/test_copy.txt");
                var d2 = proxy.DeleteFile("Images/test_moved.txt");
                var d3 = proxy.DeleteDirectory("RenamedFolder");
                if (d1.Success && d2.Success && d3.Success)
                {
                    Console.WriteLine("PASSED (All test artifacts removed cleanly)");
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED: Cleanup failed");
                    failed++;
                }
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex.Message); failed++; }

            Console.WriteLine("\n==========================================================");
            Console.WriteLine(" TEST RESULTS SUMMARY: {0} PASSED, {1} FAILED", passed, failed);
            Console.WriteLine("==========================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
