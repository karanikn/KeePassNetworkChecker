using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using KeePassLib;

namespace KeePassNetworkChecker
{
    public class NetworkCheckerForm : Form
    {
        private readonly PwEntry[] _entries;
        private readonly KeePassNetworkCheckerExt _plugin;
        private DataGridView _grid;
        private Button _btnRefresh;
        private Button _btnClose;
        private Label _lblStatus;
        private BackgroundWorker _worker;

        private class CheckResult
        {
            public PwEntry Entry;
            public string Device;
            public string URL;
            public string Ping;
            public string Port;
            public string Web;
            public bool   PingOk;
            public bool   PortOk;
            public bool   WebOk;
            public bool   AnyOk { get { return PingOk || PortOk || WebOk; } }
        }

        public NetworkCheckerForm(PwEntry[] entries, KeePassNetworkCheckerExt plugin)
        {
            _entries = entries;
            _plugin  = plugin;
            BuildUI();
            SetupWorker();
        }

        private void BuildUI()
        {
            Text          = "Network Checker";
            Size          = new Size(860, 460);
            MinimumSize   = new Size(700, 380);
            StartPosition = FormStartPosition.CenterParent;
            Font          = new Font("Segoe UI", 9f);

            Label lblTitle = new Label();
            lblTitle.Text     = "Network Checker";
            lblTitle.Font     = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 12);

            _lblStatus = new Label();
            _lblStatus.Text     = "Ready.";
            _lblStatus.AutoSize = true;
            _lblStatus.Location = new Point(12, 46);

            _grid = new DataGridView();
            _grid.Location              = new Point(12, 70);
            _grid.Anchor                = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.Size                  = new Size(820, 300);
            _grid.BorderStyle           = BorderStyle.FixedSingle;
            _grid.CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal;
            _grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect           = false;
            _grid.ReadOnly              = true;
            _grid.AllowUserToAddRows    = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible     = false;
            _grid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.RowTemplate.Height    = 24;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Device",  HeaderText = "Device",  FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "URL",     HeaderText = "URL",     FillWeight = 28 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ping",    HeaderText = "Ping",    FillWeight = 13 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Port",    HeaderText = "Port",    FillWeight = 17 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Web",     HeaderText = "HTTP",    FillWeight = 11 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Overall", HeaderText = "Status",  FillWeight = 11 });
            _grid.CellFormatting += Grid_CellFormatting;

            _btnRefresh = new Button();
            _btnRefresh.Text   = "Refresh";
            _btnRefresh.Size   = new Size(80, 26);
            _btnRefresh.Click += OnRefreshClick;

            _btnClose = new Button();
            _btnClose.Text         = "Close";
            _btnClose.Size         = new Size(80, 26);
            _btnClose.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel pnl = new FlowLayoutPanel();
            pnl.FlowDirection = FlowDirection.RightToLeft;
            pnl.Dock          = DockStyle.Bottom;
            pnl.Height        = 40;
            pnl.Padding       = new Padding(4);
            pnl.Controls.Add(_btnClose);
            pnl.Controls.Add(_btnRefresh);

            Controls.Add(lblTitle);
            Controls.Add(_lblStatus);
            Controls.Add(_grid);
            Controls.Add(pnl);

            Resize += (s, e) => _grid.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 130);
            Load   += (s, e) => StartChecks();
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            string col = _grid.Columns[e.ColumnIndex].Name;
            string val = e.Value.ToString();

