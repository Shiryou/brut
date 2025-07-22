using System;
using System.IO;

using Eto.Forms;
using Microsoft.Extensions.Logging;
using Serilog;

using ResourceUtilityLib;

namespace BrutGui
{
    public static class Globals
    {
        public static Microsoft.Extensions.Logging.ILogger logger = null;
        public static ResourceUtility resource = null;
        public static string resourceName = null;
        public static MRU mru = new MRU(10);
    }

    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            InitLogging();
            if (args.Length > 0)
            {
                Globals.resource = new ResourceUtility(args[0], Globals.logger);
                Globals.resourceName = Path.GetFileName(args[0]).ToUpper();
            }
            LoadMRU();
            new Application(Eto.Platform.Detect).Run(new MainForm());
            SaveMRU();
        }

        public static void LoadMRU()
        {
            if (File.Exists("mru.json"))
            {
                string jsonString = File.ReadAllText("mru.json");
                Globals.mru = new MRU(Globals.mru.Size(), jsonString);
            }
        }

        public static void SaveMRU()
        {
            string fileName = "mru.json";

            File.WriteAllText(fileName, Globals.mru.ToJson());
        }

        public static void InitLogging()
        {
            // Configure Serilog to write to a file
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("SourceContext", "BrutGui.Program")
                .WriteTo.File("brut.log", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Wrap Serilog in Microsoft's logging abstraction
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger, dispose: true);
            });

            // Create a logger instance for the library class
            var logger = loggerFactory.CreateLogger<ResourceUtility>();
            Globals.logger = logger;
        }
    }
}
