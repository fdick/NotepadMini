using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public int FilesCount { get; private set; }
        public TimeSpan SearchTime { get; private set; }
        public bool IsDone { get; private set; }

        private ManualResetEventSlim _pauseEvent;
        private DateTime _startTime;

        private DateTime _startPauseTime;
        private TimeSpan _pauseTime;

        public async Task SearchFilesAsync(string directory, string pattern, CancellationToken token)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    OnDoneSearching?.Invoke(SearchTime);
                    return;
                }

                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                _startTime = DateTime.Now;
                _pauseTime = default;
                _startPauseTime = default;
                ProcessedFiles = 0;
                SearchTime = default;
                FilesCount = 0;
                IsDone = false;


                FilesCount = await GetCountFilesInDirectory(directory, regex, token);

                await Task.Run(() => SearchDirectory(directory, regex, token), token);

                SearchTime = DateTime.Now - _startTime - _pauseTime;
                OnDoneSearching?.Invoke(SearchTime);
                IsDone = true;
            }
            catch (OperationCanceledException)
            {
                OnCanceled?.Invoke();
            }
            //catch
            //{
            //    OnDoneSearching?.Invoke(SearchTime);
            //}
        }

        public void SetPause(bool pause = true)
        {
            if (pause)
            {
                _pauseEvent.Reset();
                _startPauseTime = DateTime.Now;
            }
            else
            {
                _pauseEvent.Set();
                _pauseTime += DateTime.Now - _startPauseTime;
            }
        }

        private void SearchDirectory(string directory, Regex regex, CancellationToken token)
        {
            _pauseEvent.Wait();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            try
            {

                var filesEnum = Directory.EnumerateFiles(directory);

                foreach (var file in filesEnum)
                {
                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);

                    _pauseEvent.Wait();


                    SearchTime = DateTime.Now - _startTime - _pauseTime;

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
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }


        async Task<int> GetCountFilesInDirectory(string directory, Regex regex, CancellationToken token)
        {

            _pauseEvent.Wait();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            try
            {
                int fileCount = 0;
                var dirEnum = Directory.EnumerateFiles(directory);

                foreach (var d in dirEnum)
                {
                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);

                    _pauseEvent.Wait();

                    SearchTime = DateTime.Now - _startTime - _pauseTime;

                    if (regex.IsMatch(Path.GetFileName(d)))
                    {
                        fileCount++;
                    }

                }

                var subdirectoryTasks = Directory.EnumerateDirectories(directory)
                                                 .Select((subdir, index) => Task.Run(() => GetCountFilesInDirectory(subdir, regex, token), token));



                var subsCount = await Task.WhenAll(subdirectoryTasks);

                //OnSearch?.Invoke();

                return fileCount + subsCount.Sum();
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}
