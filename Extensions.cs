namespace DockerLambda
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    public static class Extensions
    {
        public static void UploadFolder(this ZipArchive archive, string sourceDirName, CompressionLevel compressionLevel = CompressionLevel.Fastest)
        {
            var folders = new Stack<string>();

            folders.Push(sourceDirName);

            do
            {
                var currentFolder = folders.Pop();

                foreach (var item in Directory.GetFiles(currentFolder))
                {
                    archive.CreateEntryFromFile(item, item.Substring(sourceDirName.Length + 1), compressionLevel);
                }

                foreach (var item in Directory.GetDirectories(currentFolder))
                {
                    folders.Push(item);
                }
            }
            while (folders.Count > 0);
        }

        public static void SafelyCreateZipFromDirectory(this string sourceDirectoryName, string zipFilePath)
        {
            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(sourceDirectoryName))
                {
                    var entryName = Path.GetFileName(file);
                    var entry = archive.CreateEntry(entryName);
                    entry.LastWriteTime = File.GetLastWriteTime(file);
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var stream = entry.Open())
                    {
                        fs.CopyTo(stream, 81920);
                    }
                }
            }
        }
    }
}
