//Traceutils assembly
//writen by Locky, 2009.
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ExpressProfiler
{
    [Serializable]
    public class CEvent
    {
        // ReSharper disable UnaccessedField.Global
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        // ReSharper disable InconsistentNaming
        // ReSharper disable MemberCanBePrivate.Global
        [XmlAttribute]
        public long EventClass;

        [XmlAttribute]
        public long DatabaseID;

        [XmlAttribute]
        public long ObjectID;

        [XmlAttribute]
        public long RowCounts;

        public string TextData;

        [XmlAttribute]
        public string DatabaseName;

        [XmlAttribute]
        public string ObjectName;

        [XmlAttribute]
        public long Count, CPU, Reads, Writes, Duration, SPID, NestLevel;
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore InconsistentNaming
        // ReSharper restore FieldCanBeMadeReadOnly.Global
        // ReSharper restore UnaccessedField.Global

        public long AvgCPU
        {
            get { return Count == 0 ? 0 : CPU/Count; }
        }
        public long AvgReads
        {
            get{ return Count == 0 ? 0 : Reads / Count;}
        }
        public long AvgWrites
        {
            get { return Count == 0 ? 0 : Writes / Count;}
        }
        public long AvgDuration
        {
            get { return Count == 0 ? 0 : Duration / Count; }
        }
            
        //needed for serialization
        // ReSharper disable UnusedMember.Global
        public CEvent() { }
        // ReSharper restore UnusedMember.Global

        public CEvent(long aDatabaseID, string aDatabaseName, long aObjectID, string aObjectName, string aTextData)
        {
            DatabaseID = aDatabaseID;
            DatabaseName = aDatabaseName;
            ObjectID = aObjectID;
            ObjectName = aObjectName;
            TextData = aTextData;
        }

        public CEvent(long eventClass,long spid, long nestLevel, long aDatabaseID, string aDatabaseName, long aObjectID, string aObjectName, string aTextData, long duration, long reads, long writes,long cpu)
        {
            EventClass = eventClass;
            DatabaseID = aDatabaseID;
            DatabaseName = aDatabaseName;
            ObjectID = aObjectID;
            ObjectName = aObjectName;
            TextData = aTextData;
            Duration = duration;
            Reads = reads;
            Writes = writes;
            CPU = cpu;
            SPID = spid;
            NestLevel = nestLevel;
        }

        public string GetKey()
        {
            return String.Format("({0}).({1}).({2}).({3})", DatabaseID, ObjectID, ObjectName, TextData);
        }
    }

    public class SimpleEventList
    {
        public readonly SortedDictionary<string,CEvent> List;

        public SimpleEventList() 
        {
            List = new SortedDictionary<string, CEvent>(); 
        }

        public void SaveToFile(string filename) 
        {
            var eventArray = new CEvent[List.Count];
            List.Values.CopyTo(eventArray, 0);
            var x = new XmlSerializer(typeof(CEvent[]));

            using (var fs = new FileStream(filename, FileMode.Create))
            {
                x.Serialize(fs, eventArray);
            }
        }

        public void AddEvent(long eventClass, long nestLevel, long databaseID,string databaseName,long objectID,string objectName, string textData, long cpu, long reads, long writes, long duration,long count,long rowcounts)
        { 
            CEvent cEvent;
            var key = String.Format("({0}).({1}).({2})",databaseID,objectID,textData);

            if(!List.TryGetValue(key,out cEvent))
            {
                cEvent = new CEvent(databaseID,databaseName,objectID,objectName,textData);
                List.Add(key, cEvent);
            }

            cEvent.NestLevel = nestLevel;
            cEvent.EventClass = eventClass;
            cEvent.Count += count;
            cEvent.CPU += cpu;
            cEvent.Reads += reads;
            cEvent.Writes += writes;
            cEvent.Duration += duration;
            cEvent.RowCounts += rowcounts;
        }        
    }

    public class CEventList
    {
        public readonly SortedDictionary<string, CEvent[]> EventList;
        public CEventList() { EventList = new SortedDictionary<string, CEvent[]>(); }

        public void AppendFromFile(int cnt, string filename, bool ignorenonamesp, bool transform)
        {
            var x = new XmlSerializer(typeof(CEvent[]));

            using (var fs = new FileStream(filename, FileMode.Open))
            {
                var eventArray = (CEvent[])x.Deserialize(fs);
                var lexer = new YukonLexer();

                foreach (CEvent e in eventArray)
                {
                    if (e.TextData.Contains("statman") || e.TextData.Contains("UPDATE STATISTICS")) continue;

                    if (!ignorenonamesp || e.ObjectName.Length != 0)
                    {
                        if (transform)
                        {

                            AddEvent(cnt, e.DatabaseID, e.DatabaseName
                                     , e.ObjectName.Length == 0 ? 0 : e.ObjectID
                                     , e.ObjectName.Length == 0 ? "" : e.ObjectName
                                     , e.ObjectName.Length == 0 ?
                                                                    lexer.StandardSql(e.TextData) : e.TextData, e.CPU, e.Reads, e.Writes, e.Duration, e.Count,e.RowCounts);
                        }
                        else
                        {
                            AddEvent(cnt, e.DatabaseID, e.DatabaseName, e.ObjectID, e.ObjectName, e.TextData, e.CPU, e.Reads, e.Writes, e.Duration, e.Count,e.RowCounts);
                        }
                    }
                }
            }
        }

        public void AddEvent(int cnt, long databaseID, string databaseName, long objectID, string objectName, string textData, long cpu, long reads, long writes, long duration, long count,long rowcounts)
        {
            CEvent[] eventArray;
            CEvent cEvent;
            var key = String.Format("({0}).({1}).({2}).({3})", databaseID, objectID, objectName, textData);

            if (!EventList.TryGetValue(key, out eventArray))
            {
                eventArray = new CEvent[2];

                for (int k = 0; k < eventArray.Length; k++)
                {
                    eventArray[k] = new CEvent(databaseID, databaseName, objectID, objectName, textData);
                }

                EventList.Add(key, eventArray);
                cEvent = eventArray[cnt];
            }
            else
            {
                cEvent = eventArray[cnt];
            }

            cEvent.Count += count;
            cEvent.CPU += cpu;
            cEvent.Reads += reads;
            cEvent.Writes += writes;
            cEvent.Duration += duration;
            cEvent.RowCounts += rowcounts;
        }
    }
}