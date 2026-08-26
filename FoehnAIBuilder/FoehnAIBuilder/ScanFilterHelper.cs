using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoehnAIBuilder.Tools.Scan
{
    /// <summary>
    /// Helper class to filter scan results based on ignore rules from appsettings.json.
    /// </summary>
    public static class ScanFilterHelper
    {
        /// <summary>
        /// Filters a list of file paths based on the ignore rules from ScanToolOptions.
        /// </summary>
        /// <param name="filePaths">List of file paths to filter.</param>
        /// <param name="ignoreFiles">List of file names to ignore.</param>
        /// <param name="ignoreExtensions">List of file extensions to ignore.</param>
        /// <param name="ignoreFolders">List of folder names to ignore.</param>
        /// <returns>Filtered list of file paths.</returns>
        public static List<string> FilterScanResults(
            List<string> filePaths,
            List<string> ignoreFiles,
            List<string> ignoreExtensions,
            List<string> ignoreFolders)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return new List<string>();
            }

            var filteredFiles = filePaths.Where(filePath =>
            {
                // Check if the file name is in the ignore list
                var fileName = Path.GetFileName(filePath);
                if (ignoreFiles.Any(ignoreFile => fileName.Equals(ignoreFile, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Check if the file extension is in the ignore list
                var fileExtension = Path.GetExtension(filePath);
                if (ignoreExtensions.Any(ignoreExt => fileExtension.Equals(ignoreExt, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Check if the file is in an ignored folder
                var directoryName = Path.GetDirectoryName(filePath);
                if (ignoreFolders.Any(ignoreFolder => directoryName.Contains(ignoreFolder, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                return true;
            }).ToList();

            return filteredFiles;
        }

        /// <summary>
        /// Filters a list of directory paths based on the ignore rules from ScanToolOptions.
        /// </summary>
        /// <param name="directoryPaths">List of directory paths to filter.</param>
        /// <param name="ignoreFolders">List of folder names to ignore.</param>
        /// <returns>Filtered list of directory paths.</returns>
        public static List<string> FilterDirectoryResults(
            List<string> directoryPaths,
            List<string> ignoreFolders)
        {
            if (directoryPaths == null || directoryPaths.Count == 0)
            {
                return new List<string>();
            }

            var filteredDirectories = directoryPaths.Where(directoryPath =>
            {
                // Check if the directory name is in the ignore list
                var directoryName = Path.GetFileName(directoryPath);
                if (ignoreFolders.Any(ignoreFolder => directoryName.Equals(ignoreFolder, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                return true;
            }).ToList();

            return filteredDirectories;
        }
    }
}