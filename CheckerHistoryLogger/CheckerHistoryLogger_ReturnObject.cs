using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vishnu_UserModules
{
    /// <summary>
    /// ReturnObject für das Ergebnis des CheckerHistoryLoggers.
    /// </summary>
    /// <remarks>
    /// Autor: Erik Nagel, NetEti
    ///
    /// 27.11.2020 Erik Nagel: erstellt
    /// 30.08.2022 Erik Nagel: verallgemeinert.
    /// </remarks>
    [Serializable()]
    public class CheckerHistoryLogger_ReturnObject
    {
        /// <summary>
        /// Wrapper-Klasse um List&lt;SubResult&gt; SubResults.
        /// </summary>
        [Serializable()]
        public class SubResultListContainer : ISerializable
        {
            /// <summary>
            /// 0 bis n Datensätze bestehend aus einem Detail-Ergebnis (bool?) und Detail-Record (hier: string).
            /// </summary>
            public List<SubResult> SubResults { get; set; }

            /// <summary>
            /// Standard Konstruktor.
            /// </summary>
            public SubResultListContainer()
            {
                this.SubResults = new List<SubResult>();
            }

            /// <summary>
            /// Deserialisierungs-Konstruktor.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Übertragungs-Kontext.</param>
            protected SubResultListContainer(SerializationInfo info, StreamingContext context)
            {
                this.SubResults = (List<SubResult>)info.GetValue("SubResults", typeof(List<SubResult>));
            }

            /// <summary>
            /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Serialisierungs-Kontext.</param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("SubResults", this.SubResults);
            }

            /// <summary>
            /// Überschriebene ToString()-Methode.
            /// </summary>
            /// <returns>Id des Knoten + ":" + ReturnObject.ToString()</returns>
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                string delimiter = "";
                if (this.SubResults != null)
                {
                    foreach (SubResult subResult in this.SubResults)
                    {
                        stringBuilder.Append(delimiter + subResult.ToString());
                        delimiter = Environment.NewLine;
                    }
                }
                return stringBuilder.ToString();
            }

            /// <summary>
            /// Vergleicht Dieses Result mit einem übergebenen Result nach Inhalt.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }
                SubResultListContainer subResultList = obj as SubResultListContainer;
                if (this.SubResults.Count != subResultList.SubResults.Count)
                {
                    return false;
                }
                for (int i = 0; i < this.SubResults.Count; i++)
                {
                    if (this.SubResults[i] != subResultList.SubResults[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Erzeugt einen eindeutigen Hashcode für dieses Result.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (this.ToString()).GetHashCode();
            }
        }

        /// <summary>
        /// Klasse für einen Record eines Teilergebnisses.
        /// </summary>
        [Serializable()]
        public class SubResultRecord : ISerializable
        {
            /// <summary>
            /// Enthält einen Gesamtstring für das SubResult (ToString()).
            /// </summary>
            public string LongResultString { get; set; }

            /// <summary>
            /// Standard Konstruktor.
            /// </summary>
            public SubResultRecord() { }

            /// <summary>
            /// Deserialisierungs-Konstruktor.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Übertragungs-Kontext.</param>
            protected SubResultRecord(SerializationInfo info, StreamingContext context)
            {
                this.LongResultString = info.GetString("LongResultString");
            }

            /// <summary>
            /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Serialisierungs-Kontext.</param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("LongResultString", this.LongResultString);
            }

            /// <summary>
            /// Überschriebene ToString()-Methode.
            /// </summary>
            /// <returns>Id des Knoten + ":" + ReturnObject.ToString()</returns>
            public override string ToString()
            {
                return this.LongResultString;
            }

            /// <summary>
            /// Vergleicht dieses Result mit einem übergebenen Result nach Inhalt.
            /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (this.ToString() != obj.ToString())
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Erzeugt einen eindeutigen Hashcode für dieses Result.
            /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (this.ToString()).GetHashCode();
            }
        }

        /// <summary>
        /// Klasse für ein Teilergebnis.
        /// </summary>
        [Serializable()]
        public class SubResult : ISerializable
        {
            /// <summary>
            /// Der Wert einer Detail-Information der Prüfroutine
            /// (i.d.R int).
            ///  </summary>
            public SubResultRecord ResultRecord { get; set; }

            /// <summary>
            /// Standard Konstruktor.
            /// </summary>
            public SubResult() { }

            /// <summary>
            /// Deserialisierungs-Konstruktor.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Übertragungs-Kontext.</param>
            protected SubResult(SerializationInfo info, StreamingContext context)
            {
                this.ResultRecord = (SubResultRecord)info.GetValue("ResultRecord", typeof(SubResultRecord));
            }

            /// <summary>
            /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
            /// </summary>
            /// <param name="info">Property-Container.</param>
            /// <param name="context">Serialisierungs-Kontext.</param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ResultRecord", this.ResultRecord);
            }

            /// <summary>
            /// Überschriebene ToString()-Methode.
            /// </summary>
            /// <returns>Id des Knoten + ":" + ReturnObject.ToString()</returns>
            public override string ToString()
            {
                return String.Format("{0}", this.ResultRecord.ToString());
            }

            /// <summary>
            /// Vergleicht dieses Result mit einem übergebenen Result nach Inhalt.
            /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (this.ToString() != obj.ToString())
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Erzeugt einen eindeutigen Hashcode für dieses Result.
            /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (this.ToString()).GetHashCode();
            }
        }

        /// <summary>
        /// Return-Objekt des aufgerufenen SubCheckers.
        /// </summary>
        public object SubCheckerReturnObject { get; set; }

        /// <summary>
        /// Wrapper-Klasse um List&lt;SubResult&gt; SubResults.
        /// </summary>
        public SubResultListContainer SubResultContainer { get; set; }

        /// <summary>
        /// Das logische Gesamtergebnis eines Prüfprozesses:
        /// true, false oder null.
        /// </summary>
        public bool? LogicalResult { get; set; }

        /// <summary>
        /// Die Anzahl der Treffer, die das Prüfkriterium erfüllen.
        /// </summary>
        public long RecordCount { get; set; }

        /// <summary>
        /// Aufbereiteter Zeitstempel der letzten Auswertung.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Name der Datei mit den letzten SubChecker-Werten.
        /// </summary>
        public string SubCheckerResultsInfoFile { get; set; }

        /// <summary>
        /// Klartext-Informationen zur Prüfroutine
        /// (was die Routine prüft).
        ///  </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public CheckerHistoryLogger_ReturnObject()
        {
            this.SubResultContainer = new SubResultListContainer();
            this.LogicalResult = null;
        }

        /// <summary>
        /// Deserialisierungs-Konstruktor.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Übertragungs-Kontext.</param>
        protected CheckerHistoryLogger_ReturnObject(SerializationInfo info, StreamingContext context)
        {
            this.SubCheckerReturnObject = (Object)info.GetValue("SubCheckerReturnObject", typeof(Object));
            this.SubResultContainer = (SubResultListContainer)info.GetValue("SubResults", typeof(SubResultListContainer));
            this.LogicalResult = (bool?)info.GetValue("LogicalResult", typeof(bool?));
            this.RecordCount = (long)info.GetValue("RecordCount", typeof(long));
            this.Timestamp = (DateTime)info.GetValue("Timestamp", typeof(DateTime));
            this.SubCheckerResultsInfoFile = info.GetString("SubCheckerResultsInfoFile");
            this.Comment = info.GetString("Comment");
        }

        /// <summary>
        /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Serialisierungs-Kontext.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SubCheckerReturnObject", this.SubCheckerReturnObject);
            info.AddValue("SubResults", this.SubResultContainer);
            info.AddValue("LogicalResult", this.LogicalResult);
            info.AddValue("RecordCount", this.RecordCount);
            info.AddValue("Timestamp", this.Timestamp);
            info.AddValue("SubCheckerResultsInfoFile", this.SubCheckerResultsInfoFile);
            info.AddValue("Comment", this.Comment);
        }

        /// <summary>
        /// Überschriebene ToString()-Methode - stellt öffentliche Properties
        /// als einen (mehrzeiligen) aufbereiteten String zur Verfügung.
        /// </summary>
        /// <returns>Alle öffentlichen Properties als ein String aufbereitet.</returns>
        public override string ToString()
        {
            string subCheckerReturnObjectString = this.SubCheckerReturnObject.ToString();
            string logicalResultStr = this.LogicalResult.ToString();
            StringBuilder str = new StringBuilder(subCheckerReturnObjectString);
            str.Append(String.Format("\n{0} (Info: {1})", logicalResultStr == "" ? "null" : logicalResultStr, this.Comment));
            str.Append(String.Format("\nLogdatei: {0}", this.SubCheckerResultsInfoFile));
            str.Append(String.Format("\nletzte Auswertung: {0:dd.MM.yyyy HH:mm}", this.Timestamp));
            str.Append(String.Format("\nAnzahl Werte: {0}", this.RecordCount));
            str.Append("\nRecords:");
            foreach (SubResult subResult in this.SubResultContainer.SubResults)
            {
                str.Append(String.Format("\n    {0}", subResult.ToString()));
            }
            return str.ToString();
        }

        /// <summary>
        /// Vergleicht dieses Result mit einem übergebenen Result nach Inhalt.
        /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
        /// </summary>
        /// <param name="obj">Das zu vergleichende Result.</param>
        /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            if (this.ToString() != obj.ToString())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Erzeugt einen eindeutigen Hashcode für dieses Result.
        /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
        /// </summary>
        /// <returns>Hashcode (int).</returns>
        public override int GetHashCode()
        {
            return (this.ToString()).GetHashCode();
        }
    }
}
