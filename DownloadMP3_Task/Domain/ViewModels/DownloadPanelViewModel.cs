using DownloadMP3_Task.Commands;
using DownloadMP3_Task.Views;
using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using VideoLibrary;

namespace DownloadMP3_Task.ViewModels
{
    public class DownloadPanelViewModel : BaseViewModel
    {
        public Thread DownloadingThread { get; set; }

        private ManualResetEvent _workerEvent = new ManualResetEvent(false);
        public RelayCommand WaitCommand { get; set; }

        public RelayCommand StartDownloaderCommand { get; set; }

        private string linkText;

        public string URLToDownload
        {
            get { return linkText; }
            set { linkText = value; OnPropertyChanged(); }
        }

        private YouTubeVideo video;

        public YouTubeVideo Video
        {
            get { return video; }
            set { video = value; OnPropertyChanged(); }
        }

        private int duration;

        public int Duration
        {
            get { return duration; }
            set { duration = value; OnPropertyChanged(); }
        }

        DispatcherTimer dispatcherTimer;

        public RelayCommand ResumeDownloaderCommand { get; set; }

        private int progressValue;

        public int ProgressValue
        {
            get { return progressValue; }
            set { progressValue = value; OnPropertyChanged(); }
        }

        private int minProgressValue;

        public int MinProgressValue
        {
            get { return minProgressValue; }
            set { minProgressValue = value; OnPropertyChanged(); }
        }

        private long? maxProgressValue;

        public long? MaxProgressValue
        {
            get { return maxProgressValue; }
            set { maxProgressValue = value; OnPropertyChanged(); }
        }

        public long? ContentLength { get; set; }

        public bool IsWaited { get; set; }

        public bool IsStarted { get; set; }

        public DownloadPanelViewModel()
        {
            MinProgressValue = 0;
            MaxProgressValue = 100;

            dispatcherTimer = new DispatcherTimer();

            StartDownloaderCommand = new RelayCommand(async (c) =>
            {
                try
                {
                    IsStarted = true;
                    var youtube = YouTube.Default;
                    Video = await youtube.GetVideoAsync(linkText);
                    WebRequest w_request = WebRequest.Create(Video.Uri);
                    WebResponse webResponse = await w_request.GetResponseAsync();
                    ContentLength = webResponse.ContentLength;
                    Stream stream = webResponse.GetResponseStream();
                    if (Video != null)
                    {

                        DownloadingThread = new Thread(() => DownloadMusic(Video, ContentLength, stream));
                        DownloadingThread.Start();

                        dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                        dispatcherTimer.Tick += DispatcherTimer_Tick;
                        dispatcherTimer.Start();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Link is not correct");
                    URLToDownload = null;

                }
            }, (a) =>
            {
                if (linkText != null && !IsStarted)
                {
                    return true;
                }
                return false;
            });
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Duration += 1;
        }

        public void DownloadMusic(YouTubeVideo video, long? length, Stream stream)
        {
            byte[] buffer = new byte[256 * 1024];
            long? kbLength = length / 1024;
            long? mbSize = kbLength / 1024;
            MaxProgressValue = length;

            try
            {
                var youtube = YouTube.Default;

                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var source = $@"{desktopPath}\";

                using (var ms = new FileStream(source + video.FullName, FileMode.Create))
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {

                        ProgressValue += read / 10;
                        ms.Write(buffer, 0, read);
                    }
                }

                var inputFile = new MediaFile { Filename = source + video.FullName };
                var outputFile = new MediaFile { Filename = $"{source + video.FullName}.mp3" };

                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                    engine.Convert(inputFile, outputFile);

                    dispatcherTimer.Stop();
                    MessageBox.Show("Downloaded Successfully");

                    File.Delete(source + video.FullName);

                    Video = null;
                    URLToDownload = null;
                    ProgressValue = 0;
                    Duration = 0;
                    IsWaited = false;
                    IsStarted = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}