using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Client
{
    /// <summary>
    /// Main client window providing a graphical file manager interface.
    /// Communicates with the remote server exclusively via .NET Remoting (IRemoteFileManager).
    /// </summary>
    public class MainForm : Form
    {
        #region Private Fields

        private IRemoteFileManager _remoteManager;
        private bool _isConnected = false;
        private string _currentPath = ""; // "" represents ServerStorage root
        private bool _isSearchMode = false;

        // UI Controls
        private TextBox txtHost;
        private TextBox txtPort;
        private Button btnConnect;
        private Button btnDisconnect;
        private Label lblConnectionStatus;
        private Label lblServerStats;

        private Button btnNavUp;
        private Button btnNavHome;
        private Button btnRefresh;
        private TextBox txtCurrentPath;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnClearSearch;

        private ToolStripButton btnNewFile;
        private ToolStripButton btnNewFolder;
        private ToolStripButton btnUpload;
        private ToolStripButton btnDownload;
        private ToolStripButton btnRead;
        private ToolStripButton btnEdit;
        private ToolStripButton btnRename;
        private ToolStripButton btnCopy;
        private ToolStripButton btnMove;
        private ToolStripButton btnDelete;
        private ToolStripButton btnProperties;

        private TreeView treeFolders;
        private ListView listViewFiles;
        private ImageList imageListSmall;
        private ContextMenuStrip contextMenuFiles;

        private ToolStripStatusLabel statusLabelMessage;
        private ToolStripStatusLabel statusLabelLiveAlert;
        private ToolStripStatusLabel statusLabelCount;
        private ToolStripStatusLabel statusLabelEndpoint;

        private System.Windows.Forms.Timer _pollTimer;
        private long _lastReceivedEventId = 0;
        private bool _isRefreshingFromPush = false;
        private string _broadcastLogFilePath;

        #endregion

        public MainForm()
        {
            InitializeComponent();
            SetupIconImageList();
            
            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 1000;
            _pollTimer.Tick += PollTimer_Tick;

            _broadcastLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_broadcasts_log.txt");

            UpdateUiState();
        }

        #region UI Initialization

        private void InitializeComponent()
        {
            this.Text = "Remote File Management System (.NET Remoting)";
            this.Size = new Size(1100, 720);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 1. Top Main Menu
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem menuFile = new ToolStripMenuItem("&Connection");
            ToolStripMenuItem miConnect = new ToolStripMenuItem("&Connect to Server", null, (s, e) => BtnConnect_Click(s, e));
            ToolStripMenuItem miDisconnect = new ToolStripMenuItem("&Disconnect", null, (s, e) => BtnDisconnect_Click(s, e));
            ToolStripMenuItem miExit = new ToolStripMenuItem("E&xit", null, (s, e) => this.Close());
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { miConnect, miDisconnect, new ToolStripSeparator(), miExit });

            ToolStripMenuItem menuOps = new ToolStripMenuItem("&Operations");
            ToolStripMenuItem miNewFile = new ToolStripMenuItem("New &File...", null, (s, e) => ActionNewFile());
            ToolStripMenuItem miNewFolder = new ToolStripMenuItem("New &Folder...", null, (s, e) => ActionNewFolder());
            ToolStripMenuItem miUpload = new ToolStripMenuItem("&Upload File...", null, (s, e) => ActionUpload());
            ToolStripMenuItem miDownload = new ToolStripMenuItem("&Download File...", null, (s, e) => ActionDownload());
            ToolStripMenuItem miRead = new ToolStripMenuItem("&Read / View", null, (s, e) => ActionRead());
            ToolStripMenuItem miEdit = new ToolStripMenuItem("&Edit / Update", null, (s, e) => ActionEdit());
            ToolStripMenuItem miRename = new ToolStripMenuItem("Re&name...", null, (s, e) => ActionRename());
            ToolStripMenuItem miCopy = new ToolStripMenuItem("&Copy To...", null, (s, e) => ActionCopy());
            ToolStripMenuItem miMove = new ToolStripMenuItem("&Move To...", null, (s, e) => ActionMove());
            ToolStripMenuItem miDelete = new ToolStripMenuItem("&Delete", null, (s, e) => ActionDelete());
            ToolStripMenuItem miProps = new ToolStripMenuItem("&Properties", null, (s, e) => ActionProperties());
            ToolStripMenuItem miViewBroadcasts = new ToolStripMenuItem("📢 &View Broadcasts / Server Messages (Notepad)", null, (s, e) => ActionOpenBroadcastLog());
            menuOps.DropDownItems.AddRange(new ToolStripItem[] {
                miNewFile, miNewFolder, new ToolStripSeparator(),
                miUpload, miDownload, new ToolStripSeparator(),
                miRead, miEdit, miRename, miCopy, miMove, miDelete, new ToolStripSeparator(),
                miProps, new ToolStripSeparator(),
                miViewBroadcasts
            });

            ToolStripMenuItem menuHelp = new ToolStripMenuItem("&Help");
            ToolStripMenuItem miServerStats = new ToolStripMenuItem("&Server Storage Statistics", null, (s, e) => ShowServerStorageStats());
            ToolStripMenuItem miAbout = new ToolStripMenuItem("&About .NET Remoting Project", null, (s, e) => ShowAboutDialog());
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { miServerStats, new ToolStripSeparator(), miAbout });

            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuOps, menuHelp });

            // 2. Connection Bar Panel
            Panel pnlConnection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(243, 245, 248),
                Padding = new Padding(12, 8, 12, 8)
            };

            Label lblHost = new Label { Text = "Server Host:", Location = new Point(14, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtHost = new TextBox { Text = "localhost", Location = new Point(95, 13), Size = new Size(130, 25) };

            Label lblPort = new Label { Text = "Port:", Location = new Point(235, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtPort = new TextBox { Text = "9000", Location = new Point(275, 13), Size = new Size(60, 25) };

            btnConnect = new Button
            {
                Text = "⚡ Connect",
                Location = new Point(345, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = new Button
            {
                Text = "Disconnect",
                Location = new Point(452, 10),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(220, 224, 230),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.Click += BtnDisconnect_Click;

            lblConnectionStatus = new Label
            {
                Text = "● Disconnected",
                ForeColor = Color.Red,
                Location = new Point(555, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            lblServerStats = new Label
            {
                Text = "",
                ForeColor = Color.DarkSlateGray,
                Location = new Point(780, 16),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlConnection.Controls.AddRange(new Control[] {
                lblHost, txtHost, lblPort, txtPort, btnConnect, btnDisconnect, lblConnectionStatus, lblServerStats
            });

            // 3. Navigation Bar Panel
            Panel pnlNavigation = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 6)
            };

            btnNavHome = new Button { Text = "🏠 Root", Location = new Point(12, 7), Size = new Size(68, 28), FlatStyle = FlatStyle.Flat };
            btnNavHome.Click += (s, e) => NavigateTo("");

            btnNavUp = new Button { Text = "⬆ Up", Location = new Point(85, 7), Size = new Size(60, 28), FlatStyle = FlatStyle.Flat };
            btnNavUp.Click += (s, e) => NavigateUp();

            btnRefresh = new Button { Text = "🔄 Refresh", Location = new Point(150, 7), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat };
            btnRefresh.Click += (s, e) => RefreshCurrentDirectory();

            Label lblPath = new Label { Text = "Location:", Location = new Point(238, 12), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtCurrentPath = new TextBox { Text = "/", Location = new Point(300, 9), Size = new Size(340, 25), ReadOnly = true, BackColor = Color.White };

            Label lblSearch = new Label { Text = "Search:", Location = new Point(650, 12), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtSearch = new TextBox { Location = new Point(705, 9), Size = new Size(180, 25) };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ActionSearch(); };

            btnSearch = new Button { Text = "🔍 Find", Location = new Point(892, 7), Size = new Size(65, 28), FlatStyle = FlatStyle.Flat };
            btnSearch.Click += (s, e) => ActionSearch();

            btnClearSearch = new Button { Text = "✖ Clear", Location = new Point(962, 7), Size = new Size(70, 28), FlatStyle = FlatStyle.Flat, Visible = false };
            btnClearSearch.Click += (s, e) => ClearSearch();

            pnlNavigation.Controls.AddRange(new Control[] {
                btnNavHome, btnNavUp, btnRefresh, lblPath, txtCurrentPath, lblSearch, txtSearch, btnSearch, btnClearSearch
            });

            // 4. Action ToolStrip
            ToolStrip toolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(248, 249, 251),
                Padding = new Padding(8, 2, 8, 2),
                Font = new Font("Segoe UI", 9F)
            };

            btnNewFile = new ToolStripButton("📄 New File", null, (s, e) => ActionNewFile());
            btnNewFolder = new ToolStripButton("📁 New Folder", null, (s, e) => ActionNewFolder());
            btnUpload = new ToolStripButton("⬆ Upload File", null, (s, e) => ActionUpload());
            btnDownload = new ToolStripButton("⬇ Download File", null, (s, e) => ActionDownload());
            btnRead = new ToolStripButton("📖 Read", null, (s, e) => ActionRead());
            btnEdit = new ToolStripButton("✏ Edit", null, (s, e) => ActionEdit());
            btnRename = new ToolStripButton("🏷 Rename", null, (s, e) => ActionRename());
            btnCopy = new ToolStripButton("📋 Copy", null, (s, e) => ActionCopy());
            btnMove = new ToolStripButton("✂ Move", null, (s, e) => ActionMove());
            btnDelete = new ToolStripButton("🗑 Delete", null, (s, e) => ActionDelete());
            btnProperties = new ToolStripButton("ℹ Properties", null, (s, e) => ActionProperties());
            ToolStripButton btnViewLog = new ToolStripButton("📢 Server Broadcasts", null, (s, e) => ActionOpenBroadcastLog());

            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnNewFile, btnNewFolder, new ToolStripSeparator(),
                btnUpload, btnDownload, new ToolStripSeparator(),
                btnRead, btnEdit, new ToolStripSeparator(),
                btnRename, btnCopy, btnMove, btnDelete, new ToolStripSeparator(),
                btnProperties, new ToolStripSeparator(),
                btnViewLog
            });

            // 5. Main Split Container (Left: TreeView, Right: ListView)
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 260,
                SplitterWidth = 5
            };

            // Left Panel: Directory Tree
            Panel pnlTreeHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(235, 238, 242) };
            Label lblTree = new Label { Text = "Server Folders", Location = new Point(8, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlTreeHeader.Controls.Add(lblTree);

            treeFolders = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true
            };
            treeFolders.AfterSelect += TreeFolders_AfterSelect;
            treeFolders.BeforeExpand += TreeFolders_BeforeExpand;

            splitContainer.Panel1.Controls.Add(treeFolders);
            splitContainer.Panel1.Controls.Add(pnlTreeHeader);

            // Right Panel: Files List
            listViewFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.None
            };
            listViewFiles.Columns.Add("Name", 320);
            listViewFiles.Columns.Add("Type", 140);
            listViewFiles.Columns.Add("Size", 100, HorizontalAlignment.Right);
            listViewFiles.Columns.Add("Date Modified", 160);
            listViewFiles.DoubleClick += ListViewFiles_DoubleClick;
            listViewFiles.SelectedIndexChanged += (s, e) => UpdateSelectionUi();

            // Right Click Context Menu for ListView
            contextMenuFiles = new ContextMenuStrip();
            contextMenuFiles.Items.Add("📖 Open / Read", null, (s, e) => ActionRead());
            contextMenuFiles.Items.Add("✏ Edit / Update", null, (s, e) => ActionEdit());
            contextMenuFiles.Items.Add("⬇ Download to PC...", null, (s, e) => ActionDownload());
            contextMenuFiles.Items.Add(new ToolStripSeparator());
            contextMenuFiles.Items.Add("🏷 Rename...", null, (s, e) => ActionRename());
            contextMenuFiles.Items.Add("📋 Copy to...", null, (s, e) => ActionCopy());
            contextMenuFiles.Items.Add("✂ Move to...", null, (s, e) => ActionMove());
            contextMenuFiles.Items.Add("🗑 Delete", null, (s, e) => ActionDelete());
            contextMenuFiles.Items.Add(new ToolStripSeparator());
            contextMenuFiles.Items.Add("ℹ Properties", null, (s, e) => ActionProperties());
            listViewFiles.ContextMenuStrip = contextMenuFiles;

            splitContainer.Panel2.Controls.Add(listViewFiles);

            // 6. Bottom Status Strip
            StatusStrip statusStrip = new StatusStrip();
            statusLabelMessage = new ToolStripStatusLabel { Text = "Ready. Click 'Connect' to begin.", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusLabelLiveAlert = new ToolStripStatusLabel { Text = "🔔 Live Sync: Ready", BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched, ForeColor = Color.DarkSlateGray };
            statusLabelCount = new ToolStripStatusLabel { Text = "0 items", BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched };
            statusLabelEndpoint = new ToolStripStatusLabel { Text = "tcp://localhost:9000", BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched };

            statusStrip.Items.AddRange(new ToolStripItem[] {
                statusLabelMessage, statusLabelLiveAlert, statusLabelCount, statusLabelEndpoint
            });

            // Add all controls in order
            this.Controls.Add(splitContainer);
            this.Controls.Add(toolStrip);
            this.Controls.Add(pnlNavigation);
            this.Controls.Add(pnlConnection);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);

            this.MainMenuStrip = menuStrip;
        }

        private void SetupIconImageList()
        {
            imageListSmall = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };

            // Dynamically generate crisp 16x16 icon bitmaps
            imageListSmall.Images.Add("folder", CreateIconBitmap(Color.FromArgb(255, 193, 7), "📁"));
            imageListSmall.Images.Add("text", CreateIconBitmap(Color.FromArgb(70, 130, 180), "📄"));
            imageListSmall.Images.Add("pdf", CreateIconBitmap(Color.FromArgb(220, 53, 69), "📕"));
            imageListSmall.Images.Add("image", CreateIconBitmap(Color.FromArgb(40, 167, 69), "🖼"));
            imageListSmall.Images.Add("archive", CreateIconBitmap(Color.FromArgb(111, 66, 193), "📦"));
            imageListSmall.Images.Add("file", CreateIconBitmap(Color.FromArgb(108, 117, 125), "📄"));

            listViewFiles.SmallImageList = imageListSmall;
            treeFolders.ImageList = imageListSmall;
        }

        private Bitmap CreateIconBitmap(Color bg, string symbol)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Font f = new Font("Segoe UI Emoji", 9F))
                using (Brush b = new SolidBrush(bg))
                {
                    g.DrawString(symbol, f, b, -2, -1);
                }
            }
            return bmp;
        }

        private string GetIconKeyForItem(FileItem item)
        {
            if (item.IsDirectory) return "folder";
            string ext = (item.Extension ?? "").ToLowerInvariant();
            switch (ext)
            {
                case ".txt":
                case ".log":
                case ".cs":
                case ".xml":
                case ".json":
                case ".config":
                    return "text";
                case ".pdf":
                    return "pdf";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    return "image";
                case ".zip":
                case ".rar":
                case ".7z":
                    return "archive";
                default:
                    return "file";
            }
        }

        #endregion

        #region Remoting Connection Management

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Please enter the server host or IP address.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid TCP port number (1 - 65535).", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                statusLabelMessage.Text = string.Format("Connecting to tcp://{0}:{1}/RemoteFileManager...", host, port);

                // Register client TCP channel if not already registered
                if (ChannelServices.GetChannel("tcpClient") == null)
                {
                    BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
                    IDictionary props = new Hashtable();
                    props["name"] = "tcpClient";
                    TcpChannel channel = new TcpChannel(props, clientProvider, null);
                    ChannelServices.RegisterChannel(channel, false);
                }

                // Obtain the remote reference to the IRemoteFileManager interface via .NET Remoting
                string url = string.Format("tcp://{0}:{1}/RemoteFileManager", host, port);
                IRemoteFileManager proxy = (IRemoteFileManager)Activator.GetObject(typeof(IRemoteFileManager), url);

                // Invoke remote Ping() method to verify active connectivity
                Stopwatch sw = Stopwatch.StartNew();
                bool pingOk = proxy.Ping();
                sw.Stop();

                if (pingOk)
                {
                    _remoteManager = proxy;
                    _isConnected = true;

                    lblConnectionStatus.Text = "● Connected ✓";
                    lblConnectionStatus.ForeColor = Color.DarkGreen;
                    statusLabelEndpoint.Text = url;
                    statusLabelMessage.Text = string.Format("Connected to server successfully (Ping: {0} ms).", sw.ElapsedMilliseconds);

                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    txtHost.Enabled = false;
                    txtPort.Enabled = false;

                    UpdateUiState();
                    LoadTreeRoot();
                    NavigateTo("");
                    UpdateServerStats();

                    _lastReceivedEventId = 0;
                    _pollTimer.Start();
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _remoteManager = null;
                lblConnectionStatus.Text = "● Connection Failed";
                lblConnectionStatus.ForeColor = Color.Red;
                statusLabelMessage.Text = "Connection failed: " + ex.Message;

                MessageBox.Show(
                    "Unable to connect to the Remote File Management Server.\n\n" +
                    "Make sure the server application is running and listening on the specified host and port.\n\n" +
                    "Details: " + ex.Message,
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _pollTimer?.Stop();

            _isConnected = false;
            _remoteManager = null;

            lblConnectionStatus.Text = "● Disconnected";
            lblConnectionStatus.ForeColor = Color.Red;
            lblServerStats.Text = "";
            statusLabelMessage.Text = "Disconnected from server.";
            statusLabelCount.Text = "0 items";

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            txtHost.Enabled = true;
            txtPort.Enabled = true;

            treeFolders.Nodes.Clear();
            listViewFiles.Items.Clear();
            txtCurrentPath.Text = "/";
            _currentPath = "";

            UpdateUiState();
        }

        private void UpdateUiState()
        {
            bool hasSelection = listViewFiles.SelectedItems.Count > 0;
            FileItem selectedItem = hasSelection ? (FileItem)listViewFiles.SelectedItems[0].Tag : null;
            bool isFile = selectedItem != null && !selectedItem.IsDirectory;

            btnNavHome.Enabled = _isConnected;
            btnNavUp.Enabled = _isConnected && !string.IsNullOrEmpty(_currentPath);
            btnRefresh.Enabled = _isConnected;
            txtSearch.Enabled = _isConnected;
            btnSearch.Enabled = _isConnected;

            btnNewFile.Enabled = _isConnected;
            btnNewFolder.Enabled = _isConnected;
            btnUpload.Enabled = _isConnected;

            btnDownload.Enabled = _isConnected && isFile;
            btnRead.Enabled = _isConnected && isFile;
            btnEdit.Enabled = _isConnected && isFile;
            btnRename.Enabled = _isConnected && hasSelection;
            btnCopy.Enabled = _isConnected && isFile;
            btnMove.Enabled = _isConnected && hasSelection;
            btnDelete.Enabled = _isConnected && hasSelection;
            btnProperties.Enabled = _isConnected && hasSelection;
        }

        private void UpdateSelectionUi()
        {
            UpdateUiState();
            if (listViewFiles.SelectedItems.Count > 0)
            {
                FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;
                statusLabelMessage.Text = string.Format("Selected: {0} ({1})", item.Name, item.IsDirectory ? "Folder" : item.FormattedSize);
            }
            else
            {
                statusLabelMessage.Text = _isConnected ? "Ready." : "Disconnected.";
            }
        }

        #endregion

        #region Directory Navigation and File Listing

        private void NavigateTo(string relativePath)
        {
            if (!_isConnected || _remoteManager == null) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                _currentPath = relativePath ?? "";
                _isSearchMode = false;
                btnClearSearch.Visible = false;

                txtCurrentPath.Text = string.IsNullOrEmpty(_currentPath) ? "/" : "/" + _currentPath;

                // Call remote method GetFiles & GetDirectories via .NET Remoting
                List<FileItem> dirs = _remoteManager.GetDirectories(_currentPath);
                List<FileItem> files = _remoteManager.GetFiles(_currentPath);

                PopulateListView(dirs, files);

                statusLabelMessage.Text = string.Format("Viewing /{0}", _currentPath);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error navigating directory: " + ex.Message, "Remoting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabelMessage.Text = "Failed to list directory contents.";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void NavigateUp()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;

            int lastSlash = _currentPath.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                NavigateTo(_currentPath.Substring(0, lastSlash));
            }
            else
            {
                NavigateTo("");
            }
        }

        private void RefreshCurrentDirectory()
        {
            if (_isSearchMode)
            {
                ActionSearch();
            }
            else
            {
                NavigateTo(_currentPath);
            }
            UpdateServerStats();
        }

        private void PopulateListView(List<FileItem> dirs, List<FileItem> files)
        {
            listViewFiles.BeginUpdate();
            listViewFiles.Items.Clear();

            int totalDirs = 0;
            int totalFiles = 0;

            if (dirs != null)
            {
                foreach (var d in dirs)
                {
                    ListViewItem lvi = new ListViewItem(d.Name);
                    lvi.SubItems.Add(d.TypeDescription);
                    lvi.SubItems.Add("--");
                    lvi.SubItems.Add(d.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    lvi.ImageKey = "folder";
                    lvi.Tag = d;
                    listViewFiles.Items.Add(lvi);
                    totalDirs++;
                }
            }

            if (files != null)
            {
                foreach (var f in files)
                {
                    ListViewItem lvi = new ListViewItem(f.Name);
                    lvi.SubItems.Add(f.TypeDescription);
                    lvi.SubItems.Add(f.FormattedSize);
                    lvi.SubItems.Add(f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    lvi.ImageKey = GetIconKeyForItem(f);
                    lvi.Tag = f;
                    listViewFiles.Items.Add(lvi);
                    totalFiles++;
                }
            }

            listViewFiles.EndUpdate();
            statusLabelCount.Text = string.Format("{0} folder(s), {1} file(s)", totalDirs, totalFiles);
        }

        private void ListViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;

            if (item.IsDirectory)
            {
                NavigateTo(item.RelativePath);
            }
            else
            {
                // If it's a text/code file, open in editor; otherwise download prompt
                string ext = (item.Extension ?? "").ToLowerInvariant();
                if (ext == ".txt" || ext == ".log" || ext == ".cs" || ext == ".xml" || ext == ".json" || ext == ".config" || ext == ".html" || ext == ".csv")
                {
                    ActionRead();
                }
                else
                {
                    if (MessageBox.Show(
                        string.Format("'{0}' is a binary file ({1}).\nWould you like to download it to your computer?", item.Name, item.TypeDescription),
                        "Download File",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ActionDownload();
                    }
                }
            }
        }

        #endregion

        #region TreeView Operations

        private void LoadTreeRoot()
        {
            treeFolders.Nodes.Clear();
            TreeNode root = new TreeNode("ServerStorage (/)", 0, 0)
            {
                Tag = ""
            };
            treeFolders.Nodes.Add(root);

            LoadTreeSubdirectories(root);
            root.Expand();
            treeFolders.SelectedNode = root;
        }

        private void LoadTreeSubdirectories(TreeNode parentNode)
        {
            if (!_isConnected || _remoteManager == null) return;

            try
            {
                string path = (string)parentNode.Tag;
                List<FileItem> subdirs = _remoteManager.GetDirectories(path);
                parentNode.Nodes.Clear();

                foreach (var dir in subdirs)
                {
                    TreeNode node = new TreeNode(dir.Name, 0, 0)
                    {
                        Tag = dir.RelativePath
                    };
                    // Dummy node for expand [+] button
                    node.Nodes.Add(new TreeNode("Loading..."));
                    parentNode.Nodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                statusLabelMessage.Text = "Tree load error: " + ex.Message;
            }
        }

        private void TreeFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            {
                e.Node.Nodes.Clear();
                LoadTreeSubdirectories(e.Node);
            }
        }

        private void TreeFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                string targetPath = (string)e.Node.Tag ?? "";
                if (targetPath != _currentPath || _isSearchMode)
                {
                    NavigateTo(targetPath);
                }
            }
        }

        #endregion

        #region File Operations (Invoking .NET Remoting Methods)

        private void ActionNewFile()
        {
            if (!_isConnected) return;

            string fileName = InputDialog.Show(this, "New Remote File", "Enter name for new file (e.g. notes.txt):");
            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("File name contains invalid characters.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string relativeFilePath = string.IsNullOrEmpty(_currentPath) ? fileName : _currentPath + "/" + fileName;

            using (var editor = new FileEditorForm(_remoteManager, relativeFilePath, isNewFile: true))
            {
                editor.ShowDialog(this);
                if (editor.FileModified)
                {
                    RefreshCurrentDirectory();
                }
            }
        }

        private void ActionNewFolder()
        {
            if (!_isConnected) return;

            string folderName = InputDialog.Show(this, "New Remote Folder", "Enter name for new directory:");
            if (string.IsNullOrWhiteSpace(folderName)) return;

            if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Folder name contains invalid characters.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetDir = string.IsNullOrEmpty(_currentPath) ? folderName : _currentPath + "/" + folderName;

            try
            {
                Cursor = Cursors.WaitCursor;
                FileOperationResult result = _remoteManager.CreateDirectory(targetDir);

                if (result.Success)
                {
                    statusLabelMessage.Text = result.Message;
                    RefreshCurrentDirectory();
                    LoadTreeRoot();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error Creating Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Remote error creating folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ActionRead()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;
            if (item.IsDirectory) return;

            using (var editor = new FileEditorForm(_remoteManager, item.RelativePath, isNewFile: false))
            {
                editor.ShowDialog(this);
                if (editor.FileModified)
                {
                    RefreshCurrentDirectory();
                }
            }
        }

        private void ActionEdit()
        {
            ActionRead();
        }

        private void ActionRename()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;

            string prompt = item.IsDirectory ? "Enter new folder name:" : "Enter new file name:";
            string newName = InputDialog.Show(this, "Rename Item", prompt, item.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                FileOperationResult result;

                if (item.IsDirectory)
                {
                    result = _remoteManager.RenameDirectory(item.RelativePath, newName);
                }
                else
                {
                    result = _remoteManager.RenameFile(item.RelativePath, newName);
                }

                if (result.Success)
                {
                    statusLabelMessage.Text = result.Message;
                    RefreshCurrentDirectory();
                    if (item.IsDirectory) LoadTreeRoot();
                }
                else
                {
                    MessageBox.Show(result.Message, "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Remote rename error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ActionCopy()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;
            if (item.IsDirectory)
            {
                MessageBox.Show("Directory copy is not supported in this version. Select a file to copy.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var folderPicker = new FolderSelectForm(_remoteManager, _currentPath))
            {
                if (folderPicker.ShowDialog(this) == DialogResult.OK)
                {
                    string targetFolder = folderPicker.SelectedFolderPath;
                    string destPath = string.IsNullOrEmpty(targetFolder) ? item.Name : targetFolder + "/" + item.Name;

                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        FileOperationResult result = _remoteManager.CopyFile(item.RelativePath, destPath);

                        if (result.Success)
                        {
                            statusLabelMessage.Text = result.Message;
                            RefreshCurrentDirectory();
                        }
                        else
                        {
                            MessageBox.Show(result.Message, "Copy Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Remote copy error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void ActionMove()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;

            using (var folderPicker = new FolderSelectForm(_remoteManager, _currentPath))
            {
                if (folderPicker.ShowDialog(this) == DialogResult.OK)
                {
                    string targetFolder = folderPicker.SelectedFolderPath;
                    string destPath = string.IsNullOrEmpty(targetFolder) ? item.Name : targetFolder + "/" + item.Name;

                    if (destPath.Equals(item.RelativePath, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Source and destination paths are identical.", "Move Aborted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        FileOperationResult result = _remoteManager.MoveFile(item.RelativePath, destPath);

                        if (result.Success)
                        {
                            statusLabelMessage.Text = result.Message;
                            RefreshCurrentDirectory();
                            if (item.IsDirectory) LoadTreeRoot();
                        }
                        else
                        {
                            MessageBox.Show(result.Message, "Move Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Remote move error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void ActionDelete()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;

            string msg = string.Format("Are you sure you want to permanently delete '{0}' from the remote server?", item.Name);
            if (MessageBox.Show(msg, "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                FileOperationResult result;

                if (item.IsDirectory)
                {
                    result = _remoteManager.DeleteDirectory(item.RelativePath);
                }
                else
                {
                    result = _remoteManager.DeleteFile(item.RelativePath);
                }

                if (result.Success)
                {
                    statusLabelMessage.Text = result.Message;
                    RefreshCurrentDirectory();
                    if (item.IsDirectory) LoadTreeRoot();
                }
                else
                {
                    MessageBox.Show(result.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Remote delete error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ActionUpload()
        {
            if (!_isConnected) return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select File from Local Computer to Upload to Server";
                ofd.Filter = "All Files (*.*)|*.*|Documents (*.txt;*.pdf;*.docx)|*.txt;*.pdf;*.docx|Images (*.jpg;*.png)|*.jpg;*.png";
                ofd.Multiselect = false;

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        string localPath = ofd.FileName;
                        string fileName = Path.GetFileName(localPath);
                        string targetRelativePath = string.IsNullOrEmpty(_currentPath) ? fileName : _currentPath + "/" + fileName;

                        statusLabelMessage.Text = string.Format("Reading local file '{0}'...", fileName);
                        byte[] fileBytes = File.ReadAllBytes(localPath);

                        statusLabelMessage.Text = string.Format("Transmitting {0:N0} bytes via .NET Remoting...", fileBytes.Length);
                        Stopwatch sw = Stopwatch.StartNew();

                        // Call remote UploadFile method
                        FileOperationResult result = _remoteManager.UploadFile(targetRelativePath, fileBytes);
                        sw.Stop();

                        if (result.Success)
                        {
                            statusLabelMessage.Text = string.Format("Uploaded {0} ({1:N0} bytes) in {2} ms.", fileName, fileBytes.Length, sw.ElapsedMilliseconds);
                            MessageBox.Show(result.Message, "Upload Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshCurrentDirectory();
                        }
                        else
                        {
                            MessageBox.Show(result.Message, "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Upload error: " + ex.Message, "Remote Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void ActionDownload()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;
            if (item.IsDirectory)
            {
                MessageBox.Show("Please select a file to download.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Save Remote File to Local Computer";
                sfd.FileName = item.Name;
                sfd.Filter = "All Files (*.*)|*.*";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        statusLabelMessage.Text = string.Format("Downloading '{0}' from server...", item.Name);
                        Stopwatch sw = Stopwatch.StartNew();

                        // Call remote DownloadFile method
                        byte[] fileBytes = _remoteManager.DownloadFile(item.RelativePath);
                        sw.Stop();

                        File.WriteAllBytes(sfd.FileName, fileBytes);

                        statusLabelMessage.Text = string.Format("Downloaded {0} ({1:N0} bytes) in {2} ms.", item.Name, fileBytes.Length, sw.ElapsedMilliseconds);
                        MessageBox.Show(
                            string.Format("File '{0}' downloaded successfully ({1:N0} bytes).\nSaved to: {2}", item.Name, fileBytes.Length, sfd.FileName),
                            "Download Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Download error: " + ex.Message, "Remote Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void ActionSearch()
        {
            if (!_isConnected) return;
            string pattern = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(pattern))
            {
                MessageBox.Show("Please enter search text or pattern.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                statusLabelMessage.Text = string.Format("Searching for '{0}' across server storage...", pattern);

                // Call remote SearchFiles method
                List<FileItem> searchResults = _remoteManager.SearchFiles(pattern, "");

                _isSearchMode = true;
                btnClearSearch.Visible = true;
                txtCurrentPath.Text = string.Format("[Search Results for: '{0}']", pattern);

                var dirs = new List<FileItem>();
                var files = new List<FileItem>();
                foreach (var itm in searchResults)
                {
                    if (itm.IsDirectory) dirs.Add(itm);
                    else files.Add(itm);
                }

                PopulateListView(dirs, files);
                statusLabelMessage.Text = string.Format("Search complete: {0} matching item(s) found.", searchResults.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error: " + ex.Message, "Remote Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ClearSearch()
        {
            txtSearch.Text = "";
            btnClearSearch.Visible = false;
            NavigateTo(_currentPath);
        }

        private void ActionProperties()
        {
            if (!_isConnected || listViewFiles.SelectedItems.Count == 0) return;
            FileItem item = (FileItem)listViewFiles.SelectedItems[0].Tag;

            try
            {
                Cursor = Cursors.WaitCursor;
                // Query fresh file info from server
                FileItem freshInfo = _remoteManager.GetFileInfo(item.RelativePath);
                using (var propForm = new FilePropertiesForm(freshInfo))
                {
                    propForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to retrieve file info: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ShowServerStorageStats()
        {
            if (!_isConnected || _remoteManager == null)
            {
                MessageBox.Show("Please connect to the server first.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                ServerStorageStats stats = _remoteManager.GetStorageStats();

                string msg = string.Format(
                    "REMOTE SERVER STORAGE DETAILS:\n\n" +
                    "• Storage Root: {0}\n" +
                    "• Total Files: {1}\n" +
                    "• Total Directories: {2}\n" +
                    "• Total Storage Size: {3} ({4:N0} bytes)\n" +
                    "• Server Host Machine: {5}\n" +
                    "• Server System Time: {6:yyyy-MM-dd HH:mm:ss}\n" +
                    "• Remoting Technology: Microsoft .NET Remoting 4.8",
                    stats.StorageRootPath,
                    stats.TotalFiles,
                    stats.TotalDirectories,
                    stats.FormattedTotalSize,
                    stats.TotalSizeBytes,
                    stats.ServerMachineName,
                    stats.ServerTime
                );

                MessageBox.Show(msg, "Server Storage Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to get storage stats: " + ex.Message, "Remoting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void UpdateServerStats()
        {
            if (!_isConnected || _remoteManager == null) return;

            try
            {
                ServerStorageStats stats = _remoteManager.GetStorageStats();
                lblServerStats.Text = string.Format("Server: {0} files | {1}", stats.TotalFiles, stats.FormattedTotalSize);
            }
            catch
            {
                // Ignore background stats refresh errors
            }
        }

        private void ShowAboutDialog()
        {
            string about =
                "===========================================================\n" +
                "  Remote File Management System Using .NET Remoting\n" +
                "===========================================================\n\n" +
                "College Project Demonstration\n\n" +
                "Key Concepts Demonstrated:\n" +
                "• .NET Remoting Distributed Architecture\n" +
                "• Remote Method Invocation (RPC) via TCP Channel\n" +
                "• Shared Contracts Library (IRemoteFileManager)\n" +
                "• MarshalByRefObject and Infinite Lifetime Leases\n" +
                "• Cross-process Object and Binary Marshaling\n" +
                "• Real-time Binary File Upload and Download\n" +
                "• Sandboxed ServerStorage and Path Traversal Protection\n\n" +
                "Built with C# and .NET Framework 4.8";

            MessageBox.Show(about, "About Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Polling

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            if (!_isConnected || _remoteManager == null) return;
            
            try
            {
                var notifications = _remoteManager.GetPendingNotifications(_lastReceivedEventId);
                if (notifications != null && notifications.Count > 0)
                {
                    bool requiresRefresh = false;
                    foreach (var n in notifications)
                    {
                        if (n.EventId > _lastReceivedEventId)
                        {
                            _lastReceivedEventId = n.EventId;
                        }

                        if (n.EventType == "SERVER_MESSAGE" || n.EventType == "ADMIN_BROADCAST")
                        {
                            LogBroadcastToFile(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [SERVER BROADCAST] {1}", n.Timestamp, n.Message));
                            statusLabelLiveAlert.Text = "📢 Broadcast: " + n.Message;
                            statusLabelLiveAlert.ForeColor = Color.DarkRed;
                            MessageBox.Show("Message from Server Administrator:\n\n" + n.Message + "\n\n(Saved to Broadcasts Log file)", "Server Broadcast", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            // File change notification
                            LogBroadcastToFile(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", n.Timestamp, n.EventType, n.Message));
                            requiresRefresh = true;
                            statusLabelLiveAlert.Text = "🔔 Live Sync: " + n.Message;
                        }
                    }

                    if (requiresRefresh)
                    {
                        _isRefreshingFromPush = true;
                        
                        var selectedRelativePaths = new List<string>();
                        foreach (ListViewItem item in listViewFiles.SelectedItems)
                        {
                            if (item.Tag is FileItem fi) selectedRelativePaths.Add(fi.RelativePath);
                        }

                        RefreshCurrentDirectory();
                        
                        foreach (ListViewItem item in listViewFiles.Items)
                        {
                            if (item.Tag is FileItem fi && selectedRelativePaths.Contains(fi.RelativePath))
                            {
                                item.Selected = true;
                            }
                        }

                        _isRefreshingFromPush = false;
                        
                        statusLabelLiveAlert.ForeColor = Color.Blue;
                        
                        var resetTimer = new System.Windows.Forms.Timer { Interval = 4000 };
                        resetTimer.Tick += (s, args) => { 
                            if (statusLabelLiveAlert.Text != "🔔 Live Sync: Ready")
                            {
                                statusLabelLiveAlert.Text = "🔔 Live Sync: Ready"; 
                                statusLabelLiveAlert.ForeColor = Color.DarkSlateGray;
                            }
                            resetTimer.Stop(); 
                            resetTimer.Dispose(); 
                        };
                        resetTimer.Start();
                    }
                }
            }
            catch
            {
                // Ignore transient polling errors
            }
        }

        private void LogBroadcastToFile(string entry)
        {
            try
            {
                if (string.IsNullOrEmpty(_broadcastLogFilePath))
                {
                    _broadcastLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_broadcasts_log.txt");
                }
                File.AppendAllText(_broadcastLogFilePath, entry + Environment.NewLine);
            }
            catch
            {
                // Silently ignore disk write issues
            }
        }

        private void ActionOpenBroadcastLog()
        {
            try
            {
                if (string.IsNullOrEmpty(_broadcastLogFilePath))
                {
                    _broadcastLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_broadcasts_log.txt");
                }

                if (!File.Exists(_broadcastLogFilePath))
                {
                    File.WriteAllText(_broadcastLogFilePath,
                        "=====================================================\r\n" +
                        "      REMOTE FILE MANAGEMENT - CLIENT BROADCAST LOG   \r\n" +
                        "=====================================================\r\n" +
                        "Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n\r\n" +
                        "All server administrator broadcasts and live push notifications\r\n" +
                        "received by this client are automatically recorded here.\r\n" +
                        "-----------------------------------------------------\r\n\r\n");
                }

                Process.Start("notepad.exe", _broadcastLogFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open log file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                RefreshCurrentDirectory();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back && txtSearch.Focused == false)
            {
                NavigateUp();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && listViewFiles.Focused && listViewFiles.SelectedItems.Count > 0)
            {
                ActionDelete();
                e.Handled = true;
            }
        }

        #endregion
    }
}
