using NetEti.Globals;
using System;
using System.IO;
using Vishnu.Interchange;
using Vishnu_UserModules;

namespace CheckerHistoryLoggerDemo
{
    internal class Program
    {
        private static CheckerHistoryLogger demoChecker;
        static void Main(string[] args)
        {
            demoChecker = new CheckerHistoryLogger();
            demoChecker.NodeProgressChanged += SubNodeProgressChanged;
            Check_DiskSpace();
            FreeDemoChecker();
            Console.ReadLine();
        }

        private static void Check_DiskSpace()
        {
            //bool? logicalResult = demoChecker.Run(@"quiet|Ermittelt den aktuellen Plattenplatz auf Laufwerk C.|CheckDiskSpace.dll|C|20184|100|3|ermittelt den Plattenplatz",
            //    new TreeParameters("MainTree", null) { CheckerDllDirectory = Directory.GetCurrentDirectory() }, null);
            bool? logicalResult = demoChecker.Run(@"Ermittelt den Plattenplatz auf Laufwerk C über einen längeren Zeitraum.|CheckDiskSpace.dll|C|20184|100|3|ermittelt den Plattenplatz",
                new TreeParameters("MainTree", null) { CheckerDllDirectory = Directory.GetCurrentDirectory() }, null);
            ShowResult(logicalResult);
        }

        private static void ShowResult(bool? logicalResult)
        {
            string logicalResultString;
            switch (logicalResult)
            {
                case true: logicalResultString = "true"; break;
                case false: logicalResultString = "false"; break;
                default: logicalResultString = "null"; break;
            }
            string recordsString = demoChecker.ReturnObject.ToString();
            Console.WriteLine("LogicalResult: {0}\nResult: {1}",
                logicalResultString, recordsString);
        }

        private static void FreeDemoChecker()
        {
            if (demoChecker != null)
            {
                demoChecker.NodeProgressChanged -= SubNodeProgressChanged;
                if (demoChecker is IDisposable)
                {
                    (demoChecker as IDisposable).Dispose();
                }
            }
        }

        // Wird vom UserChecker bei Veränderung des Verarbeitungsfortschritts aufgerufen.
        // Wann und wie oft der Aufruf erfolgen soll, wird im UserChecker festgelegt.
        static void SubNodeProgressChanged(object sender, CommonProgressChangedEventArgs args)
        {
            Console.WriteLine("{0} of {1}", args.CountSucceeded, args.CountAll);
        }
    }
}
