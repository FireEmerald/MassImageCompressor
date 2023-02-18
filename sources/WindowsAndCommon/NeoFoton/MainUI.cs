using System;
using System.Drawing;
using System.Windows.Forms;
using NeoFotonCommon;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace NeoFoton
{
    public partial class MainUI : Form, IImgCompressorView
    {
        IImageCompressorPresenter presenter;
        #region IImgCompressorView Members
        /// <summary>
        /// Reserved for future Use
        /// </summary>
        public bool CompressImage { get; set; }

        /// <summary>
        /// Reserved for future Use
        /// </summary>
        public string ImagePath { get; set; }

        public string InputDirPath { get; set; }

        public int QualityCompression { get; set; }

        public bool FixedHeight { get; set; }

        public decimal FileSize
        {
            get
            {
                decimal sizeToReturn = 0;
                this.Invoke((MethodInvoker)delegate
                      {
                          if (!chkFileSize.Checked)
                              sizeToReturn = 0;
                          else
                          {
                              if (cmbKBMB.SelectedItem == "KB")
                                  sizeToReturn = fileSize * 1024;
                              else
                                  sizeToReturn = fileSize * 1024 * 1024;
                          }
                      });
                return sizeToReturn;
            }
        }

        /// <summary>
        ///  If FixedHeight is false then Height represents % of current height
        /// </summary>
        public int Height { get; set; }

        public string OutputDirPath { get; set; }

        public string Message { get; set; }
        public SupportedMimeType mimeTypeToSave { get; set; }
        #endregion

        private uint zoomLevel = 100;
        private string compressedImgPath = "";
        private int compressedImgHeight;
        private int compressedImgWidth;
        private bool checkFileSize = false;
        private decimal fileSize;
        private string fileSizeUnit = "KB";
        private bool autoUpdate = true;



        private ProgressDlg progressDlg = new ProgressDlg();
        Timer takeBackupBlinkTimer = new Timer();
        Timer controlHighlightTimer = new Timer();


        public MainUI()
        {

            takeBackupBlinkTimer.Interval = 500;
            takeBackupBlinkTimer.Enabled = false;
            takeBackupBlinkTimer.Start();
            takeBackupBlinkTimer.Tick += new EventHandler(takeBackupBlinkTimer_Tick);

            InitializeComponent();
            SetToolTips();
            rbSizePercentage.Checked = true;
            this.presenter = new ImageCompressorPresenter();
            this.presenter.View = this;

            LinkLabel.Link link = new LinkLabel.Link();
            link.LinkData = "http://icompressor.blogspot.in/2016/10/introduction-to-hassle-free-image.html";
            linkToBlog.Links.Add(link);

            LinkLabel.Link linkToFbk = new LinkLabel.Link();
            linkToFbk.LinkData = "http://icompressor.blogspot.in/2016/10/give-me-your-feedback.html";
            linkToFeedback.Links.Add(linkToFbk);

            LinkLabel.Link linkToDonate_ = new LinkLabel.Link();
            linkToDonate_.LinkData = "https://www.paypal.me/Yogendrasinh";
            linkToDonate.Links.Add(linkToDonate_);

            LinkLabel.Link linkToDwnload = new LinkLabel.Link();
            linkToDwnload.LinkData = "https://sourceforge.net/projects/icompress/?source=navbar";
            linkToDownload.Links.Add(linkToDwnload);


            LinkLabel.Link linkToRateSf = new LinkLabel.Link();
            linkToRateSf.LinkData = "https://sourceforge.net/projects/icompress/reviews?source=navbar";
            linkToRate.Links.Add(linkToRateSf);

        }

        private void CheckIfNewVersionAvailable()
        {
            try
            {
                WebClient client = new WebClient();

                // Add a user agent header in case the 
                // requested URI contains a query.

                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                Stream data = client.OpenRead("http://sourceforge.net/rest/p/icompress/wiki/Version");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                this.Invoke((MethodInvoker)delegate
                {
                    if (s.Contains("Current Version") && !s.Contains("Current Version is 3.1"))
                    {
                        linkToDownload.Visible = true;
                        linkToDownload.BackColor = Color.Yellow;
                    }
                    else
                    {
                        linkToDownload.Visible = true;
                        linkToDownload.Text = "You're using latest version!";
                    }
                });
            }
            catch
            {
                //ignore
            }
        }

        private bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private void SetToolTips()
        {
            new ToolTip().SetToolTip(rbSizeInPixels, "Fix new size of image.");
            new ToolTip().SetToolTip(numtxtCompress, "In Pixel, Sample inputs: 640, 1028.");
            new ToolTip().SetToolTip(rbJpeg, "JPEG is best compression method for photographs.");
            new ToolTip().SetToolTip(rbPng, "PNG is best for computer generated images. For example, a screenshot.");
            //new ToolTip().SetToolTip(btnPreview, "See if compression parameters that you give suits your need.");
        }

        private bool SetView()
        {
            bool isViewGood = true;
            this.Invoke((MethodInvoker)delegate
            {
                this.InputDirPath = txtOpen.Text;
                this.OutputDirPath = txtSave.Text;
                this.QualityCompression = trkCompress.Value;
                this.FixedHeight = !rbSizePercentage.Checked;
                if (FixedHeight)
                    if (!string.IsNullOrEmpty(numTxtHeight.Text) && numTxtHeight.Text.Trim() != "0")
                    {
                        this.Height = Convert.ToInt32(numTxtHeight.Text);
                    }
                    else
                    {
                        MessageBox.Show(
                                "Size should be non-zero",
                                "Invalid Size Specified",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                        ShowError(numTxtHeight);
                        isViewGood = false;
                    }
                else
                    this.Height = trkSize.Value;

                if (rbPng.Checked)
                    mimeTypeToSave = SupportedMimeType.PNG;
                else if (rbJpeg.Checked)
                    mimeTypeToSave = SupportedMimeType.JPEG;
                else
                    mimeTypeToSave = SupportedMimeType.ORIGINAL;

                try
                {
                    fileSize = decimal.Parse(txtSize.Text.Trim());
                }
                catch
                {
                    fileSize = 0;
                }

            });

            return isViewGood;
        }

        System.Threading.Thread compressWorker;
        DateTime startTime, endTime;
        private void btnCompress_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(txtSave.Text))
                txtSave.Text =
                    txtOpen.Text
                    + System.IO.Path.DirectorySeparatorChar.ToString()
                    + "compressed";
            else if (Helper.AddDirectorySeparatorAtEnd(txtSave.Text).ToUpper()
                        == Helper.AddDirectorySeparatorAtEnd(txtOpen.Text).ToUpper())
            {
                if (MessageBox.Show(
                    "You have given Input and Output as same directory. "
                    + "This will replace existing hight quality images. Do you want to continue?",
                    "Replace Images Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    //Do not proceed
                    return;
            }

            compressWorker = new System.Threading.Thread(CompressWorker);
            compressWorker.IsBackground = false;
            compressWorker.Start();
            progressDlg.Owner = this;
            startTime = DateTime.Now;
            progressDlg.ShowDialog();
        }

        int totalDirectoriesToCompress = 0;
        int totalDirectoryCountCompressing = 0;
        List<string> dirsToCompress;
        private void CompressWorker()
        {
            int compressedCout = 0;

            try
            {
                if (SetView())
                {
                    dirsToCompress = new List<string>();
                    if (chkCompressAll.Checked)
                        dirsToCompress = System.IO.Directory.GetDirectories(InputDirPath, "*", System.IO.SearchOption.AllDirectories).ToList();
                    dirsToCompress.Add(InputDirPath);

                    totalDirectoriesToCompress = dirsToCompress.Count;
                    totalDirectoryCountCompressing = 0;

                    foreach (var dir in dirsToCompress)
                    {
                        totalDirectoryCountCompressing++;
                        compressedCout += this.presenter.CompressDirectory(dir, ProgressUpdate, totalDirectoriesToCompress > 1);
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        progressDlg.Hide();
                        endTime = DateTime.Now;
                    });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressDlg.Hide();
                        endTime = DateTime.Now;
                    });
                    return;
                }

                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(compressedCout.ToString() + " images compressed in " + (int)(endTime - startTime).TotalSeconds + " seconds.", "Result");
                });
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(
                       "Compression Operation Aborted!",
                       "Aborted!",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(
                       ex.Message,
                       "Error",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error);
                });
            }
        }

        private void ProgressUpdate(int progress)
        {
            //progressDlg.Invoke((MethodInvoker)delegate
            //{
            if (this.progressDlg.Visible == true)
            {
                this.progressDlg.UpdateProgressBar(progress, totalDirectoriesToCompress, totalDirectoryCountCompressing, dirsToCompress[totalDirectoryCountCompressing - 1]);
            }
            else
                compressWorker.Abort();
            //});
        }


        private void btnOpen_Click(object sender, EventArgs e)
        {

            if (Directory.Exists(txtOpen.Text))
            {
                folderBrowser.SelectedPath = txtOpen.Text;
            }

            if (folderBrowser.ShowDialog() == DialogResult.OK)
                txtOpen.Text = folderBrowser.SelectedPath;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtSave.Text))
                folderBrowser.SelectedPath = txtSave.Text;
            else if(Directory.Exists(Path.Combine(txtSave.Text, "..")))
                folderBrowser.SelectedPath = Directory.GetParent(txtSave.Text).FullName;

            if (folderBrowser.ShowDialog() == DialogResult.OK)
                txtSave.Text = folderBrowser.SelectedPath;
        }

        private void trkCompress_Scroll(object sender, EventArgs e)
        {
            numtxtCompress.Text = trkCompress.Value.ToString();
            trkCompress.Value = Convert.ToInt32(numtxtCompress.Text);
        }

        private void msktxtCompress_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(numtxtCompress.Text))
                return;
            numtxtCompress.Text =
                Convert.ToInt32(numtxtCompress.Text) > 100 ? "100" : numtxtCompress.Text;
            trkCompress.Value = Convert.ToInt32(numtxtCompress.Text);

            UpdatePreview(null, null);
        }

        List<string> opPreviewImages = new List<string>();


        System.Threading.Thread previewThread;
        private void btnPreview_Click(bool? next, bool forceRefresh = false)
        {

            if (string.IsNullOrEmpty(txtOpen.Text))
                return;

            if (!forceRefresh && !autoUpdate)
                return;

            if (previewThread != null && previewThread.IsAlive)
            {
                try
                {
                    previewThread.Abort();
                    foreach (var process in Process.GetProcessesByName("dcraw"))
                    {
                        process.Kill();
                    }
                }
                catch
                {
                    //ignore. Most probably access right related issue.
                }
            }

            previewThread = new System.Threading.Thread(() => PreviewWorker(next));
            previewThread.Start();
        }

        private void PreviewWorker(bool? next)
        {
            this.Invoke((MethodInvoker)delegate
                 {
                     if (next != null)
                         if (next.Value)
                             btnPrev.Enabled = true;
                         else
                             btnNext.Enabled = true;
                     label3.Text = "Working on generating preview (raw formats take more time)";
                 });

            //zoomLevel = 100;
            if (SetView())
            {
                compressedImgPath = this.presenter.GetPreviewImage(opPreviewImages, next);
            }
            else
            {
                /*MessageBox.Show(
                    "No image found on specified folder.",
                    "Wrong Directory?",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);*/
                this.Invoke((MethodInvoker)delegate
                    {
                        webBroPicView.DocumentText = "";
                    });



                return;
            }

            this.Invoke((MethodInvoker)delegate
                   {
                       if (opPreviewImages.Count == 0)
                       {
                           webBroPicView.DocumentText = "";
                           label3.Text = "";
                           return;
                       }

                       if (string.IsNullOrEmpty(compressedImgPath))
                       {
                           return;
                       }

                       if (compressedImgPath.Equals("NO_PREV"))
                       {
                           btnPrev.Enabled = false;
                           label3.Text = "";
                           return;
                       }
                       if (compressedImgPath.Equals("NO_NEXT"))
                       {
                           btnNext.Enabled = false;
                           label3.Text = "";
                           return;
                       }

                       try
                       {
                           Bitmap img = new Bitmap(compressedImgPath);
                           compressedImgHeight = img.Height;
                           compressedImgWidth = img.Width;
                           string webContent = GetHTMLForImg(compressedImgPath);
                           webBroPicView.DocumentText = webContent;
                           label3.Text = this.Message;
                           img.Dispose();
                       }
                       catch(Exception ex)
                       {
                           //ignore. Happens if you use quality/dim scroll on slow machine.
                           Console.WriteLine(ex.Message);
                       }
                   });
        }



        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomLevel += 10;
            webBroPicView.DocumentText = GetHTMLForImg(compressedImgPath);
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            if (zoomLevel >= 10)
            {
                zoomLevel -= 10;
            }
            webBroPicView.DocumentText = GetHTMLForImg(compressedImgPath);
        }

        private void btnNormalSize_Click(object sender, EventArgs e)
        {
            zoomLevel = 100;
            webBroPicView.DocumentText = GetHTMLForImg(compressedImgPath);
        }

        private void trkSize_Scroll(object sender, EventArgs e)
        {
            numtxtSize.Text = trkSize.Value.ToString();
            trkSize.Value = Convert.ToInt32(numtxtSize.Text);

        }

        private void msktxtSize_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(numtxtSize.Text))
                return;
            numtxtSize.Text =
                Convert.ToInt32(numtxtSize.Text) > 100 ? "100" : numtxtSize.Text;
            trkSize.Value = Convert.ToInt32(numtxtSize.Text);

            UpdatePreview(null,null);
        }

        private string GetHTMLForImg(string imgPath)
        {
            long previewImgWidth = compressedImgWidth * zoomLevel / 100;
            long previewImgHeight = compressedImgHeight * zoomLevel / 100;

            return "<body style=\"margin:0px\"><img src=\"" + compressedImgPath + "\" width=" + previewImgWidth.ToString() + " height=" + previewImgHeight.ToString() + "/></body>";
        }

        private void rbSizePercentage_CheckedChanged(object sender, EventArgs e)
        {
            pnlSizePerc.Enabled = rbSizePercentage.Checked;
            pnlSizePix.Enabled = !rbSizePercentage.Checked;

            UpdatePreview(null, null);
        }

        private void rbSizeInPixels_CheckedChanged(object sender, EventArgs e)
        {
            pnlSizePerc.Enabled = !rbSizeInPixels.Checked;
            pnlSizePix.Enabled = rbSizeInPixels.Checked;

            UpdatePreview(null, null);
        }

        private void linkToBlog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);
        }

        private void chkCompressAll_CheckedChanged(object sender, EventArgs e)
        {
            txtSave.Enabled = !chkCompressAll.Checked;
            btnSave.Enabled = !chkCompressAll.Checked;
            lblTakeBackup.Visible = chkCompressAll.Checked;
            takeBackupBlinkTimer.Enabled = chkCompressAll.Checked;
            if (chkCompressAll.Checked)
                rbKeepOriginal.Checked = true;
            rbJpeg.Enabled = !chkCompressAll.Checked;
            rbPng.Enabled = !chkCompressAll.Checked;

            UpdatePreview(null, null);
        }


        void takeBackupBlinkTimer_Tick(object sender, EventArgs e)
        {
            if (lblTakeBackup.BackColor == Color.Transparent)
                lblTakeBackup.BackColor = Color.Red;
            else
                lblTakeBackup.BackColor = Color.Transparent;
        }

        void ShowError(Control ctrl)
        {
            var highlightThread = new System.Threading.Thread(() =>
            {
                Color origColorOfControl = ctrl.BackColor;
                for (int i = 0; i < 10; i++)
                {
                    HighLightControl(ctrl);
                    System.Threading.Thread.Sleep(100);
                }
                ctrl.BackColor = origColorOfControl;
            });

            highlightThread.IsBackground = true;
            highlightThread.Start();
        }

        void HighLightControl(Control ctrl)
        {
            ctrl.Invoke((MethodInvoker)delegate
                {
                    if (ctrl.BackColor != Color.Red)
                        ctrl.BackColor = Color.Red;
                    else
                        ctrl.BackColor = Color.White;
                });
        }

        private void MainUI_DragEnter(object sender, DragEventArgs e)
        {
            DragDropEffects effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (Directory.Exists(path))
                {
                    effects = DragDropEffects.Copy;
                    txtOpen.Text = path;
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }


        private void chkFileSize_CheckedChanged(object sender, EventArgs e)
        {
            txtSize.Enabled = chkFileSize.Checked;
            cmbKBMB.Enabled = chkFileSize.Checked;
            checkFileSize = chkFileSize.Checked;

            if (chkFileSize.Checked)
                txtSize.Focus();
            else
                fileSize = 0;

            try
            {
                if (string.IsNullOrEmpty(txtSize.Text))
                    fileSize = 0;
                else
                    fileSize = decimal.Parse(txtSize.Text.Trim());
            }
            catch
            {
                fileSize = 0;
                txtSize.Text = "0";
                if (chkFileSize.Checked)
                {
                    MessageBox.Show(
                        "You specified invalid file size. File size set to zero now.",
                        "Invalid File Size",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
            opPreviewImages.Clear();
            btnPreview_Click(true, true);
        }

        private void MainUI_Load(object sender, EventArgs e)
        {
            this.cmbKBMB.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbKBMB.SelectedIndex = 1; //select MB.

            // Update version check is not implemented
            //var versionCheckThread = new System.Threading.Thread(() => CheckIfNewVersionAvailable());
            //versionCheckThread.Start();
        }

        private void txtOpen_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(txtOpen.Text))
            {
                //Start Preview Thread
                opPreviewImages.Clear();
                zoomLevel = 100;
                btnPreview_Click(true);
                txtSave.Text = Path.Combine(txtOpen.Text, "Compressed");
            }
            else
            {
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            btnPreview_Click(true);
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            btnPreview_Click(false);
        }

        private void btnRefreshPreview_Click(object sender, EventArgs e)
        {
            btnPreview_Click(null, true);
        }


        private void txtSize_TextChanged(object sender, EventArgs e)
        {
            opPreviewImages.Clear();
            btnPreview_Click(true, true);
        }

        private void cmbKBMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            opPreviewImages.Clear();
            btnPreview_Click(true, true);
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            btnPreview_Click(null);
        }

        private void chkAutoUpdatePreview_CheckedChanged(object sender, EventArgs e)
        {
            autoUpdate = chkAutoUpdatePreview.Checked;

            lblPreview.Visible = !autoUpdate;
            btnRefreshPreview.Visible = !autoUpdate;
            btnPreview_Click(null);
        }
    }
}