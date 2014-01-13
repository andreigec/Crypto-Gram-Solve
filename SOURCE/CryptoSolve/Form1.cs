using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ANDREICSLIB;
using CryptoGramSolve.ServiceReference1;

namespace CryptoSolve
{
    public partial class Form1 : Form
    {
        #region licensing

        private const string AppTitle = "CryptoGramSolve";
        private const double AppVersion = 0.3;
        private const String HelpString = "";

        private readonly String OtherText =
            @"©" + DateTime.Now.Year +
            @" Andrei Gec (http://www.andreigec.net)

Licensed under GNU LGPL (http://www.gnu.org/)

Zip Assets © SharpZipLib (http://www.sharpdevelop.net/OpenSource/SharpZipLib/)
";
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private bool ThreadActive()
        {
            return (t != null && t.IsAlive);
        }

        public delegate void IncrementCounter(String v);
        public delegate bool ResultCallBack(String s);

        public DateTime? LastUpdate = null;

        private Thread t = null;

        public bool GetResultIsOK(String s)
        {
            if (string.IsNullOrWhiteSpace(decryptedtext.Text) == false)
                decryptedtext.Text += "\r\n";

            decryptedtext.Text += s;
            return stopAfterFirstResultToolStripMenuItem.Checked;
        }

        public void IncrementProgress(String v)
        {
            if (LastUpdate == null || (DateTime.Now - (DateTime)LastUpdate).TotalMilliseconds > 500)
            {
                LastUpdate = DateTime.Now;
            }
            else
                return;

            statuslabel.Text = v;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            Licensing.CreateLicense(this, menuStrip1, new Licensing.SolutionDetails(GetDetails, HelpString, AppTitle, AppVersion, OtherText));
            Solver.Init();
            statuslabel.Text = "";
        }

        public Licensing.DownloadedSolutionDetails GetDetails()
        {
            try
            {
                var sr = new ServicesClient();
                var ti = sr.GetTitleInfo(AppTitle);
                if (ti == null)
                    return null;
                return ToDownloadedSolutionDetails(ti);

            }
            catch (Exception)
            {
            }
            return null;
        }

        public static Licensing.DownloadedSolutionDetails ToDownloadedSolutionDetails(TitleInfoServiceModel tism)
        {
            return new Licensing.DownloadedSolutionDetails()
            {
                ZipFileLocation = tism.LatestTitleDownloadPath,
                ChangeLog = tism.LatestTitleChangelog,
                Version = tism.LatestTitleVersion
            };
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void setbuttons(bool running)
        {
            if (running)
            {
                decodebutton.Text = "Abort";
            }
            else
            {
                decodebutton.Text = "Decode Cryptogram";
                statuslabel.Text = "";
            }
        }

        private void Solve()
        {
            if (ThreadActive())
                t = null;

            if (t != null)
                return;

            statuslabel.Text = "0";

            t = new Thread(() => Solver.Solve(encryptedtext.Text, GetResultIsOK, IncrementProgress));
            t.Start();
            threadcheck.Enabled = true;
        }

        private void decodebutton_Click(object sender, EventArgs e)
        {
            if (ThreadActive())
                abort();
            else
                Solve();
        }

        private void loadbutton_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "|*.txt";
            var res = ofd.ShowDialog();
            if (res != DialogResult.OK)
                return;

            LoadFile(ofd.FileName);
        }

        private void LoadFile(String fn)
        {
            var t = FileExtras.LoadFile(fn);

            var s1 = t.Split(new[] { '\f' }, StringSplitOptions.None);
            if (!s1.Any())
            {
                MessageBox.Show("Error loading file");
                return;
            }

            encryptedtext.Text = s1[0];

            if (s1.Count() >= 1)
                decryptedtext.Text = s1[1];
        }

        private void SaveToFile(String filename)
        {
            var o = encryptedtext.Text + '\f' + decryptedtext.Text;
            FileExtras.SaveToFile(filename, o);
        }

        private void threadcheck_Tick(object sender, EventArgs e)
        {
            if (ThreadActive() == false)
            {
                t = null;
                threadcheck.Enabled = false;
            }

            setbuttons(t != null);
            setbuttons(t != null);
        }

        private void abort()
        {
            if (ThreadActive())
            {
                t.Abort();
                t = null;
            }

            setbuttons(false);
        }

        private void stopAfterFirstResultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopAfterFirstResultToolStripMenuItem.Checked = !stopAfterFirstResultToolStripMenuItem.Checked;
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var helpstr = @"Help:
";

            MessageBox.Show(helpstr, "Help", MessageBoxButtons.OK);
        }

        private void savebutton_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "|*.txt";
            var res = sfd.ShowDialog();
            if (res != DialogResult.OK)
                return;
            SaveToFile(sfd.FileName);
        }



        private void encodebutton_Click(object sender, EventArgs e)
        {
            var et = Solver.EncodeText(decryptedtext.Text);
            encryptedtext.Text = et;
        }

        private void decryptedtext_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = TextboxExtras.HandleInput(TextboxExtras.InputType.CreateAllTrue(), e.KeyChar);
        }

        private void encryptedtext_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = TextboxExtras.HandleInput(TextboxExtras.InputType.CreateAllTrue(), e.KeyChar);
        }
    }
}
