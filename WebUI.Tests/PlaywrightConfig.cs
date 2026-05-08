using System;
using System.IO;
using Microsoft.Playwright;
using NUnit.Framework;

namespace WebUI.Tests
{
    internal static class PlaywrightConfig
    {
        public static string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? "https://djinni.co";
        public static bool Headless => (Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") ?? "1") != "0";

        public static BrowserNewContextOptions CreateContextOptions(string recordingsDir)
        {
            Directory.CreateDirectory(recordingsDir);
            return new BrowserNewContextOptions
            {
                RecordVideoDir = recordingsDir,
                RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 },
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                IgnoreHTTPSErrors = true,
                AcceptDownloads = true
            };
        }
    }
}