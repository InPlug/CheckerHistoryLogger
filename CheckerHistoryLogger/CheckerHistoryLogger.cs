using NetEti.ApplicationControl;
using NetEti.Globals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Vishnu.Interchange;

namespace Vishnu_UserModules
{
    /// <summary>
    /// Der CheckerHistoryLogger erzeugt Logfiles für die Ergebnisse beliebiger Checker.
    /// Er wird selbst als Checker im Job definiert und ruft seinerseits den
    /// tatsächlichen Checker als Sub-Checker auf, dessen Name in den Parametern
    /// übergeben wurde.
    /// Die Dll des Sub-Checkers muss sich (inkl. dort referenzierter Assemblies)
    /// im Plugin-Verzeichnis des aktuellen Jobs befinden.
    /// CheckerHistoryLogger lädt die Dll später dynamisch und ruft deren
    /// Run-Routine auf.
    /// Danach lädt CheckerHistoryLogger eine Logdatei mit historischen Werten des
    /// Sub-Checkers und ergänzt diese um die aktuellen Werte.
    /// In dieser Klasse wird nur mit ToString() gearbeitet, da der Typ des
    /// konkreten ReturnObjects des Sub-Checkers hier unbekannt ist.
    /// ToDo:
    /// Für ein spezifischeres Logging des ReturnObjects des SubCheckers kann
    /// diese Klasse abgeleitet werden und die virtuellen Methoden
    /// SetupSubCheckerResultsLogging, LogSubCheckerData und ReadHistory überschrieben werden.
    /// </summary>
    /// <remarks>
    /// Autor: Erik Nagel
    ///
    /// 21.10.2021 Erik Nagel: erstellt
    /// </remarks>
    public class CheckerHistoryLogger : INodeChecker, IDisposable
    {
        #region INodeChecker Implementation

        /// <summary>
        /// Kann aufgerufen werden, wenn sich der Verarbeitungsfortschritt
        /// des Checkers geändert hat, muss aber zumindest aber einmal zum
        /// Schluss der Verarbeitung aufgerufen werden.
        /// </summary>
        public event ProgressChangedEventHandler? NodeProgressChanged;

        /// <summary>
        /// Rückgabe-Objekt des Checkers
        /// </summary>
        public object? ReturnObject { get; set; }

        /// <summary>
        /// Hier wird der Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Ihre Aufrufparameter aus der JobDescription.xml oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter, enthält den Pfad zum Job/Plugin-Verzeichnis.</param>
        /// <param name="source">Auslösendes TreeEvent (kann null sein).</param>
        /// <returns>True, False oder null</returns>
        public bool? Run(object? checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            this._checkerDllDirectory = treeParameters.CheckerDllDirectory
                ?? throw new ArgumentNullException("CheckerDllDirectory ist nicht gesetzt.");
            this.OnNodeProgressChanged(0);
            this.ReturnObject = null; // optional: in UserChecker_ReturnObject IDisposable implementieren und hier aufrufen.
            String? paraString = checkerParameters?.ToString();
            if (paraString != null && this._paraString != paraString)
            {
                this._paraString = paraString;
                this.EvaluateParametersOrFail(this._paraString);
            }
            //--- Aufruf der Checker-Business-Logik ----------
            bool? returnCode = this.Work(this._subCheckerParaString, treeParameters, source);
            //------------------------------------------------
            this.OnNodeProgressChanged(100); // erforderlich!
            return returnCode;
        }

        #endregion INodeChecker Implementation

        /// <summary>
        /// Kürzel, das den Namen der aktuellen SubChecker-Dll enthält und steuert,
        /// dass das Logging, welches eigentlich ein broadcasting an alle bedeutet,
        /// nur für den in SetupSubCheckerResultsLogging() aufgesetzten Logger
        /// gefiltert wird.
        /// </summary>
        public string LoggingRegexId { get; private set; }

