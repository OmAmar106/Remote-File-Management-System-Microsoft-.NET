using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Client
{
    /// <summary>
    /// Text file editor and viewer dialog.
    /// Demonstrates remote file reading, updating, and appending via .NET Remoting.
    /// </summary>
    public class FileEditorForm : Form
    {
        private readonly IRemoteFileManager _remoteManager;
        private readonly string _relativeFilePath;
        private readonly bool _isNewFile;

        private TextBox txtContent;
        private ToolStripStatusLabel lblStats;
        private ToolStripStatusLabel lblSaveStatus;
        private Button btnSave;
        private Button btnAppend;
        private Button btnReload;
        private CheckBox chkWordWrap;

        public bool FileModified { get; private set; }

        public FileEditorForm(IRemoteFileManager remoteManager, string relativeFilePath, bool isNewFile = false)
        {
            _remoteManager = remoteManager;
            _relativeFilePath = relativeFilePath;
            _isNewFile = isNewFile;

            InitializeComponent();
            LoadRemoteContent();
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("{0} - Remote Text Editor (.NET Remoting)", System.IO.Path.GetFileName(_relativeFilePath));
            this.Size = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            // Top action bar
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(240, 243, 246),
                Padding = new Padding(10, 8, 10, 8)
            };

            btnSave = new Button
            {
                Text = "💾 Save (Update)",
                Location = new Point(12, 10),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnAppend = new Button
            {
                Text = "➕ Append to File",
                Location = new Point(150, 10),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAppend.FlatAppearance.BorderSize = 0;
            btnAppend.Click += BtnAppend_Click;

            btnReload = new Button
            {
                Text = "🔄 Reload",
                Location = new Point(288, 10),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(220, 224, 230),
                FlatStyle = FlatStyle.Flat
            };
            btnReload.FlatAppearance.BorderSize = 0;
            btnReload.Click += BtnReload_Click;

            chkWordWrap = new CheckBox
            {
                Text = "Word Wrap",
                Checked = true,
                Location = new Point(400, 14),
                Size = new Size(100, 24)
            };
            chkWordWrap.CheckedChanged += (s, e) => { txtContent.WordWrap = chkWordWrap.Checked; };

            Label lblPath = new Label
            {
                Text = "Server Path: /" + _relativeFilePath,
                Location = new Point(510, 15),
                Size = new Size(290, 20),
                ForeColor = Color.DarkSlateGray,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlTop.Controls.Add(btnSave);
            pnlTop.Controls.Add(btnAppend);
            pnlTop.Controls.Add(btnReload);
            pnlTop.Controls.Add(chkWordWrap);
            pnlTop.Controls.Add(lblPath);

            // Editor text box
            txtContent = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))),
                WordWrap = true
            };
            txtContent.TextChanged += TxtContent_TextChanged;

            // Status bar
            StatusStrip statusStrip = new StatusStrip();
            lblSaveStatus = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblStats = new ToolStripStatusLabel
            {
                Text = "Lines: 0 | Chars: 0",
                Alignment = ToolStripItemAlignment.Right
            };
            statusStrip.Items.Add(lblSaveStatus);
            statusStrip.Items.Add(lblStats);

            this.Controls.Add(txtContent);
            this.Controls.Add(pnlTop);
            this.Controls.Add(statusStrip);
        }

        private void LoadRemoteContent()
        {
            if (_isNewFile)
            {
                txtContent.Text = "";
                lblSaveStatus.Text = "New file created locally. Click 'Save' to commit to server.";
                UpdateMetrics();
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                string content = _remoteManager.ReadFile(_relativeFilePath);
                txtContent.Text = content ?? "";
                txtContent.SelectionStart = 0;
                txtContent.SelectionLength = 0;
                lblSaveStatus.Text = "File loaded from server successfully.";
                UpdateMetrics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read file from server: " + ex.Message, "Remote Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblSaveStatus.Text = "Error reading remote file.";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                FileOperationResult result;

                if (_isNewFile)
                {
                    result = _remoteManager.CreateFile(_relativeFilePath, txtContent.Text);
                }
                else
                {
                    result = _remoteManager.UpdateFile(_relativeFilePath, txtContent.Text);
                }

                if (result.Success)
                {
                    FileModified = true;
                    lblSaveStatus.Text = string.Format("Saved at {0:HH:mm:ss} - {1}", DateTime.Now, result.Message);
                    MessageBox.Show(result.Message, "Save Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblSaveStatus.Text = "Save failed: " + result.Message;
                    MessageBox.Show(result.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception during remote save: " + ex.Message, "Remoting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnAppend_Click(object sender, EventArgs e)
        {
            string appendText = InputDialog.Show(this, "Append Text", "Enter text to append to this server file:");
            if (!string.IsNullOrEmpty(appendText))
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    // Automatically add newline if needed
                    if (!appendText.EndsWith("\r\n") && !appendText.EndsWith("\n"))
                    {
                        appendText = "\r\n" + appendText;
                    }

                    var result = _remoteManager.AppendToFile(_relativeFilePath, appendText);
                    if (result.Success)
                    {
                        FileModified = true;
                        LoadRemoteContent(); // Refresh view
                        lblSaveStatus.Text = "Appended text to remote file successfully.";
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Append Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error appending: " + ex.Message, "Remoting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Discard current unsaved changes and reload from server?", "Confirm Reload", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoadRemoteContent();
            }
        }

        private void TxtContent_TextChanged(object sender, EventArgs e)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            int lineCount = txtContent.Lines.Length;
            int charCount = txtContent.Text.Length;
            lblStats.Text = string.Format("Lines: {0:N0} | Chars: {1:N0}", lineCount, charCount);
        }
    }
}
