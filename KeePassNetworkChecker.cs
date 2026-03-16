using System;
using System.Collections.Generic;
using System.Windows.Forms;
using KeePass.Plugins;
using KeePassLib;

namespace KeePassNetworkChecker
{
    public sealed class KeePassNetworkCheckerExt : Plugin
    {
        private IPluginHost m_host = null;
        internal NetworkStatusColumnProvider ColProvider = null;

        internal const string CfgShowWindow = "KeePassNetworkChecker.ShowWindow";

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;
            ColProvider = new NetworkStatusColumnProvider(m_host);
            m_host.ColumnProviderPool.Add(ColProvider);
            return true;
        }

        public override void Terminate()
        {
            if (ColProvider != null)
            {
                m_host.ColumnProviderPool.Remove(ColProvider);
                ColProvider = null;
            }
            m_host = null;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t == PluginMenuType.Entry)
            {
                ToolStripMenuItem tsmi = new ToolStripMenuItem();
                tsmi.Text = "Network Check (Ping / Port / HTTP)";
                tsmi.Click += delegate(object s, EventArgs e)
                {
                    PwEntry[] sel = m_host.MainWindow.GetSelectedEntries();
                    if (sel == null || sel.Length == 0) return;
                    PwEntry[] entries = new PwEntry[sel.Length];
                    sel.CopyTo(entries, 0);
                    using (NetworkCheckerForm form = new NetworkCheckerForm(entries, this))
                        form.ShowDialog(m_host.MainWindow);
                };
                return tsmi;
            }

            if (t == PluginMenuType.Group)
            {
                ToolStripMenuItem tsmi = new ToolStripMenuItem();
                tsmi.Text = "Network Check All Group Entries";
                tsmi.Click += delegate(object s, EventArgs e)
                {
                    PwGroup grp = m_host.MainWindow.GetSelectedGroup();
                    if (grp == null) return;
                    List<PwEntry> list = new List<PwEntry>();
                    foreach (PwEntry pe in grp.Entries)
                    {
                        if (!string.IsNullOrEmpty(pe.Strings.ReadSafe("URL").Trim()))
                            list.Add(pe);
                    }
                    if (list.Count == 0)
                    {
                        MessageBox.Show("No entries with URLs found in this group.",
                            "Network Checker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    using (NetworkCheckerForm form = new NetworkCheckerForm(list.ToArray(), this))
                        form.ShowDialog(m_host.MainWindow);
                };
                return tsmi;
            }

            if (t == PluginMenuType.Main)
            {
                ToolStripMenuItem tsmi = new ToolStripMenuItem();
                tsmi.Text = "Network Checker Options";
                tsmi.Click += delegate(object s, EventArgs e)
                {
                    using (SettingsForm form = new SettingsForm(m_host))
                        form.ShowDialog(m_host.MainWindow);
                };
                return tsmi;
            }

            return null;
        }
    }
}
