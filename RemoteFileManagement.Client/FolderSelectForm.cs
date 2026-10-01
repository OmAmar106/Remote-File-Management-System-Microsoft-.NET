using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Client
{
    /// <summary>
    /// Dialog for selecting a destination folder on the server (used for Copy and Move operations).
    /// Dynamically queries directory structures from the server using .NET Remoting.
    /// </summary>
    public class FolderSelectForm : Form
    {
        private readonly IRemoteFileManager _remoteManager;
        private TreeView treeFolders;
        private Button btnSelect;
        private Button btnCancel;
        private Button btnNewFolder;
        private Label lblSelected;

        public string SelectedFolderPath { get; private set; }

        public FolderSelectForm(IRemoteFileManager remoteManager, string initialPath = "")
        {
            _remoteManager = remoteManager;
            SelectedFolderPath = initialPath ?? "";
            InitializeComponent();
            LoadRootDirectories();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Destination Server Folder";
            this.Size = new Size(460, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblHeader = new Label
            {
                Text = "Choose a destination folder on the server:",
                Location = new Point(16, 14),
                Size = new Size(410, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            treeFolders = new TreeView
            {
                Location = new Point(16, 42),
                Size = new Size(410, 310),
                HideSelection = false
            };
            treeFolders.AfterSelect += TreeFolders_AfterSelect;
            treeFolders.BeforeExpand += TreeFolders_BeforeExpand;

            lblSelected = new Label
            {
                Text = "Selected: / (ServerStorage root)",
                Location = new Point(16, 362),
                Size = new Size(410, 22),
                ForeColor = Color.DarkSlateGray
            };

            btnNewFolder = new Button
            {
                Text = "+ New Folder",
                Location = new Point(16, 395),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(235, 238, 242),
                FlatStyle = FlatStyle.Flat
            };
            btnNewFolder.Click += BtnNewFolder_Click;

            btnSelect = new Button
            {
                Text = "Select",
                DialogResult = DialogResult.OK,
                Location = new Point(236, 395),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSelect.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(336, 395),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(225, 228, 232),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblHeader);
            this.Controls.Add(treeFolders);
            this.Controls.Add(lblSelected);
            this.Controls.Add(btnNewFolder);
            this.Controls.Add(btnSelect);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSelect;
            this.CancelButton = btnCancel;
        }

        private void LoadRootDirectories()
        {
            treeFolders.Nodes.Clear();
            TreeNode rootNode = new TreeNode("ServerStorage (/)", 0, 0)
            {
                Tag = ""
            };
            treeFolders.Nodes.Add(rootNode);

            LoadSubdirectories(rootNode);
            rootNode.Expand();
            treeFolders.SelectedNode = rootNode;
        }

        private void LoadSubdirectories(TreeNode parentNode)
        {
            try
            {
                string path = (string)parentNode.Tag;
                List<FileItem> dirs = _remoteManager.GetDirectories(path);
                parentNode.Nodes.Clear();

                foreach (var dir in dirs)
                {
                    TreeNode node = new TreeNode(dir.Name)
                    {
                        Tag = dir.RelativePath
                    };
                    // Add dummy child to show expand [+] handle
                    node.Nodes.Add(new TreeNode("Loading..."));
                    parentNode.Nodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load directories: " + ex.Message, "Remote Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TreeFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            {
                e.Node.Nodes.Clear();
                LoadSubdirectories(e.Node);
            }
        }

        private void TreeFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectedFolderPath = (string)e.Node.Tag ?? "";
            lblSelected.Text = string.IsNullOrEmpty(SelectedFolderPath)
                ? "Selected: / (ServerStorage root)"
                : "Selected: /" + SelectedFolderPath;
        }

        private void BtnNewFolder_Click(object sender, EventArgs e)
        {
            string name = InputDialog.Show(this, "New Folder", "Enter new folder name on server:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                string target = string.IsNullOrEmpty(SelectedFolderPath) ? name : SelectedFolderPath + "/" + name;
                var res = _remoteManager.CreateDirectory(target);
                if (res.Success)
                {
                    if (treeFolders.SelectedNode != null)
                    {
                        LoadSubdirectories(treeFolders.SelectedNode);
                        treeFolders.SelectedNode.Expand();
                    }
                }
                else
                {
                    MessageBox.Show(res.Message, "Error Creating Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
