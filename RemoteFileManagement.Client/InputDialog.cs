using System;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteFileManagement.Client
{
    /// <summary>
    /// Reusable prompt dialog for collecting single-line text input (e.g. file name, folder name).
    /// </summary>
    public class InputDialog : Form
    {
        private Label lblPrompt;
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;

        public string InputText
        {
            get { return txtInput.Text; }
            set { txtInput.Text = value; }
        }

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent(title, prompt, defaultValue);
        }

        private void InitializeComponent(string title, string prompt, string defaultValue)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 160);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.BackColor = Color.FromArgb(245, 247, 250);

            lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(20, 20),
                Size = new Size(380, 25),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            txtInput = new TextBox
            {
                Text = defaultValue,
                Location = new Point(20, 55),
                Size = new Size(380, 26)
            };

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 105),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(310, 105),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(225, 228, 232),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            txtInput.SelectAll();
        }

        public static string Show(IWin32Window owner, string title, string prompt, string defaultValue = "")
        {
            using (var dlg = new InputDialog(title, prompt, defaultValue))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.InputText;
                }
                return null;
            }
        }
    }
}