            if (col == "Overall")
            {
                e.CellStyle.ForeColor = val == "UP" ? Color.Green : Color.Red;
                e.CellStyle.Font      = new Font(_grid.Font, FontStyle.Bold);
            }
            else if (col == "Ping" || col == "Port" || col == "Web")
            {
                bool ok = val != "N/A" && !val.StartsWith("ERR") &&
                          !val.StartsWith("TIMEOUT") && !val.StartsWith("FAIL");
                e.CellStyle.ForeColor = ok ? Color.Green : Color.Red;
            }
        }

        private void SetupWorker()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork             += Worker_DoWork;
            _worker.ProgressChanged    += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_Completed;
        }

        private void StartChecks()
        {
            if (_worker.IsBusy) return;
            _btnRefresh.Enabled = false;
            _lblStatus.Text     = "Checking...";
            _grid.Rows.Clear();
            _worker.RunWorkerAsync(_entries);
        }

        private void OnRefreshClick(object sender, EventArgs e) { StartChecks(); }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            PwEntry[] entries = (PwEntry[])e.Argument;
            List<CheckResult> results = new List<CheckResult>();

            foreach (PwEntry entry in entries)
            {
                string title  = entry.Strings.ReadSafe("Title");
                string url    = entry.Strings.ReadSafe("URL").Trim();
                CheckResult r = new CheckResult { Entry = entry, Device = title, URL = url };

                if (string.IsNullOrEmpty(url))
                {
                    r.Ping = "N/A"; r.Port = "N/A"; r.Web = "N/A";
                    results.Add(r);
                    _worker.ReportProgress(results.Count, title);
                    continue;
                }

                string fullUrl = url.Contains("://") ? url : "http://" + url;
                string host = url;
                int port = 80;
                try
                {
                    Uri uri = new Uri(fullUrl);
                    host = uri.Host;
                    port = uri.IsDefaultPort ? (uri.Scheme == "https" ? 443 : 80) : uri.Port;
                }
                catch { }

                Tuple<bool,long> pingRes = DoPing(host);
                r.PingOk = pingRes.Item1;
                r.Ping   = pingRes.Item1 ? pingRes.Item2 + " ms" : "TIMEOUT";

                Tuple<bool,long> portRes = DoPortCheck(host, port);
                r.PortOk = portRes.Item1;
                r.Port   = portRes.Item1 ? port + " OK (" + portRes.Item2 + "ms)" : port + " FAIL";

                Tuple<bool,int> httpRes = DoHttpCheck(fullUrl);
                r.WebOk = httpRes.Item1;
                r.Web   = httpRes.Item1 ? httpRes.Item2.ToString() : "ERR " + httpRes.Item2;

                results.Add(r);
                _worker.ReportProgress(results.Count, title);
            }
            e.Result = results;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _lblStatus.Text = "Checking " + (string)e.UserState + "...";
        }

        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _lblStatus.Text = "Error: " + e.Error.Message;
                _btnRefresh.Enabled = true;
                return;
            }

            List<CheckResult> results = (List<CheckResult>)e.Result;
            _grid.Rows.Clear();

            foreach (CheckResult r in results)
            {
                int idx = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[idx];
                row.Cells["Device"].Value  = r.Device;
                row.Cells["URL"].Value     = r.URL;
                row.Cells["Ping"].Value    = r.Ping  ?? "N/A";
                row.Cells["Port"].Value    = r.Port  ?? "N/A";
                row.Cells["Web"].Value     = r.Web   ?? "N/A";
                row.Cells["Overall"].Value = r.AnyOk ? "UP" : "DOWN";

                // Update Net Status column in KeePass for this entry only
                if (_plugin != null && _plugin.ColProvider != null && r.Entry != null)
                    _plugin.ColProvider.SetStatus(r.Entry.Uuid.ToHexString(), r.AnyOk);
            }

            // Refresh KeePass entry list to show updated Net Status column
            if (_plugin != null && _plugin.ColProvider != null)
                _plugin.ColProvider.RefreshUI();

            _lblStatus.Text     = "Last checked: " + DateTime.Now.ToString("HH:mm:ss") +
                                  "  -  " + results.Count + " device(s)";
            _btnRefresh.Enabled = true;
        }

        private static Tuple<bool,long> DoPing(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(host, 3000);
                    if (reply != null && reply.Status == IPStatus.Success)
                        return Tuple.Create(true, reply.RoundtripTime);
                }
            }
            catch { }
            return Tuple.Create(false, 0L);
        }

        private static Tuple<bool,long> DoPortCheck(string host, int port)
        {
            try
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(host, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(3000);
                    sw.Stop();
                    if (ok && client.Connected)
                    {
                        try { client.EndConnect(ar); } catch { }
                        return Tuple.Create(true, sw.ElapsedMilliseconds);
                    }
                }
            }
            catch { }
            return Tuple.Create(false, 0L);
        }

        private static Tuple<bool,int> DoHttpCheck(string url)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    delegate(object s,
                             System.Security.Cryptography.X509Certificates.X509Certificate c,
                             System.Security.Cryptography.X509Certificates.X509Chain ch,
                             System.Net.Security.SslPolicyErrors er) { return true; };
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768;

                HttpWebRequest req    = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout           = 6000;
                req.AllowAutoRedirect = true;
                req.Method            = "GET";
                req.UserAgent         = "KeePassNetworkChecker/1.2";

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    int code = (int)resp.StatusCode;
                    return Tuple.Create(code >= 200 && code < 400, code);
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse err = ex.Response as HttpWebResponse;
                if (err != null) return Tuple.Create(false, (int)err.StatusCode);
            }
            catch { }
            return Tuple.Create(false, 0);
        }
    }
}
