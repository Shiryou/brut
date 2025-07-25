﻿using Microsoft.Extensions.Logging;

using ResourceUtilityLib;
using ResourceUtilityLib.Logging;

using Serilog;
using Serilog.Events;

enum Operations
{
    NoOp,
    OpAdd,
    OpRemove,
    OpExtract,
    OpExtractAll,
    OpList,
    OpResponseFile,
    OpVerify
}

class ResourceUtilityCli
{
    static void Main(string[] args)
    {
        var logger = CheckLogging(ref args);

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: resutil resfile-name [[s nnnnn] [c] [n] [r] [u] [+|-|e sourcefile-name ]] [--log logfile] [--log-level level] |");
            Console.WriteLine(" [@ respfile-name] | [l] | [v]");
            Console.WriteLine("   +  add file");
            Console.WriteLine("   -  remove file");
            Console.WriteLine("   c  compress resources (default)");
            Console.WriteLine("   e  extract file");
            Console.WriteLine("   x  extract all files");
            Console.WriteLine("   hc use CRC hash (default)");
            Console.WriteLine("   hi use ID hash");
            Console.WriteLine("   l  list contents of resource file");
            Console.WriteLine("   n  do not rotate PCX resources (default)");
            Console.WriteLine("   r  rotate PCX resources");
            Console.WriteLine("   s  nnnnn max size of resource permitted");
            Console.WriteLine("   t  attempt to restore a PCX file");
            Console.WriteLine("   u  do not compress resources");
            Console.WriteLine("   v  verify resource file");
            Console.WriteLine("   @  respfile run commands in respfile");
            Console.WriteLine("");
            Console.WriteLine("   --log  create a log file at the given location.");
            Console.WriteLine("   --log-level  create a log file at the given location.");
            Console.WriteLine("Note: Compression, respfiles, and derotating a PCX are not yet supported.");
            return;
        }

        string resfile = args[0];

        Console.WriteLine("Processing " + resfile);
        ResourceUtility ru;
        try
        {
            ru = new ResourceUtility(resfile, logger);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Unable to find " + resfile);
            return;
        }

        Console.WriteLine("Version: " + ru.FileVersion());
        Console.WriteLine("Resources: " + ru.Count());

        Operations operation = Operations.NoOp;
        string filename = "";
        for (int i = 1; i < args.Length; i++)
        {
            bool file_operation = false;

            switch (Char.ToUpper(args[i][0]))
            {
                case '+':
                    operation = Operations.OpAdd;
                    file_operation = true;
                    break;
                case '-':
                    operation = Operations.OpRemove;
                    file_operation = true;
                    break;
                case 'E':
                    operation = Operations.OpExtract;
                    file_operation = true;
                    break;
                case 'X':
                    operation = Operations.OpExtractAll;
                    break;
                case 'H':
                    switch (Char.ToUpper(args[i][1]))
                    {
                        case 'C':
                            ru.UseCRCHash();
                            break;
                        case 'I':
                            ru.UseIDHash();
                            break;
                        default:
                            Console.WriteLine("Invalid hashing algorithm. Using default.");
                            break;
                    }
                    break;
                case 'L':
                    operation = Operations.OpList;
                    break;
                case 'V':
                    operation = Operations.OpVerify;
                    break;

                case 'C':
                    Console.WriteLine("Compression is not yet supported when adding a file.");
                    ru.EnableCompression();
                    break;
                case 'U':
                    ru.DisableCompression();
                    break;
                case 'R':
                    ru.EnablePCXRotation();
                    break;
                case 'N':
                    ru.DisablePCXRotation();
                    break;
                case 'T':
                    ru.RestorePCX();
                    break;
            }

            if (file_operation)
            {
                filename = args[++i];
            }
        }

        switch (operation)
        {
            case Operations.OpAdd:
                Add(ru, filename);
                LogHelper.Info("Adding {0} to {0}", filename, resfile);
                break;
            case Operations.OpRemove:
                LogHelper.Info("Removing {0} from {0}", filename, resfile);
                Remove(ru, filename);
                break;
            case Operations.OpExtract:
                LogHelper.Info("Extracting {0} from {0}", filename, resfile);
                Extract(ru, filename);
                break;
            case Operations.OpExtractAll:
                LogHelper.Info("Extracting all files in {0}", resfile);
                ExtractAll(ru);
                break;
            case Operations.OpList:
                LogHelper.Info("Listing files in {0}", resfile);
                List(ru);
                break;
            case Operations.OpVerify:
                LogHelper.Info("Verifying files in {0}", resfile);
                List(ru, true);
                break;
        }

