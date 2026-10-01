using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace RemoteFileManagement.Server
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            Console.Title = "Remote File Management Server (.NET Remoting)";

            // Determine port from argument or configuration or default
            int port = 9000;
            if (args.Length > 0 && int.TryParse(args[0], out int argPort))
            {
                port = argPort;
            }
            else
            {
                string configPort = ConfigurationManager.AppSettings["ServerPort"];
                if (!string.IsNullOrEmpty(configPort) && int.TryParse(configPort, out int parsedPort))
                {
                    port = parsedPort;
                }
            }

            // Determine storage directory
            string storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerStorage");
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            PrintBanner(port, storagePath);

            TcpChannel channel = null;

            try
            {
                // Try loading from Server.config or App.config first if available
                string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Server.config");
                bool configuredFromXml = false;

                if (File.Exists(configFile))
                {
                    try
                    {
                        RemotingConfiguration.Configure(configFile, false);
                        configuredFromXml = true;
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("[CONFIG] Remoting configuration successfully loaded from Server.config.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[CONFIG WARN] Could not apply Server.config ({0}). Falling back to programmatic registration.", ex.Message);
                        Console.ResetColor();
                    }
                }

                // If not configured from XML, configure programmatically
                if (!configuredFromXml)
                {
                    // Configure binary formatter with Full type filter level to permit rich remote object serialization
                    BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider
                    {
                        TypeFilterLevel = TypeFilterLevel.Full
                    };

                    BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();

                    IDictionary channelProperties = new Hashtable();
                    channelProperties["port"] = port;
                    channelProperties["name"] = "tcp" + port;

                    channel = new TcpChannel(channelProperties, clientProvider, serverProvider);
                    ChannelServices.RegisterChannel(channel, false);

                    // Register RemoteFileManager as a WellKnown Singleton service
                    RemotingConfiguration.RegisterWellKnownServiceType(
                        typeof(RemoteFileManager),
                        "RemoteFileManager",
                        WellKnownObjectMode.Singleton
                    );

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[REMOTING] TCP Channel registered on port {0}.", port);
                    Console.WriteLine("[REMOTING] Service registered: tcp://localhost:{0}/RemoteFileManager (Singleton)", port);
                    Console.ResetColor();
                }

                // Initialize the instance to ensure ServerStorage and sample files are prepared
                RemoteFileManager initialInstance = new RemoteFileManager(storagePath);
                var stats = initialInstance.GetStorageStats();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n[STORAGE READY] {0} files, {1} folders initialized.", stats.TotalFiles, stats.TotalDirectories);
                Console.ResetColor();

                Console.WriteLine("\n==============================================================");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" SERVER STARTED SUCCESSFULLY. WAITING FOR CLIENT REQUESTS... ");
                Console.ResetColor();
                Console.WriteLine("==============================================================");
                Console.WriteLine(" Commands: [M] Broadcast Message to Clients  |  [S] Show Stats  |  [O] Open Storage  |  [C] Clear  |  [Q] Exit");
                Console.WriteLine("------------------------------------------------------------------------------------------------------\n");

                bool isInteractive = true;
                try
                {
                    isInteractive = !Console.IsInputRedirected && Environment.UserInteractive;
                }
                catch
                {
                    isInteractive = false;
                }

                // Handle Ctrl+C cleanly
                bool running = true;
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    running = false;
                    Console.WriteLine("\n[SHUTDOWN] Terminating server cleanly...");
                };

                // Command loop
                while (running)
                {
                    if (isInteractive)
                    {
                        try
                        {
                            if (Console.KeyAvailable)
                            {
                                ConsoleKeyInfo key = Console.ReadKey(true);
                                if (key.Key == ConsoleKey.Q)
                                {
                                    Console.WriteLine("\n[SHUTDOWN] Stopping server...");
                                    break;
                                }
                                else if (key.Key == ConsoleKey.M)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write("\n[SERVER BROADCAST] Enter message for connected clients: ");
                                    Console.ResetColor();
                                    string broadcastText = Console.ReadLine();
                                    if (!string.IsNullOrWhiteSpace(broadcastText))
                                    {
                                        RemoteFileManager.PushNotification("SERVER_MESSAGE", broadcastText);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("[SENT] Pushed broadcast to all clients: \"{0}\"\n", broadcastText);
                                        Console.ResetColor();
                                    }
                                }
                                else if (key.Key == ConsoleKey.S)
                                {
                                    var s = initialInstance.GetStorageStats();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("\n--- SERVER STORAGE STATS ---");
                                    Console.WriteLine("  Root: {0}", s.StorageRootPath);
                                    Console.WriteLine("  Files: {0}", s.TotalFiles);
                                    Console.WriteLine("  Directories: {0}", s.TotalDirectories);
                                    Console.WriteLine("  Total Size: {0}", s.FormattedTotalSize);
                                    Console.WriteLine("  Server Time: {0:yyyy-MM-dd HH:mm:ss}", s.ServerTime);
                                    Console.WriteLine("----------------------------\n");
                                    Console.ResetColor();
                                }
                                else if (key.Key == ConsoleKey.O)
                                {
                                    try
                                    {
                                        Process.Start("explorer.exe", storagePath);
                                        Console.WriteLine("[LOCAL] Opened ServerStorage directory in Windows Explorer.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("[LOCAL ERROR] Could not open folder: " + ex.Message);
                                    }
                                }
                                else if (key.Key == ConsoleKey.C)
                                {
                                    Console.Clear();
                                    PrintBanner(port, storagePath);
                                    Console.WriteLine(" Logs cleared. Waiting for client operations...\n");
                                }
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            isInteractive = false;
                        }
                    }

                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[FATAL ERROR] Server startup failure: " + ex.Message);
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                try { Console.ReadKey(); } catch { }
            }
            finally
            {
                if (channel != null)
                {
                    try
                    {
                        ChannelServices.UnregisterChannel(channel);
                    }
                    catch { }
                }
            }
        }

        private static void PrintBanner(int port, string storagePath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"==============================================================");
            Console.WriteLine(@"          REMOTE FILE MANAGEMENT SERVER SYSTEM               ");
            Console.WriteLine(@"               Using Microsoft .NET Remoting                 ");
            Console.WriteLine(@"==============================================================");
            Console.ResetColor();
            Console.WriteLine(" Architecture : Distributed Client-Server via .NET Remoting");
            Console.WriteLine(" Transport    : TCP Channel (Binary Serialization Formatter)");
            Console.WriteLine(" Endpoint     : tcp://localhost:{0}/RemoteFileManager", port);
            Console.WriteLine(" Storage Path : {0}", storagePath);
            Console.WriteLine(" Framework    : .NET Framework 4.8 (C#)");
            Console.WriteLine("==============================================================");
        }
    }
}
