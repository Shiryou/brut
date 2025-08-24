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
        public static string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "brut");
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
            Globals.mru.Load(Path.Combine(Globals.appData, "mru.json"));
            new Application(Eto.Platform.Detect).Run(new MainForm());
            Globals.mru.Save(Path.Combine(Globals.appData, "mru.json"));
        }

        public static void InitLogging()
        {
            // Configure Serilog to write to a file
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("SourceContext", "BrutGui.Program")
                .WriteTo.File(Path.Combine(Globals.appData, "brut-.log"), rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
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
