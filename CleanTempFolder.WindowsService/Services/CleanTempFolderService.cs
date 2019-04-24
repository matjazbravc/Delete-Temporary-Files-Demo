using Topshelf;
using Topshelf.Logging;

namespace DeleteTempFiles.WindowsService.Services
{
    public class CleanTempFolderService : ServiceControl
    {
        private static readonly LogWriter _log = HostLogger.Get<CleanTempFolderService>();

        public bool Start(HostControl hostControl)
        {
            _log.Info("Service Started");
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _log.Info("Service Stopped");
            return true;
        }

        public bool Pause(HostControl hostControl)
        {
            _log.Info("Service Paused");
            return true;
        }

        public bool Continue(HostControl hostControl)
        {
            _log.Info("Service Continued");
            return true;
        }
    }
}
