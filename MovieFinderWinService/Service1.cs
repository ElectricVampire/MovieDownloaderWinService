using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace MovieFinderWinService
{
    public partial class MovieDownloaderService : ServiceBase
    {
        /// <summary>
        /// Log Source
        /// </summary>
        private static readonly string logSource = "MovieDownloaderService";

        /// <summary>
        /// Folder where created file from source folder need to be copied
        /// </summary>
        private string destinationFolder = string.Empty;

        /// <summary>
        /// Folder from where created file need to be copied in dest
        /// </summary>
        private List<string> sourceFolders = null;

        /// <summary>
        /// Type of created files which matters for us
        /// </summary>
        private List<string> extensionFilters = null;

        /// <summary>
        /// File is found in use while trying to copy. We will wait till threadSleepTime before next copy try of same file
        /// </summary>
        private int threadSleepTime;

        /// <summary>
        /// Constructor
        /// </summary>
        public MovieDownloaderService()
        {
            InitializeComponent();
            Logger.LogFilePath = ConfigurationManager.AppSettings["LogFilePath"];            
        }

        /// <summary>
        /// Service started
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            Logger.Log(logSource, "Service started");
            try
            {
                if (ReadConfig())
                {
                    FolderWatcher folderWatcher = new FolderWatcher(sourceFolders, destinationFolder, extensionFilters, threadSleepTime);
                }
                else
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(logSource, "Exception :" + ex.Message, LogLevel.Error);
                Stop();
            }
        }

        private bool ReadConfig()
        {
            bool success = true;

            // Read dest folder path
            destinationFolder = ConfigurationManager.AppSettings["DestinationFolder"];
            if (string.IsNullOrEmpty(destinationFolder))
            {
                Logger.Log(logSource, "Unable to read destination folder path", LogLevel.Error);
                success = false;
            }
            else
            {
                Logger.Log(logSource, "Destination Folder :" + destinationFolder);
            }

            // Read source folder paths
            sourceFolders = ConfigurationManager.AppSettings["SourceFolders"].Split(',').Select(s => s.Trim()).ToList();
            StringBuilder sbFolders = new StringBuilder("Source Folders: ");
            if (sourceFolders != null && sourceFolders.Count != 0)
            {
                foreach (var folder in sourceFolders)
                {
                    sbFolders.Append(folder + Environment.NewLine);
                }
                Logger.Log(logSource, sbFolders.ToString());
            }
            else
            {
                Logger.Log(logSource, "Unable to read source folder path", LogLevel.Error);
                success = false;
            }

            // Read filter extensions
            extensionFilters = ConfigurationManager.AppSettings["ExtensionFilters"].Split(',').Select(s => s.Trim()).ToList();
            StringBuilder sbExtensionFilters = new StringBuilder("Extensions: ");
            if (extensionFilters != null && extensionFilters.Count != 0)
            {
                foreach (var extension in extensionFilters)
                {
                    sbExtensionFilters.Append(extension + ",");
                }
                Logger.Log(logSource, sbExtensionFilters.ToString());
            }
            else
            {
                // Its fine, we will consider all the file as file of our interest.
                Logger.Log(logSource, "Unable to read extensions", LogLevel.Error);
            }

            // Thread Sleep timer
            if (Int32.TryParse(ConfigurationManager.AppSettings["ThreadSleepTime"], out threadSleepTime))
            {
                Logger.Log(logSource, "ThreadSleepTime :" + threadSleepTime);
            }
            else
            {
                Logger.Log(logSource, "Unable to read ThreadSleepTime", LogLevel.Error);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Service stopped
        /// </summary>
        protected override void OnStop()
        {
            Logger.Log(logSource, "Service Stopped", LogLevel.Information);
        }
        /// <summary>
        /// Added to debug the service as console app
        /// </summary>
        /// <param name="args"></param>
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
    }
}