        /// <summary>
        /// Konstruktor - holt den InfoController und richtet das globale Logging ein.
        /// </summary>
        public CheckerHistoryLogger()
        {
            this._publisher = InfoController.GetInfoController();
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
            // Globales Logging installieren
            this._logger = new Logger();
            InfoType[] loggerInfos = InfoTypes.Collection2InfoTypeArray(InfoTypes.All);
            this._publisher.RegisterInfoReceiver(this._logger, loggerInfos);
            this._checkerDllDirectory = String.Empty;
            this.LoggingRegexId = String.Empty;
            this._subCheckerResultsInfoFile = String.Empty;
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Abschlussarbeiten.
        /// </summary>
        /// <param name="disposing">False, wenn vom eigenen Destruktor aufgerufen.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Aufräumarbeiten durchführen und dann beenden.
                    this.FreeExistingSubChecker();
                    this._subCheckerResultsLogger?.Dispose();
                    this._logger?.Dispose();
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~CheckerHistoryLogger()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        #region protected members

        /// <summary>
        /// Richtet das Logging für Ergebnisse des SubCheckers ein.
        /// Diese Standardimplementierung richtet ein einfaches Textfile-Logging in das DLL-Verzeichnis
        /// des SubCheckers ein und arbeitet mit ToString.
        /// Für ein spezifischeres Logging mit einem typisierten subCheckerReturnObject kann diese
        /// Routine überschrieben werden.
        /// </summary>
        /// <param name="loggingRegexId">Kürzel, das den Namen der aktuellen SubChecker-Dll enthält und steuert,
        /// dass das Logging, welches eigentlich ein broadcasting an alle bedeutet, nur für den in
        /// SetupSubCheckerResultsLogging() aufgesetzten Logger gefiltert wird (z.B. '#checkdiskspace#').</param>
        protected virtual void SetupSubCheckerResultsLogging(string loggingRegexId)
        {
            // Sorgt dafür, dass nur Zeilen, die den SubCheckerName enthalten, vom hier instanziierten
            // Logger geloggt werden.
            string loggingRegexFilter = String.Format($@"@(?:{loggingRegexId})");

            this._subCheckerResultsLogger = new Logger(this._subCheckerResultsInfoFile, loggingRegexFilter, true);
            InfoType[] loggerInfos = new InfoType[] { InfoType.Milestone };
            this._publisher.RegisterInfoReceiver(this._subCheckerResultsLogger, loggerInfos);
        }

        /// <summary>
        /// Loggt ein Returnobject eines SubCheckers über dessen ToString()-Methode in eine Textdatei.
        /// Für ein spezifischeres Logging mit einem typisierten subCheckerReturnObject kann diese
        /// Routine überschrieben werden.
        /// </summary>
        /// <param name="logicalResult">Das logische Ergebnis eines SubCheckers (true, false oder null).</param>
        /// <param name="subCheckerReturnObject">Das ReturnObject eines SubCheckers.</param>
        /// <param name="loggingRegexId">Kürzel, das den Namen der aktuellen SubChecker-Dll enthält und steuert,
        /// dass das Logging, welches eigentlich ein broadcasting an alle bedeutet, nur für den in
        /// SetupSubCheckerResultsLogging() aufgesetzten Logger gefiltert wird (z.B. '#checkdiskspace#').</param>
        protected virtual void LogSubCheckerData(bool? logicalResult, object subCheckerReturnObject, string loggingRegexId)
        {
            string timestamp = DateTime.Now.ToString("dd.MM.yy HH:mm:ss,fff");
            string logicalResultString = logicalResult.ToString() ?? "null";
            string subString = subCheckerReturnObject.ToString() ?? "";
            string subcheckerReturnObjectToStringOneLine = subString.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", "");
            string message = String.Format($"{timestamp} {logicalResultString} - {subcheckerReturnObjectToStringOneLine}");
            this._publisher.Publish(this, loggingRegexId + " " + message, InfoType.Milestone);
            this._subCheckerResultsLogger?.Flush();
        }

        /// <summary>
        /// Liest historische Ergebnisse des SubCheckers in eine Objekt-Liste ein.
        /// Diese Standardimplementierung erwartet ein Textfile im Verzeichnis der SubChecker-Dll.
        /// Für ein spezifischeres Logging mit einem typisierten subCheckerReturnObject kann diese
        /// Routine überschrieben werden.
        /// </summary>
        /// <param name="loggingRegexId">Kürzel, das den Namen der aktuellen SubChecker-Dll enthält und steuert,
        /// dass das Logging, welches eigentlich ein broadcasting an alle bedeutet, nur für den in
        /// SetupSubCheckerResultsLogging() aufgesetzten Logger gefiltert wird (z.B. '#checkdiskspace#').</param>
        /// <returns>Eine Objekt-Liste von historischen ReturnObjects des SubCheckers.</returns>
        protected virtual List<object> ReadHistory(string loggingRegexId)
        {
            List<object> records = new List<object>();
            string? line;
            if (File.Exists(this._subCheckerResultsInfoFile))
            {
                using (System.IO.StreamReader file = new System.IO.StreamReader(this._subCheckerResultsInfoFile, System.Text.Encoding.Default))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        records.Insert(0, line);
                    }
                    file.Close();
                }
            }
            return records;
        }

