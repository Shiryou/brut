using System;
using Eto.Forms;

namespace BrutGui
{

    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
