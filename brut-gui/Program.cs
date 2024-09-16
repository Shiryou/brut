using System;

using Eto.Forms;

using ResourceUtilityLib;

namespace BrutGui
{
    public static class Globals
    {
        public static ResourceUtility resource = null;
        public static string resourceName = null;
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
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