        #endregion protected members

        #region private members

        private IInfoController _publisher;
        private INodeChecker? _subChecker;
        private VishnuAssemblyLoader _assemblyLoader;
        private string _checkerDllDirectory;
        private Logger? _subCheckerResultsLogger;
        private CheckerHistoryLogger_ReturnObject? _checkerHistoryLogger_ReturnObject;
        private bool _quiet;
        private string? _paraString;
        private string? _comment;
        private string? _subCheckerName;
        private string? _subCheckerPath;
        private string? _subCheckerParaString;
        private string _subCheckerResultsInfoFile;

        private Logger _logger;

        private bool? Work(object? checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            // Hier folgt die eigentliche Checker-Verarbeitung, die einen erweiterten boolean als Rückgabe
            // dieses Checkers ermittelt und ggf. auch ein Return-Objekt mit zusätzlichen Informationen füllt.
            bool? rtn;
            List<object> records = new List<object>();
            object subChecker_ReturnObject;
            rtn = this._subChecker?.Run(checkerParameters, treeParameters, source);
            if (rtn == null || this._subChecker?.ReturnObject == null)
            {
                return null;
            }
            subChecker_ReturnObject = this._subChecker.ReturnObject;

            this.LogSubCheckerData(rtn, subChecker_ReturnObject, this.LoggingRegexId);

            records = this.ReadHistory(this.LoggingRegexId);

            if (this._checkerHistoryLogger_ReturnObject != null)
            {
                this._checkerHistoryLogger_ReturnObject.SubCheckerReturnObject = subChecker_ReturnObject;
                this._checkerHistoryLogger_ReturnObject.SubResultContainer?.SubResults?.Clear();
                rtn &= this.RecordsToResult(records, this._checkerHistoryLogger_ReturnObject);
                this._checkerHistoryLogger_ReturnObject.LogicalResult = rtn;
            }
            else
            {
                throw new ApplicationException("_checkerHistoryLogger_ReturnObject ist null.");
            }
            if (this._quiet)
            {
                this.ReturnObject = subChecker_ReturnObject;
            }
            else
            {
                this.ReturnObject = this._checkerHistoryLogger_ReturnObject;
            }
            return rtn;
        }

        private void EvaluateParametersOrFail(string paraString)
        {
            this._quiet = false;
            this._comment = "";
            this._subCheckerName = null;
            this._subCheckerParaString = "";

            List<string> paraStrings = paraString.Split('|').ToList();
            while (paraStrings.Count > 0 && String.IsNullOrEmpty(this._subCheckerName))
            {
                string para = paraStrings.First();
                paraStrings.RemoveAt(0);
                this.EvaluateParameter(para);
            }
            if (String.IsNullOrEmpty(this._subCheckerName))
            {
                throw new ArgumentException(this.syntax("Es muss der Name der konkreten Checker-Dll übergeben werden."));
            }
            if (paraStrings.Count > 0)
            {
                this._subCheckerParaString = String.Join("|", paraStrings);
            }
            // Sorgt später dafür, dass nur Zeilen, die den SubCheckerName enthalten, geloggt werden.
            this.LoggingRegexId = String.Format($@"#{Path.GetFileNameWithoutExtension(this._subCheckerName)}#");

            string subCheckerPath = Path.Combine(this._checkerDllDirectory, this._subCheckerName);
            if (!subCheckerPath.Equals(this._subCheckerPath))
            {
                this.FreeExistingSubChecker();
                this._subCheckerPath = subCheckerPath;
                this.LoadSubChecker();
            }
            string historyRootPath = GenericSingletonProvider.GetInstance<AppSettings>().ResolvedSnapshotDirectory;
            string subCheckerResultsInfoFile = Path.Combine(historyRootPath ?? "", Path.GetFileNameWithoutExtension(this._subCheckerName) + ".log");
            if (!subCheckerResultsInfoFile.Equals(this._subCheckerResultsInfoFile))
            {
                this._subCheckerResultsLogger?.Dispose();
                this._subCheckerResultsInfoFile = subCheckerResultsInfoFile;
                this.SetupSubCheckerResultsLogging(this.LoggingRegexId);
            }
            this._checkerHistoryLogger_ReturnObject = new CheckerHistoryLogger_ReturnObject()
            {
                SubCheckerResultsInfoFile = this._subCheckerResultsInfoFile,
                Comment = this._comment
            };
        }

