using System;
using System.IO;

using Eto.Forms;

using ResourceUtilityLib;

namespace BrutGui
{
    public static class Globals
    {
        public static ResourceUtility resource = null;
        public static string resourceName = null;
        public static MRU mru = new MRU(10);
    }

    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Globals.resource = new ResourceUtility(args[0]);
                Globals.resourceName = System.IO.Path.GetFileName(args[0]).ToUpper();
            }
            LoadMRU();
            new Application(Eto.Platform.Detect).Run(new MainForm());
            SaveMRU();
        }

        public static void LoadMRU()
        {
            if (File.Exists("test.json"))
            {
                string jsonString = File.ReadAllText("test.json");
                Globals.mru = new MRU(Globals.mru.Size(), jsonString);
            }
        }

        public static void SaveMRU()
        {
            string fileName = "test.json";

            File.WriteAllText(fileName, Globals.mru.ToJson());
        }
    }
}
