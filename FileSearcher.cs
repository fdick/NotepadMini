using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class FileSearcher : IDisposable
    {

        public FileSearcher()
        {
            _pauseEvent = new ManualResetEventSlim(true);
        }

        //~FileSearcher()
        //{
        //    OnSearch = null;
        //    OnSearchedFile = null;
        //    OnDoneSearching = null;
        //}

        public void Dispose()
        {
            OnSearch = null;
            OnSearchedFile = null;
            OnDoneSearching = null;
            OnCanceled = null;
            _pauseEvent = null;
        }

        public Action OnSearch { get; set; }
        public Action<string> OnSearchedFile { get; set; }
        public Action<TimeSpan> OnDoneSearching { get; set; }
        public Action OnCanceled { get; set; }

        public int ProcessedFiles { get; private set; }
        public object FilesCount { get; private set; }
        public TimeSpan SearchTime { get; private set; }
        
        
        private ManualResetEventSlim _pauseEvent;
        private DateTime _startTime;

        public async Task SearchFilesAsync(string directory, string pattern, CancellationToken token)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    OnDoneSearching?.Invoke(SearchTime);
                    return;
                }

                var regex = new Regex(pattern/*, RegexOptions.IgnoreCase*/);

                FilesCount = GetCountFilesInDirectory(directory, regex);
                _startTime = DateTime.Now;
                ProcessedFiles = 0;
                SearchTime = default;



                await Task.Run(() => SearchDirectory(directory, regex, token), token);

                SearchTime = DateTime.Now - _startTime;
                OnDoneSearching?.Invoke(SearchTime);
            }
            catch (OperationCanceledException)
            {
                OnCanceled?.Invoke();
            }
        }

        public void SetPause(bool pause = true)
        {
            if (pause)
                _pauseEvent.Reset();
            else
                _pauseEvent.Set();
        }

        private void SearchDirectory(string directory, Regex regex, CancellationToken token)
        {
            _pauseEvent.Wait();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            int countFiles = GetCountFilesInDirectory(directory, regex);
            var files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                _pauseEvent.Wait();

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);


                SearchTime = DateTime.Now - _startTime;

                OnSearch?.Invoke();


                if (regex.IsMatch(Path.GetFileName(file)))
                {
                    ProcessedFiles++;
                    OnSearchedFile?.Invoke(file);
                }
            }
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                SearchDirectory(subDir, regex, token);
            }

        }

        private int GetCountFilesInDirectory(string directory, Regex regex)
        {
            int count = 0;
            var files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                if (regex.IsMatch(Path.GetFileName(file)))
                {
                    count++;
                }
            }


            foreach (var subDir in Directory.GetDirectories(directory))
            {
                count += GetCountFilesInDirectory(subDir, regex);
            }

            return count;
        }
    }
}
