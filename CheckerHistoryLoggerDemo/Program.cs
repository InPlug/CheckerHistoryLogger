using System.ComponentModel;
using System.IO;
using Vishnu.Interchange;
using Vishnu_UserModules;

namespace CheckerHistoryLoggerDemo
{
    internal class Program
    {
        private static CheckerHistoryLogger? _demoChecker;

        static void Main(string[] args)
        {
            if (!Directory.Exists(@"../Snapshots"))
            {
                Directory.CreateDirectory(@"../Snapshots");
            }
            _demoChecker = new CheckerHistoryLogger();
            _demoChecker.NodeProgressChanged += SubNodeProgressChanged;
            Check_DiskSpace();
            FreeDemoChecker();
            Console.ReadLine();
        }

        private static void Check_DiskSpace()
        {
            bool? logicalResult = _demoChecker?.Run(
                @"Ermittelt den Plattenplatz auf Laufwerk C über einen längeren Zeitraum.|CheckDiskSpace.dll|C|20184|100|3|ermittelt den Plattenplatz",
                new TreeParameters("MainTree", null) { CheckerDllDirectory = Directory.GetCurrentDirectory() },
                TreeEvent.UndefinedTreeEvent);
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
            string recordsString = _demoChecker?.ReturnObject?.ToString() ?? "null";
            Console.WriteLine("LogicalResult: {0}\nResult: {1}", logicalResultString, recordsString);
            if (_demoChecker?.ReturnObject is CheckerHistoryLogger_ReturnObject)
            {
                CheckerHistoryLogger_ReturnObject checkerHistoryLogger_ReturnObject
                    = (CheckerHistoryLogger_ReturnObject)_demoChecker.ReturnObject;
                if (checkerHistoryLogger_ReturnObject.SubResultContainer?.SubResults != null)
                {
                    foreach (CheckerHistoryLogger_ReturnObject.SubResult? subResult
                        in checkerHistoryLogger_ReturnObject.SubResultContainer.SubResults)
                    {
                        Console.WriteLine("\t\tSubResult: {0}", subResult?.ToString() ?? "null");
                    }
                }
            }
        }

        private static void FreeDemoChecker()
        {
            if (_demoChecker != null)
            {
                _demoChecker.NodeProgressChanged -= SubNodeProgressChanged;
                if (_demoChecker is IDisposable)
                {
                    (_demoChecker as IDisposable).Dispose();
                }
            }
        }

        // Wird vom UserChecker bei Veränderung des Verarbeitungsfortschritts aufgerufen.
        // Wann und wie oft der Aufruf erfolgen soll, wird im UserChecker festgelegt.
        static void SubNodeProgressChanged(object? sender, ProgressChangedEventArgs args)
        {
            Console.WriteLine(args.ProgressPercentage);
        }
    }
}