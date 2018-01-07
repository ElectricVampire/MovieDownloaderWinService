using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MovieFinderWinService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                MovieDownloaderService mds = new MovieDownloaderService();
                mds.TestStartupAndStop(null);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new MovieDownloaderService()
                };
                ServiceBase.Run(ServicesToRun);
            }
           
        }
    }
}
