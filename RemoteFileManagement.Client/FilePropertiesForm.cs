using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteFileManagement.Shared;

namespace RemoteFileManagement.Client
{
    /// <summary>
    /// Displays detailed metadata and attributes for a remote file or directory.
    /// </summary>
    public class FilePropertiesForm : Form
    {
        public FilePropertiesForm(FileItem item)
        {
            InitializeComponent(item);
        }

        private void InitializeComponent(FileItem item)
        {
            this.Text = (item.IsDirectory ? "Folder Properties - " : "File Properties - ") + item.Name;
            this.Size = new Size(480, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.BackColor = Color.FromArgb(248, 249, 250);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White
            };

            Label lblTitle = new Label
            {
                Text = item.Name,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(20, 12),
                Size = new Size(430, 25),
                AutoEllipsis = true
            };

            Label lblSubtitle = new Label
            {
                Text = item.IsDirectory ? "Directory on Remote Server" : item.TypeDescription,
                ForeColor = Color.DarkGray,
                Location = new Point(20, 38),
                Size = new Size(430, 20)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            TableLayoutPanel table = new TableLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(425, 230),
                ColumnCount = 2,
                RowCount = 6
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddPropertyRow(table, "Item Type:", item.TypeDescription);
            AddPropertyRow(table, "Server Path:", "/" + (item.RelativePath ?? ""));
            AddPropertyRow(table, "File Size:", item.IsDirectory ? "--" : string.Format("{0} ({1:N0} bytes)", item.FormattedSize, item.Size));
            AddPropertyRow(table, "File Extension:", string.IsNullOrEmpty(item.Extension) ? "(None)" : item.Extension);
            AddPropertyRow(table, "Created On:", item.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            AddPropertyRow(table, "Last Modified:", item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));

            Button btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(345, 330),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;

            this.Controls.Add(pnlHeader);
            this.Controls.Add(table);
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
        }

        private void AddPropertyRow(TableLayoutPanel table, string labelText, string valueText)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label val = new Label
            {
                Text = valueText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            table.Controls.Add(lbl);
            table.Controls.Add(val);
        }
    }
}
