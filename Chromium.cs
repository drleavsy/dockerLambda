namespace DockerLambda
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using ICSharpCode.SharpZipLib.Tar;
    using ICSharpCode.SharpZipLib.Zip;
    using PuppeteerSharp;

    public static class Chromium
    {
        public static string[] Args => GetArguments();

        private static string[] GetArguments()
        {
            var arguments = new List<string>
            {
                "--autoplay-policy=user-gesture-required",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-breakpad",
                "--disable-client-side-phishing-detection",
                "--disable-component-update",
                "--disable-default-apps",
                "--disable-dev-shm-usage",
                "--disable-domain-reliability",
                "--disable-extensions",
                "--disable-features=AudioServiceOutOfProcess",
                "--disable-hang-monitor",
                "--disable-ipc-flooding-protection",
                "--disable-notifications",
                "--disable-offer-store-unmasked-wallet-cards",
                "--disable-popup-blocking",
                "--disable-print-preview",
                "--disable-prompt-on-repost",
                "--disable-renderer-backgrounding",
                "--disable-setuid-sandbox",
                "--disable-speech-api",
                "--disable-sync",
                "--disk-cache-size=33554432",
                "--hide-scrollbars",
                "--ignore-gpu-blocklist",
                "--metrics-recording-only",
                "--mute-audio",
                "--no-default-browser-check",
                "--no-first-run",
                "--no-pings",
                "--no-sandbox",
                "--no-zygote",
                "--password-store=basic",
                "--use-gl=swiftshader",
                "--use-mock-keychain"
            };

            if (Chromium.GetHeadless())
            {
                arguments.Add("--single-process");
            }
            else
            {
                arguments.Add("--start-maximized");
            }

            return arguments.ToArray();
        }

        static bool GetHeadless()
        {
            string isLocalEnvVar = Environment.GetEnvironmentVariable("IS_LOCAL", target: EnvironmentVariableTarget.Process);
            string offLineEnvVar = Environment.GetEnvironmentVariable("IS_OFFLINE", target: EnvironmentVariableTarget.Process);

            if (isLocalEnvVar != null || offLineEnvVar != null)
            {
                return false;
            }

            IReadOnlyList<string> cloudEnvVars = new string[]
            {
                "AWS_LAMBDA_FUNCTION_NAME",
                "FUNCTION_NAME",
                "FUNCTION_TARGET",
                "FUNCTIONS_EMULATOR"
            };

            return cloudEnvVars.Any(key => Environment.GetEnvironmentVariable(key, target: EnvironmentVariableTarget.Process) != null);
        }

        public static ViewPortOptions GetDefaultViewport()
        {
            var browserOptions = new ViewPortOptions
            {
                DeviceScaleFactor = 1,
                HasTouch = false,
                Height = Chromium.GetHeadless() == true ? 1080 : 0,
                IsLandscape = true,
                IsMobile = true,
                Width = Chromium.GetHeadless() == true ? 1920 : 0,
            };

            return browserOptions;
        }

        public static string GetExecutablePath()
        {
            if (Chromium.GetHeadless() != true)
            {
                return null;
            }

            if (File.Exists("/tmp/chromium"))
            {
                foreach (var file in Directory.GetFiles("/tmp"))
                {
                    var onlyFileName = Path.GetFileName(file);

                    if (onlyFileName.StartsWith("core.chromium").Equals(true))
                    {
                        File.Delete($"/tmp/{onlyFileName}");
                    }
                }

                return "/tmp/chromium";
            }

            string binFolder = "/opt/nodejs/node_modules/chrome-aws-lambda/bin";
            // === /opt/nodejs/node_modules/chrome-aws-lambda/bin/chromium.br === //
            var chromiumArchive = "chromium.br";
            var chromiumFile = new FileInfo($"{Path.Combine(binFolder, chromiumArchive)}");

            var chromiumExecPath = DecompressBrotli(chromiumFile.FullName, "/tmp"); // inflate(`${input}/chromium.br`)

            RunBashCommands("/bin/bash", "-c \"chmod 777 /tmp/chromium\"");

            string awsLambdaNodejsEnvVar = Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV", target: EnvironmentVariableTarget.Process);

            //if (awsLambdaNodejsEnvVar != null && awsLambdaNodejsEnvVar.Contains("AWS_Lambda_nodejs"))
            //{
                // === /opt/nodejs/node_modules/chrome-aws-lambda/bin/aws.tar.br === //
                var libAwsArchive = "aws.tar.br";
                var libAwsArchivePath = new FileInfo($"{Path.Combine(binFolder, libAwsArchive)}");

                Chromium.DecompressBrotli(libAwsArchivePath.FullName, "/tmp");

                Chromium.DecompressTar("/tmp/aws.tar", "/tmp/aws");

                RunBashCommands("/bin/bash", "-c \"chmod -R 777 /tmp/aws\"");
            //}

            // === /opt/nodejs/node_modules/chrome-aws-lambda/bin/swiftshader.tar.br === //
            var swiftArchive = "swiftshader.tar.br";
            var swiftArchivePath = new FileInfo($"{Path.Combine(binFolder, swiftArchive)}");

            Chromium.DecompressBrotli(swiftArchivePath.FullName, "/tmp");

            Chromium.DecompressTar("/tmp/swiftshader.tar", "/tmp/swiftshader");

            RunBashCommands("/bin/bash", "-c \"chmod -R 777 /tmp/swiftshader\"");

            return chromiumExecPath;
        }

        public static void CleanTmpOnLambda(string directoryPath)
        {
            DirectoryInfo di = new DirectoryInfo(directoryPath);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        public static string DecompressBrotli(string archiveFilePath, string outputDirectoryName)
        {
            if (!Directory.Exists(outputDirectoryName))
            {
                throw new DirectoryNotFoundException($"The directory with the name {outputDirectoryName} was not found!");
            }

            FileInfo fileToDecompress = new FileInfo($"{archiveFilePath}");

            string fullPathTrimed = Path.GetFullPath(fileToDecompress.FullName).TrimEnd(Path.DirectorySeparatorChar);
            string onlyFileName = Path.GetFileName(fullPathTrimed);

            var onlyFileNameWithoutExtension = onlyFileName.Remove(onlyFileName.Length - fileToDecompress.Extension.Length);
            //var newFileName = Path.Combine("/tmp", onlyFileNameWithoutExtension);
            var newFileName = Path.Combine(outputDirectoryName, onlyFileNameWithoutExtension);

            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                using (FileStream outputFileStream = File.Create(newFileName))
                {
                    using (var brotliStream = new BrotliStream(originalFileStream, CompressionMode.Decompress))
                    {
                        brotliStream.CopyTo(outputFileStream);
                    }
                }
            }

            return newFileName;
        }

        public static void DecompressTar(string archiveFilePath, string outputDirectoryName, bool keepOldFiles = false)
        {
            FileInfo fileToDecompress = new FileInfo($"{archiveFilePath}");

            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                using (var tarInputStream = new TarInputStream(originalFileStream, Encoding.Default))
                {
                    TarEntry entry;
                    while ((entry = tarInputStream.GetNextEntry()) != null)
                    {
                        if (entry.TarHeader.TypeFlag == TarHeader.LF_LINK || entry.TarHeader.TypeFlag == TarHeader.LF_SYMLINK)
                        {
                            continue;
                        }

                        string name = entry.Name;

                        if (Path.IsPathRooted(name))
                        {
                            // for UNC names...  \\machine\share\zoom\beet.txt gives \zoom\beet.txt
                            name = name.Substring(Path.GetPathRoot(name).Length);
                        }

                        name = name.Replace('/', Path.DirectorySeparatorChar);

                        string destFile = Path.Combine(outputDirectoryName, name);

                        if (entry.IsDirectory)
                        {
                            EnsureDirectoryExists(destFile);
                        }
                        else
                        {
                            string parentDirectory = Path.GetDirectoryName(destFile);
                            EnsureDirectoryExists(parentDirectory);

                            bool process = true;
                            var fileInfo = new FileInfo(destFile);

                            if (fileInfo.Exists)
                            {
                                if (keepOldFiles)
                                {
                                    Console.Out.WriteLine($"Destination file already exists: {entry}");
                                    process = false;
                                }
                                else if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
                                {
                                    Console.Out.WriteLine($"Destination file already exists, and is read-only: {entry}");
                                    process = false;
                                }
                            }

                            if (process)
                            {
                                using (var outputStream = File.Create(destFile))
                                {
                                    // If translation is disabled, just copy the entry across directly.
                                    tarInputStream.CopyEntryContents(outputStream);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static string UnZip(string archiveFileFullPath, string destinationFolder)
        {
            string fileName = Path.GetFileName(archiveFileFullPath);

            if (fileName != null)
            {
                File.Copy(archiveFileFullPath, destinationFolder);
                return destinationFolder;
            }

            using (ZipArchive zipArchive = System.IO.Compression.ZipFile.Open(archiveFileFullPath, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    entry.ExtractToFile(destinationFolder, true);
                }
            }

            return destinationFolder;
        }

        public static string RunBashCommands(string fileName, string command)
        {
            string result = string.Empty;

            var process = new Process();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = $"{fileName}",
                Arguments = $"{command}",
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Verb = "runas"
            };

            process.StartInfo = processStartInfo;

            process.Start();
            // process.WaitForExit();

            result = result + $"Test result: {process.StandardOutput.ReadToEnd()}{Environment.NewLine}Test errors: {process.StandardError.ReadToEnd()}";

            process.WaitForExit();
            process.Dispose();

            return result;
        }

        public static void RunCommands(string command)
        {
            string result = string.Empty;

            var process = new Process();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Git\git-bash.exe",
                WorkingDirectory = @"C:\Users\Dmytro_Stoliar",
                Arguments = $"{command}",
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Verb = "runas"
            };

            process.StartInfo = processStartInfo;

            process.Start();
            process.WaitForExit();

            process.Dispose();
        }

        public static void UnZipFile(string fileNameFullPath)
        {
            Console.Out.WriteLine($"Name of ZIP: {fileNameFullPath}");

            using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(fileNameFullPath)))
            {
                ZipEntry theEntry;
                // zipInputStream.Position = 0;
                while ((theEntry = zipInputStream.GetNextEntry()) != null)
                {
                    Console.WriteLine(theEntry.Name);

                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);

                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    if (fileName != String.Empty)
                    {
                        using (FileStream streamWriter = File.Create(theEntry.Name))
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];

                            while (true)
                            {
                                size = zipInputStream.Read(data, 0, data.Length);

                                if (size > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ExtractTar(string filename, string outputDir)
        {
            using (var stream = File.OpenRead(filename))
            {
                var buffer = new byte[100];
                // store current position here
                long pos = 0;
                while (true)
                {
                    pos += stream.Read(buffer, 0, 100);
                    var name = Encoding.ASCII.GetString(buffer).Trim('\0');

                    if (String.IsNullOrWhiteSpace(name))
                        break;
                    FakeSeekForward(stream, 24);
                    pos += 24;

                    pos += stream.Read(buffer, 0, 12);
                    var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
                    FakeSeekForward(stream, 376);
                    pos += 376;

                    var output = Path.Combine(outputDir, name);
                    if (!Directory.Exists(Path.GetDirectoryName(output)))
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                    if (!name.Equals("./", StringComparison.InvariantCulture))
                    {
                        using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            var buf = new byte[size];
                            pos += stream.Read(buf, 0, buf.Length);
                            str.Write(buf, 0, buf.Length);
                        }
                    }

                    var offset = (int)(512 - (pos % 512));
                    if (offset == 512)
                        offset = 0;
                    FakeSeekForward(stream, offset);
                    pos += offset;
                }
            }
        }

        private static void FakeSeekForward(Stream stream, int offset)
        {
            if (stream.CanSeek)
                stream.Seek(offset, SeekOrigin.Current);
            else
            {
                int bytesRead = 0;
                var buffer = new byte[offset];
                while (bytesRead < offset)
                {
                    int read = stream.Read(buffer, bytesRead, offset - bytesRead);
                    if (read == 0)
                        throw new EndOfStreamException();
                    bytesRead += read;
                }
            }
        }

        private static void EnsureDirectoryExists(string directoryName)
        {
            if (!Directory.Exists(directoryName))
            {
                try
                {
                    Directory.CreateDirectory(directoryName);
                }
                catch (Exception e)
                {
                    throw new TarException("Exception creating directory '" + directoryName + "', " + e.Message, e);
                }
            }
        }

        public static IEnumerable<FileSystemInfo> AllFilesAndFolders(this DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                yield return file;
            }

            foreach (var directory in dir.GetDirectories())
            {
                yield return directory;

                foreach (var obj in AllFilesAndFolders(directory))
                {
                    yield return obj;
                }
            }
        }
    }
}
