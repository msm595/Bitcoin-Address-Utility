// Copyright 2013 msm595
// Copyright 2012 Mike Caldwell (Casascius)
// This file is part of Bitcoin Address Utility.

// Bitcoin Address Utility is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Bitcoin Address Utility is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with Bitcoin Address Utility.  If not, see http://www.gnu.org/licenses/.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Printing;
using Casascius.Bitcoin;

namespace BtcAddress.Forms {
    public partial class CryptoVest : Form {
        public CryptoVest() {
            InitializeComponent();
        }

        private bool Generating = false;
        private bool GeneratingEnded = false;

        private bool WaitClose = false;
        private bool WaitCloseConfirm = false;

        private bool StopRequested = false;

        private bool PermissionToCloseWindow = false;

        private int RemainingToGenerate = 0;

        private Thread GenerationThread = null;

        public List<KeyCollectionItem> GeneratedItems = new List<KeyCollectionItem>();

        private void button1_Click(object sender, EventArgs e) {
            if (WaitClose) {
                WaitCloseConfirm = true;
                return;
            }
            if (Generating) {
                StopRequested = true;
                button1.Text = "Stopping...";
                return;
            }

            GenerationThread = new Thread(new ThreadStart(GenerationThreadProcess));
            RemainingToGenerate = (int)numGenCount.Value;

            timer1.Interval = 250;
            timer1.Enabled = true;
            Generating = true;
            GeneratingEnded = false;
            StopRequested = false;
            button1.Text = "Stop and Close";
            SetControlsEnabled(false);
            toolStripProgressBar1.Visible = true;
            GenerationThread.Start();
        }

        private void SetControlsEnabled(bool enabled) {
            foreach (Control c in this.Controls) {
                if (c is TextBox) {
                    ((TextBox)c).Enabled = enabled;
                } else if (c is NumericUpDown) {
                    ((NumericUpDown)c).Enabled = enabled;
                }
            }
            foreach (Control c in groupBox1.Controls) {
                if (c is RadioButton) {
                    ((RadioButton)c).Enabled = enabled;
                }
            }

        }

        private void timer1_Tick(object sender, EventArgs e) {
            if (WaitCloseConfirm) {
                Application.Exit();
                return;
            }

            if (GeneratingEnded) {
                Generating = false;
                GeneratingEnded = false;
                toolStripProgressBar1.Value = 0;
                toolStripProgressBar1.Visible = false;
                toolStripStatusLabel1.Text = "Printing...";

                //btnGenerateAddresses.Text = "Generate Addresses";
                //timer1.Enabled = false;
                //SetControlsEnabled(true);
                if (StopRequested == false) {
                    //PermissionToCloseWindow = true;
                    //this.Close();
                    Print();
                } else {
                    Application.Exit();
                }
            }

            if (Generating) {
                int generated = 0;
                int totaltogenerate = 0;
                lock (GeneratedItems) {
                    generated = GeneratedItems.Count;
                    totaltogenerate = generated + RemainingToGenerate;
                }
                toolStripStatusLabel1.Text = "Keys generated: " + generated;
                toolStripProgressBar1.Maximum = totaltogenerate;
                toolStripProgressBar1.Value = generated;
            }
        }

        private void Print() {
            WaitClose = true;
            PrintDialog pd = new PrintDialog();
            PrinterSettings ps = new PrinterSettings();
            pd.PrinterSettings = ps;
            DialogResult dr = pd.ShowDialog();

            if (dr == DialogResult.OK) {
                CoinInsert printer;
                printer = new CoinInsert();

                printer.keys = GeneratedItems;
                printer.PrinterSettings = pd.PrinterSettings;
                printer.DenseMode = true;
                printer.Print();
                button1.Text = "Close";
                toolStripStatusLabel1.Text = "Done printing.";
                WaitCloseConfirm = true;
            }
        }

        private void GenerationThreadProcess() {
            while (RemainingToGenerate > 0 && StopRequested == false) {
                KeyCollectionItem newitem = null;
                MiniKeyPair mkp = MiniKeyPair.CreateRandom(ExtraEntropy.GetEntropy(), (byte)(rdoLitecoin.Checked? 48 : 0));
                string s = mkp.AddressBase58; // read the property to entice it to compute everything
                newitem = new KeyCollectionItem(mkp); 

                lock (GeneratedItems) {
                    GeneratedItems.Add(newitem);
                    RemainingToGenerate--;
                }
            }
            GeneratingEnded = true;
        }

        private void CryptoVest_FormClosing(object sender, FormClosingEventArgs e) {
            if (PermissionToCloseWindow) return;
            if (Generating) {
                if (MessageBox.Show("Cancel and abandon generation in progress?", "Abort generation", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No) {
                    e.Cancel = true;
                } else {
                    StopRequested = true;
                    if (GenerationThread.ThreadState == System.Threading.ThreadState.Running) {
                        GenerationThread.Join();
                        GeneratedItems.Clear();
                        Generating = false;
                    }
                    Application.Exit();
                }
            }
        }
    }
}
