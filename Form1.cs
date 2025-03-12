using System;
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
                    statusLabel.Text = $"{PROCESSING}: {_fileSearcher.ProcessedFiles} / {_fileSearcher.FilesCount} ( {_fileSearcher.SearchTime.Seconds}.{_fileSearcher.SearchTime.Milliseconds}  sec )";
                }));
            };

            _fileSearcher.OnDoneSearching += (timePast) =>
            {
                Invoke(new Action(() =>
                {
                    statusLabel.Text = $"{DONE}: {_fileSearcher.ProcessedFiles} / {_fileSearcher.FilesCount} ( {_fileSearcher.SearchTime.Seconds}.{_fileSearcher.SearchTime.Milliseconds}  sec )";
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

                await _fileSearcher.SearchFilesAsync(dirLabel.Text, patternLabel.Text, _tokenSource.Token);

            }
            else
            {
                _tokenSource?.Cancel();
                _tokenSource = null;

                pauseBtn.Visible = false;
                _isSearching = false;

            }
        }

        private void pauseBtn_Click(object sender, EventArgs e)
        {
            if (_isPaused)
            {
                _fileSearcher.SetPause(false);
                pauseBtn.Text = PAUSE;
            }
            else
            {
                _fileSearcher.SetPause();
                pauseBtn.Text = UNPAUSE;
            }

            _isPaused = !_isPaused;

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
