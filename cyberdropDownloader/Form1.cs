﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using Downloader;
using Syroot.Windows.IO;
using System.Diagnostics;
using System.Reflection;

namespace cyberdropDownloader
{
    public partial class Form1 : Form
    {
        private String path;
        private DownloadService downloader = new DownloadService();
        public Point mouseLocation;

        public Form1()
        {
            InitializeComponent();
            this.AcceptButton = downloadBtn;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseLocation = new Point(-e.X, -e.Y);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(mouseLocation.X, mouseLocation.Y);
                Location = mousePos;
            }
        }
  
        private void Form1_Load(object sender, EventArgs e)
        {
            ToolTip toolTip1 = new ToolTip();
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            filepath.Text = KnownFolders.Downloads.Path;
            this.path = filepath.Text;
            this.versionLabel.Text = $"cy client - v{version}";
            downloader.ChunkDownloadProgressChanged += OnChunkDownloadProgressChanged;
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(this.openFolderBtn, "Open destination folder in explorer");
        }

        private void OnChunkDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Invoke(new MethodInvoker(delegate ()
            {
                downloadProgressBar.Minimum = 0;
                double receive = double.Parse(e.BytesReceived.ToString());
                double total = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = receive / total * 100;
                progressLabel.Text = $"Downloading {string.Format("{0:0}", percentage)}%";
                downloadProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
        }

        private void downloadBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(urlBox.Text))
                {
                    throw new System.ArgumentException("URL cannot be null"); 
                }
                var scraper = new CyScraper(urlBox.Text, this.path, this.downloader);
                scraper.StartAsync();
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void filepath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = KnownFolders.Downloads.Path;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filepath.Text = fbd.SelectedPath;
                this.path = filepath.Text;
            }
             
        }

        private void openFolderBtn_Click(object sender, EventArgs e)
        {
            Process.Start(path);
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }

    public class CyScraper
    {
        private string url;
        private string dest;
        private DownloadService downloader;
        public string itemName;
        public CyScraper(String url, String path, DownloadService downloader)
        {
            this.url = url;
            this.dest = path;
            this.downloader = downloader;
        }

        private string Title(HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            var title = htmlDoc.DocumentNode.SelectNodes("//div/h1[@id='title']").First().Attributes["title"].Value;
            return title;
        }

        public async void StartAsync()
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                var htmlDoc = web.Load(url);
                string title = this.Title(htmlDoc);

                foreach (HtmlNode link in htmlDoc.DocumentNode.SelectNodes("//a[@class='image'][@href]"))
                {
                    string url = link.Attributes["href"].Value;
                    itemName = link.Attributes["title"].Value;
                    // download here
                    string filepath = String.Format(@"{0}\{1}\{2}", dest, title, itemName);
                    Console.WriteLine(filepath);
                    await downloader.DownloadFileAsync(url, filepath);
                }
            } catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }
    }


}
