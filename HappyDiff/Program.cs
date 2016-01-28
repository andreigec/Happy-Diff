using System;
using System.Linq;
using System.Windows.Forms;
using ANDREICSLIB.Helpers;
using NLog;

namespace HappyDiff
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //-noform
            if (args != null && args.Any(s => string.Equals(s, "-noform", StringComparison.CurrentCultureIgnoreCase)))
            {
              
#if DEBUG
                Console.ReadKey();
#endif
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }

        }
    }
}
