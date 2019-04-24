using Quartz;
using System.Collections.Generic;
using System.IO;

namespace DeleteTempFiles.WindowsService.Services.Quartz
{
    public interface ICleanTempFolderJob : IJob
    {
        void DeleteFiles(List<string> tempFilesToDelete);

        void DeleteTempFiles(int daysAgo = 7, string filePattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories);

        List<string> GetTempFiles(string filePattern, SearchOption searchOption);
    }
}