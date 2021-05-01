using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DockerLambda
{
    using Amazon.Lambda.APIGatewayEvents;
    using PuppeteerSharp;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.InteropServices;

    public class Functions
    {
        public string FunctionResult { get; set; }

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            bool isLinux = false;
            var title = string.Empty;
            RevisionInfo revisionInfo = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                isLinux = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                isLinux = false;
            }

            //Chromium.CleanTmpOnLambda("/tmp"/*directory*/);

            if (isLinux)
            {
                //Chromium.RunBashCommands("/bin/bash", $"-c \"export CHROME_BIN=/usr/bin/chromium-browser\"");
                //Chromium.RunBashCommands("/bin/bash", $"-c \"chmod -R 777 {directory}\"");
            }

            string directory = "/tmp";
            FunctionResult = $"Running 'ls' on '/tmp' folder: {Chromium.RunBashCommands("/bin/bash", $"-c \"ls -ahl /tmp\"")}";

            Console.Out.WriteLine($"Attempting to set up puppeteer to use Chromium found under directory {directory} ");
            Console.Out.WriteLine("Unzipping Chromium to '/tmp' folder in Lambda");
            // LambdaLogger.Log("Lambda logging: test test test");

            //var chromeSource = "/tmp/chrome-linux.zip";
            //var unzippedDirectory = "/tmp/chrome-linux";

            //if (!Directory.Exists(unzippedDirectory) && Directory.Exists(directory) && isLinux)
            //{
            //    try
            //    {
            //        ZipFile.ExtractToDirectory(chromeSource, directory, true);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.Out.WriteLine($"Error during unzipping {chromeSource}");
            //    }
            //}
            //// new FileInfo(chromeSource).Delete();
            //Console.Out.WriteLine($"Running 'chmod 777' on '{unzippedDirectory}' folder: {Chromium.RunBashCommands("/bin/bash", $"-c \"chmod -R 777 {Path.GetFullPath(unzippedDirectory)}\"")}");

            //Console.Out.WriteLine($"Running 'ls' on '{unzippedDirectory}' folder: {Chromium.RunBashCommands("/bin/bash", $"-c \"ls -ahl {Path.GetFullPath(unzippedDirectory)}\"")}");
            ////var browserFetcher = new BrowserFetcher(browserFetcherOptions);
            ////new FileInfo(chromeSource).Delete();

            ////Console.Out.WriteLine($"Running 'ls' on '{unzippedDirectory}' folder: {Chromium.RunBashCommands("/bin/bash", $"-c \"ls -ahl {Path.GetFullPath(unzippedDirectory)}\"")}");
            ////try
            ////{
            ////    revisionInfo = browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision)
            ////       .GetAwaiter().GetResult(); // 843427 
            ////}
            ////catch (Exception ex)
            ////{
            ////    Console.Out.WriteLine($"Unable to download Chrome with error: {ex}");

            ////    //AmazonS3Uploader amazonS3 = new AmazonS3Uploader();

            ////    // downloadPath.SafelyCreateZipFromDirectory("/tmp/tmp.zip");
            ////    //amazonS3.UploadFile("/tmp/tmp.zip", "tmp.zip");
            ////    if (isLinux)
            ////    {
            ////        Console.Out.WriteLine($"Bash command result: {Chromium.RunBashCommands("/bin/bash", $"-c \"ls -ahl /tmp\"")}");
            ////        //$"Bash command result: {Chromium.RunBashCommands("/bin/bash", $"-c \"ls -ahl {Path.GetFullPath(Path.Combine(directory, ".."))}\"")}");
            ////    }
            ////}

            //// var executablePath = Path.Combine($"{revisionInfo?.ExecutablePath}"); // "/usr/lib64/chromium-browser"; // browserFetcher.GetExecutablePath(BrowserFetcher.DefaultRevision); // Chromium.GetExecutablePath();//  // 843427 - 89.0, 800071 - 86.0
            ////var executablePath = Path.Combine($"{revisionInfo?.ExecutablePath}");
            //var executablePath = Path.Combine($"{unzippedDirectory}", "chrome");

            ////if (string.IsNullOrEmpty(executablePath))
            ////{
            ////    throw new DirectoryNotFoundException($"Chromium executable file at {directory} is absent. Unable to start Chromium.");
            ////}

            ////Console.WriteLine($"Attemping to start Chromium using executable path: {executablePath}");

            //Browser browser = null;

            //try
            //{
            //    browser = Puppeteer.LaunchAsync(new LaunchOptions
            //    {
            //        Headless = true,
            //        Args = Chromium.Args,
            //        //Args = new string[] { "--no-sandbox" },
            //        //DefaultViewport = Chromium.GetDefaultViewport(),
            //        ExecutablePath = executablePath, // Chromium.GetExecutablePath(),
            //        IgnoreHTTPSErrors = true,
            //    }).Result;
            //}
            //catch (Exception ex)
            //{
            //    Console.Out.WriteLine($"Function runtime failed with: {ex}");
            //    throw new Exception("Starting chromium by Puppeteer has failed! <==============");
            //    //AmazonS3Uploader amazonS3 = new AmazonS3Uploader();

            //    // downloadPath.SafelyCreateZipFromDirectory("/tmp/tmp.zip");
            //    //amazonS3.UploadFile("/tmp/tmp.zip", "tmp.zip");
            //}

            ////var dump = ObjectDumper.Dump(browser);
            ////Console.WriteLine($"Puppeteer instance properties values: {dump}");

            //var page = browser?.NewPageAsync().Result;
            //var pageExample = page?.GoToAsync("https://example.com").Result;
            //title = page.GetTitleAsync().Result;
            ////Debug.WriteLine($"{title}");
            //Console.Out.WriteLine($"{title}");

            //browser?.CloseAsync();
            //Console.WriteLine($"Title of the website: {title}");

            // ============================= WebDriver way =========================== //
            ////IWebDriver driver = null;
            ////try
            ////{
            ////    WebDriverInit webDriverInit = new WebDriverInit();
            ////    driver = webDriverInit.GetWebDriver();
            ////}
            ////catch (Exception ex)
            ////{
            ////    driver?.Quit();
            ////    Console.Out.WriteLine($"Function runtime failed with: {ex}");
            ////    Console.Out.WriteLine($"Bash command result: {Chromium.RunBashCommands("/bin/bash", "-c \"ls -ahl /tmp\"")}");
            ////}

            ////driver?.Navigate().GoToUrl("https://example.com");
            ////var title = driver?.Title;
            ////driver?.Quit();
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Get Request\n");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = FunctionResult,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }
    }
}
