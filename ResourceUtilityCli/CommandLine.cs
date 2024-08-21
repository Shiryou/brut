﻿using System.Runtime.CompilerServices;

using ResourceUtilityLib;

enum Operations
{
    NoOp,
    OpAdd,
    OpRemove,
    OpExtract,
    OpList,
    OpResponseFile,
    OpVerify
}

class ResourceUtilityCli
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Output help info here.");
            return;
        }

        string resfile = args[0];

        Console.WriteLine("Processing " + resfile);
        ResourceUtility ru;
        try
        {
            ru = new ResourceUtility(resfile);
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
                case 'E':
                    operation = Operations.OpExtract;
                    file_operation = true;
                    break;
                case 'H':
                    switch (Char.ToUpper(args[i][1]))
                    {
                        case 'C':
                            ru.useCRCHash();
                            break;
                        case 'I':
                            ru.useIDHash();
                            break;
                        default:
                            Console.WriteLine("Invalid hashing algorithm.");
                            break;
                    }
                    break;
                case 'L':
                    operation = Operations.OpList;
                    break;
                case 'V':
                    operation = Operations.OpVerify;
                    break;
            }

            if (file_operation)
            {
                filename = args[++i];
            }

            switch (operation)
            {
                case Operations.OpExtract:
                    Extract(ru, filename);
                    break;
                case Operations.OpList:
                    List(ru);
                    break;
                case Operations.OpVerify:
                    List(ru, true);
                    break;
            }
        }
    }

    static void List(ResourceUtility ru, bool verify = false)
    {
        foreach (string str in ru.ListContents(verify))
        {
            Console.WriteLine(str);
        }
    }

    static void Extract(ResourceUtility ru, string filename)
    {
        ru.useCRCHash();
        try
        {
            ru.ExtractFile(filename);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine(String.Format("File {0} was not found in the resource file.", filename));
        }
    }
}