        private void EvaluateParameter(string para)
        {
            string paraString = para.Trim().ToLower();
            if (paraString.Equals("quiet"))
            {
                this._quiet = true;
                return;
            }
            if (!paraString.EndsWith(".dll"))
            {
                paraString += ".dll";
            }
            string subCheckerPath = Path.Combine(this._checkerDllDirectory, paraString);
            if (File.Exists(subCheckerPath))
            {
                this._subCheckerName = paraString;
                return;
            }
            this._comment = para;
        }

        private void FreeExistingSubChecker()
        {
            if (this._subChecker != null)
            {
                this._subChecker.NodeProgressChanged -= _subChecker_NodeProgressChanged;
                if (this._subChecker is IDisposable)
                {
                    ((IDisposable)this._subChecker).Dispose();
                }
            }
        }

        private void LoadSubChecker()
        {
            this._subChecker = this.DynamicLoadSlaveDll(this._subCheckerPath ?? "")
                ?? throw new ApplicationException(this._subCheckerPath + " konnte nicht geladern werden.");
            this._subChecker.NodeProgressChanged += _subChecker_NodeProgressChanged;
        }

        private void _subChecker_NodeProgressChanged(object? sender, ProgressChangedEventArgs args)
        {
            this.OnNodeProgressChanged(args.ProgressPercentage);
        }

        private void OnNodeProgressChanged(int progressPercentage)
        {
            NodeProgressChanged?.Invoke(null, new ProgressChangedEventArgs(progressPercentage, null));
        }

        private bool RecordsToResult(List<object> records, CheckerHistoryLogger_ReturnObject returnObject)
        {
            returnObject.Timestamp = DateTime.Now;
            returnObject.RecordCount = records.Count;
            bool rtn = true;
            int recordsCount = records != null ? records.Count : 0;
            if (recordsCount == 0)
            {
                rtn = false;
            }
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    CheckerHistoryLogger_ReturnObject.SubResult subResult = new CheckerHistoryLogger_ReturnObject.SubResult();
                    subResult.ResultRecord = new CheckerHistoryLogger_ReturnObject.SubResultRecord();
                    subResult.ResultRecord.LongResultString = records[i]?.ToString() ?? "null";
                    returnObject.SubResultContainer?.SubResults?.Add(subResult);
                }
            }
            return rtn;
        }

        // Lädt eine Plugin-Dll dynamisch. Muss keine Namespaces oder Klassennamen
        // aus der Dll kennen, Bedingung ist nur, dass eine Klasse in der Dll das
        // Interface INodeChecker implementiert. Die erste gefundene Klasse wird
        // als Instanz von INodeChecker zurückgegeben.
        private INodeChecker? DynamicLoadSlaveDll(string slavePathName)
        {
            return (INodeChecker?)this._assemblyLoader
                .DynamicLoadObjectOfTypeFromAssembly(slavePathName, typeof(INodeChecker));
        }

        private string syntax(string errorMessage)
        {
            return (
                       this.GetType().Name + ": "
                       + errorMessage
                       + Environment.NewLine
                       + "Parameter: [quiet|][comment|]sub-checker-dll[|sub-checker-parameters]"
                       + Environment.NewLine
                       + String.Format("quiet (optional): wenn angegeben, dann wird nur das aktuelle Ergebnis des Sub-Checkers zurückgegeben, ansonsten die Historie (Default)")
                       + Environment.NewLine
                       + String.Format("comment (optional): beliebiger Info-Text (darf kein '|' enthalten)")
                       + Environment.NewLine
                       + "sub-checker-dll: Name der Sub-Checker-dll, notwendiger Parameter"
                       + Environment.NewLine
                       + "weitere sub-checker-Parameter (optional, je nach sub-checker)"
                       + Environment.NewLine
                       + @"Beispiele: false|Ermittelt den aktuellen Plattenplatz auf Laufwerk C.|CheckDiskSpace.dll|C|20184|100|3|ermittelt den Plattenplatz auf Laufwerk C."
                       + Environment.NewLine
                       + @"           CheckDiskSpace.dll|D|20184|100|3"
                   );
        }

        private void Publish(string message)
        {
            InfoController.Say("CheckerHistoryLogger-" + this._subCheckerName + ": " + message);
        }

        #endregion private members
    }
}