        //TODO:check if file is empty and delete if so.
    }

    static void List(ResourceUtility ru, bool verify = false)
    {
        ResourceHeader[] headers = ru.ListContents(verify);
        for (int i = 0; i < headers.Length; i++)
        {
            if (verify)
            {
                Console.WriteLine(String.Format("{0,4} {1,12} {2,6} {3} {4} {5,6}", i, ResourceUtility.CharArrayToString(headers[i].filename).PadRight(12), headers[i].cbUncompressedData, headers[i].flags, ResourceUtility.GetCompressionTypes()[headers[i].compressionCode], headers[i].cbCompressedData));
            }
            else
            {
                Console.WriteLine(String.Format("{0,4} {1,12} {2,6}", i, ResourceUtility.CharArrayToString(headers[i].filename).PadRight(12), headers[i].cbChunk));
            }
        }
    }

    static void Add(ResourceUtility ru, string filename)
    {
        ru.AddFiles(filename);
    }

    static void Remove(ResourceUtility ru, string filename)
    {
        ru.RemoveFiles(new[] { filename });
    }

    static void Extract(ResourceUtility ru, string filename)
    {
        try
        {
            ResourceHeader resource = ru.GetFileInformation(filename);
            Log.Error("\nFile {0} was not found in the resource file.", filename);
            Console.Write(String.Format("Extracting {0} containing {1} bytes... ", filename, resource.cbUncompressedData));
            ru.ExtractFile(filename);
            Console.WriteLine("Succeeded!");
        }
        catch (FileNotFoundException)
        {
            Log.Error("\nFile {0} was not found in the resource file.", filename);
        }
    }

    static void ExtractAll(ResourceUtility ru)
    {
        ru.ExtractAll();
    }

    static ILogger<ResourceUtility> CheckLogging(ref string[] args)
    {
        string? logFile = null;
        LogEventLevel? logLevel = null;
        List<string> arglist = new();
        string[] new_args = new string[args.Length];

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--log":
                    LogHelper.Info("Enabling log file {0}", args[index + 1]);
                    logFile = args[index + 1];
                    index++;
                    break;
                case "--log-level":
                    LogHelper.Info("Enabling log level {0}", args[index + 1]);
                    switch (args[index + 1].ToUpper())
                    {
                        case "VERBOSE":
                            logLevel = LogEventLevel.Verbose;
                            break;
                        case "DEBUG":
                            logLevel = LogEventLevel.Debug;
                            break;
                        case "INFORMATION":
                            logLevel = LogEventLevel.Information;
                            break;
                        case "WARNING":
                            logLevel = LogEventLevel.Warning;
                            break;
                        case "ERROR":
                            logLevel = LogEventLevel.Error;
                            break;
                        case "FATAL":
                            logLevel = LogEventLevel.Fatal;
                            break;
                    }
                    index++;
                    break;
                default:
                    arglist.Add(args[index]);
                    break;
            }
        }
        args = arglist.ToArray();
        return InitLogging(logFile, logLevel);
    }

    public static ILogger<ResourceUtility> InitLogging(string? file = null, LogEventLevel? level = null)
    {
        LogEventLevel fileLevel = LogEventLevel.Debug;
        LogEventLevel consoleLevel = LogEventLevel.Warning;
        string logFile = "logs/brut-.log";
        if (level != null)
        {
            fileLevel = consoleLevel = (LogEventLevel)level;
        }
        if (file != null)
        {
            logFile = file;
        }

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("SourceContext", "ResourceUtilityCli")
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: consoleLevel)
            .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: fileLevel, shared: true)
            .CreateLogger();

        LogHelper.Info("Logging level {0} to {1}, and {2} to console", fileLevel, logFile, consoleLevel);

        // Wrap Serilog in Microsoft's logging abstraction
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(Log.Logger);
        });

        // Create a logger instance for the library class
        var logger = loggerFactory.CreateLogger<ResourceUtility>();
        return logger;
    }
}
