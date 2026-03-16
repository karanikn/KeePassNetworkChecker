using System;
using System.Drawing;
using System.Windows.Forms;
using KeePass.Plugins;

namespace KeePassNetworkChecker
{
    public class SettingsForm : Form
    {
        private readonly IPluginHost m_host;
        private CheckBox m_chkShowWindow;

        public SettingsForm(IPluginHost host)
        {
            m_host = host;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = "Network Checker Options";
            Size            = new Size(420, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            Font            = new Font("Segoe UI", 9f);

            Label lblTitle = new Label();
            lblTitle.Text     = "Network Checker Options";
            lblTitle.Font     = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 12);

            m_chkShowWindow = new CheckBox();
            m_chkShowWindow.Text     = "Show popup window when using Network Check";
            m_chkShowWindow.Location = new Point(12, 50);
            m_chkShowWindow.AutoSize = true;
            m_chkShowWindow.Checked  = m_host.CustomConfig.GetBool(KeePassNetworkCheckerExt.CfgShowWindow, true);

            Label lblHint = new Label();
            lblHint.Text      = "To show the status column: View \u2192 Configure Columns \u2192 enable 'Net Status'";
            lblHint.ForeColor = SystemColors.GrayText;
            lblHint.Location  = new Point(12, 80);
            lblHint.Size      = new Size(380, 32);

            Button btnOk = new Button();
            btnOk.Text         = "OK";
            btnOk.Size         = new Size(75, 26);
            btnOk.Location     = new Point(320, 112);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click       += OnOkClick;

            Controls.Add(lblTitle);
            Controls.Add(m_chkShowWindow);
            Controls.Add(lblHint);
            Controls.Add(btnOk);
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            m_host.CustomConfig.SetBool(KeePassNetworkCheckerExt.CfgShowWindow, m_chkShowWindow.Checked);
            Close();
        }
    }
}
