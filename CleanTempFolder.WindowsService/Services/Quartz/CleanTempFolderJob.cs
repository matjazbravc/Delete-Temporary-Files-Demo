using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeleteTempFiles.WindowsService.Services.Quartz
{
    public class CleanTempFolderJob : ICleanTempFolderJob
    {
        public void DeleteFiles(List<string> tempFilesToDelete)
        {
            foreach (var tempFile in tempFilesToDelete)
            {
                try
                {
                    File.Delete(tempFile);
                    Log.Information($"[{DateTime.UtcNow}] File {tempFile} deleted.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[{DateTime.UtcNow}] Deleting {tempFile} FAILED. {ex.Message}");
                }
            }
        }

        public void DeleteTempFiles(int daysAgo = 7, string filePattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            List<string> files = GetTempFiles(filePattern, searchOption);
            if (files.Count > 0)
            {
                List<string> tempFilesToDelete = files.Where(c =>
                {
                    TimeSpan ts = DateTime.Now - File.GetLastAccessTime(c);
                    return ts.Days > daysAgo;
                }).ToList();
                DeleteFiles(tempFilesToDelete);
            }
        }

        public void Execute(IJobExecutionContext context)
        {
            Log.Information("Executing DeleteTempFiles job");
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            int daysAgo = dataMap.GetInt("DaysAgo");
            DeleteTempFiles(daysAgo);
            string tempPath = Path.GetTempPath();
            DeleteEmptyDirs(tempPath);
        }

        public List<string> GetTempFiles(string filePattern, SearchOption searchOption)
        {
            string tempPath = Path.GetTempPath();
            if (Directory.Exists(tempPath))
            {
                return Directory.GetFiles(tempPath, filePattern, searchOption).ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        private void DeleteEmptyDirs(string startDir)
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(startDir))
                {
                    DeleteEmptyDirs(dir);
                }
                var entries = Directory.EnumerateFileSystemEntries(startDir);
                if (!entries.Any())
                {
                    try
                    {
                        Directory.Delete(startDir);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }
}
