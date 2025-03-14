﻿using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            DoubleBuffer.EnableDoubleBuffering(treeView1);

            _fileSearcher = new FileSearcher();

            //disable pause btn at start
            pauseBtn.Visible = false;


            //load settings
            LoadSettings();
            _fileSearcher.OnSearchedFile += (filePath) =>
            {
                Invoke(new Action(() =>
                {
                    AddFileToTree(filePath);
                }));
            };

            _fileSearcher.OnDoneSearching += (timePast) =>
            {
                Invoke(new Action(() =>
                {
                    OnDoneSearching();
                }));


            };

            _fileSearcher.OnCanceled += () =>
            {

                Invoke(new Action(() =>
                {
                    statusLabel.Text = CANCELED;
                    OnDoneSearching();
                }));


            };


            //timer
            timer1.Tick += (s, e) =>
            {
                if (!_fileSearcher.IsDone)
                    statusLabel.Text = $"{PROCESSING}: {_fileSearcher.ProcessedFiles} / {_fileSearcher.FilesCount} ( {_fileSearcher.SearchTime.Seconds}.{_fileSearcher.SearchTime.Milliseconds}  sec )";
                else
                    statusLabel.Text = $"{DONE}: {_fileSearcher.ProcessedFiles} / {_fileSearcher.FilesCount} ( {_fileSearcher.SearchTime.Seconds}.{_fileSearcher.SearchTime.Milliseconds}  sec )";

            };

            timer1.Start();

        }


        private FileSearcher _fileSearcher;
        private bool _isSearching = false;
        private bool _isPaused = false;
        private CancellationTokenSource _tokenSource;

        private const string DONE = "Done";
        private const string PROCESSING = "Processing";
        private const string CANCELED = "Canceled";
        private const string STOP = "Stop";
        private const string SEARCH = "Search";
        private const string PAUSE = "Pause";
        private const string UNPAUSE = "Unpause";


        private async void button1_Click(object sender, EventArgs e)
        {
            if (!_isSearching)
            {
                treeView1.Nodes.Clear();
                //SaveSettings();
                _isSearching = true;
                _tokenSource = new CancellationTokenSource();

                //change name
                button1.Text = STOP;
                button1.BackColor = Color.Orange;

                //enable pause btn
                pauseBtn.Visible = true;

                //save settings
                SaveSettings();

                statusLabel.Text = $"{PROCESSING}";

                await _fileSearcher.SearchFilesAsync(dirLabel.Text, patternLabel.Text, _tokenSource.Token);

            }
            else
            {
                if (_isPaused)
                    SetPause(false);

                _tokenSource?.Cancel();
                _tokenSource = null;

                pauseBtn.Visible = false;
                _isSearching = false;

            }
        }

        private void pauseBtn_Click(object sender, EventArgs e)
        {
            SetPause(!_isPaused);
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a directory";
                dialog.SelectedPath = dirLabel.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    dirLabel.Text = dialog.SelectedPath;
                }
            }
        }

        private void SetPause(bool pause)
        {
            _fileSearcher.SetPause(pause);

            if (pause)
                pauseBtn.Text = UNPAUSE;
            else
                pauseBtn.Text = PAUSE;

            _isPaused = pause;
        }

        private void AddFileToTree(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            TreeNode parentNode = GetOrCreateNode(directory);
            parentNode.Nodes.Add(filePath, Path.GetFileName(filePath));
        }

        private TreeNode GetOrCreateNode(string directory)
        {
            string[] parts = directory.Split(Path.DirectorySeparatorChar);
            TreeNodeCollection nodes = treeView1.Nodes;
            TreeNode parentNode = null;

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                TreeNode foundNode = nodes[part];
                if (foundNode == null)
                {
                    foundNode = nodes.Add(part, part);
                }
                parentNode = foundNode;
                nodes = foundNode.Nodes;
            }
            return parentNode;
        }

        private void OnDoneSearching()
        {
            button1.Text = SEARCH;
            button1.BackColor = Color.White;
            _isSearching = false;
            pauseBtn.Visible = false;
        }


        //В идеале написать отдельный сервис по сохранению в файл, но для простоты кода юзаю этот метод
        private void LoadSettings()
        {
            dirLabel.Text = Properties.Settings.Default.startDir;
            patternLabel.Text = Properties.Settings.Default.startPattern;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.startDir = dirLabel.Text;
            Properties.Settings.Default.startPattern = patternLabel.Text;
            Properties.Settings.Default.Save();
        }


    }
}
