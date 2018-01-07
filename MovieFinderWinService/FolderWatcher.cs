using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieFinderWinService
{
    /// <summary>
    /// This class watch foders and copy their content in destination folder
    /// </summary>
    class FolderWatcher
    {
        /// <summary>
        /// log source 
        /// </summary>
        private static string logSource = "FolderWatcher";

        /// <summary>
        /// File Extensions on which watch will be applied
        /// </summary>
        private static List<string> filterExtensions = null;

        /// <summary>
        /// Destination folder for copy
        /// </summary>
        private static string destinationFolder = string.Empty;

        /// <summary>
        /// After how much time copy should be try if file is not ready for copy
        /// </summary>
        private static int threadSleepTime;

        /// <summary>
        /// Sync Object
        /// </summary>
        private static object syncLock = new object();

        /// <summary>
        /// Creating instance of this class does following things
        /// - Create filewatcher instance for all the path in sourceFolder list
        /// - Copy the content of source folder to destination folder after applying extension filter
        /// </summary>
        /// <param name="sourceFolders">List of folders on which watch is to be performed</param>
        /// <param name="destFolder">Destination folder for copy</param>
        /// <param name="extenstions">File Extensions on which watch will be applied</param>
        /// <param name="sleepTime">After how much time copy should be try if file is not ready for copy</param>
        public FolderWatcher(List<string> sourceFolders, string destFolder, List<string> extenstions, int sleepTime)
        {
            filterExtensions = extenstions;
            destinationFolder = destFolder;
            threadSleepTime = sleepTime;
            foreach (var folder in sourceFolders)
            {
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
                fileSystemWatcher.Path = folder;
                fileSystemWatcher.IncludeSubdirectories = true;
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess |
                         NotifyFilters.LastWrite |
                         NotifyFilters.FileName |
                         NotifyFilters.DirectoryName;
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Created += new FileSystemEventHandler(OnCreated);
                Logger.Log(logSource, string.Format("OnCreated event registered for {0} path", folder));
            }
        }

        /// <summary>
        /// New file created in source folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            lock (syncLock)
            {
                FileInfo file = new FileInfo(e.FullPath);
                if (FileOfInterest(file))
                {
                    Logger.Log(logSource, string.Format("OnCreated event received for {0} file", file.FullName));
                    Task.Run(() => Copy(file));
                }
            }
        }

        /// <summary>
        /// Copy file to destination folder
        /// </summary>
        /// <param name="file">file which is created in source folder</param>
        private void Copy(FileInfo file)
        {
            while (IsFileLocked(file))
            {
                Task.Delay(threadSleepTime);
            }
            try
            {
                Logger.Log(logSource, "Copy started for " + file.FullName);
                File.Copy(file.FullName, Path.Combine(destinationFolder, file.Name));
                Logger.Log(logSource, "Copy Completed for " + file.FullName);
            }
            catch (Exception ex)
            {
                Logger.Log(logSource, string.Format("Exception occured while copying from source : {0} to destination.{1}Exception : {2}", file.FullName, Environment.NewLine, ex.Message), LogLevel.Error);
            }
        }

        /// <summary>
        /// Verifies if file is ready for copy and all the existing handles for file is closed
        /// </summary>
        /// <param name="file">file on which check is to be performed</param>
        /// <returns>true if file is locked otherwise false</returns>
        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        /// <summary>
        /// Performs extension filter on created files
        /// </summary>
        /// <param name="file">file on which extension filter is to be performed</param>
        /// <returns>true if file need to be copied otherwise false</returns>
        private bool FileOfInterest(FileInfo file)
        {
            bool ret = false;
            if (filterExtensions.Contains(".*") || filterExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                ret = true;
            }
            return ret;
        }
    }
}
