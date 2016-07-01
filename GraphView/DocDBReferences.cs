﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;
using Microsoft.SqlServer.TransactSql.ScriptDom;
// Add DocumentDB references
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents.Client;

namespace GraphView
{
    /// <summary>
    /// DocDBConnection is used to manage the connection with server. 
    /// </summary>
    public class DocDBConnection
    {
        public DocDBConnection(int pMaxPacketSize, GraphViewConnection connection)
        {
            MaxPacketSize = pMaxPacketSize;
            EndPointUrl = connection.DocDB_Url;
            PrimaryKey = connection.DocDB_Key;
            DatabaseID = connection.DocDB_DatabaseId;
            CollectionID = connection.DocDB_CollectionId;
            client = connection.client;
        }
        public int MaxPacketSize;
        public string EndPointUrl;
        public string PrimaryKey;
        public string DatabaseID;
        public string CollectionID;
        public DocumentClient client;
    }
    /// <summary>
    /// It is a raw data sturcture used to store the result of some process.
    /// It provides some useful functions to translate the infomation it stores into resonable output with a giving header.
    /// </summary>
    internal class Record
    {
        private static int ResultNumber = 0;
        internal void ReSetResultNumber(int pResultNumber)
        {
            ResultNumber = pResultNumber;
        }
        internal Record()
        {
            Bindings = new List<string>();
            Results = new List<string>();
            for (int i = 0; i < ResultNumber; i++) Results.Add("");
        }
        internal Record(int pResultNumber)
        {
            Bindings = new List<string>();
            Results = new List<string>();
            for (int i = 0; i < pResultNumber; i++) Results.Add("");
        }
        internal Record(List<string> pBindings, List<string> pResults)
        {
            Bindings = pBindings;
            Results = pResults;
        }
        public int GetBinding(string pId, List<int> pBindingHeader)
        {
            if (Bindings.IndexOf(pId) == -1) return -1;
            return pBindingHeader[Bindings.IndexOf(pId)];
        }
        public string GetId(string ResultIndex, List<string> pResultHeader)
        {
            if (pResultHeader.IndexOf(ResultIndex) == -1) return "";
            return Results[pResultHeader.IndexOf(ResultIndex)];
        }
        public string GetId(int Binding, List<int> pBindingHeader)
        {
            if (Bindings.Count <= pBindingHeader.IndexOf(Binding)) return "";
            return Bindings[pBindingHeader.IndexOf(Binding)];
        }
        public int GetIndex(string Result, List<string> pResultHeader)
        {
            return pResultHeader.IndexOf(Result);
        }
        public List<string> Bindings;
        public List<string> Results;
    }
    /// <summary>
    /// Table is a set of record with specified headers that is used to translate the record in it.
    /// </summary>
    internal class Table
    {
        internal List<int> BindingIndex;
        internal List<string> ResultsIndex;
        internal List<Record> records;
        internal int RecordIndex;
        public int FieldCount
        {
            get
            {
                return BindingIndex.Count + ResultsIndex.Count;
            }
        }
        public object this[Int32 index]
        {
            get
            {
                if (BindingIndex.IndexOf(index) == -1) return null;
                return records[RecordIndex].Bindings[BindingIndex.IndexOf(index)];
            }
        }
        public object this[string index]
        {
            get
            {
                if (ResultsIndex.IndexOf(index) == -1) return "";
                return records[RecordIndex].Results[ResultsIndex.IndexOf(index)];
            }
        }
        internal Table(List<int> pBindingIndex, List<string> pResultsIndex)
        {
            BindingIndex = pBindingIndex;
            ResultsIndex = pResultsIndex;
            records = new List<Record>();
        }
        internal void AddRecord(Record r)
        {
            records.Add(r);
        }
        public bool Read()
        {
            if (records.Count == RecordIndex) return false;
            else
            {
                RecordIndex++;
                return true;
            }
        }
        public int GetBinding(string pId)
        {
            if (records[RecordIndex].Bindings.IndexOf(pId) == -1) return -1;
            return BindingIndex[records[RecordIndex].Bindings.IndexOf(pId)];
        }
        public string GetId(string ResultIndex)
        {
            if (ResultsIndex.IndexOf(ResultIndex) == -1) return "";
            return records[RecordIndex].Results[ResultsIndex.IndexOf(ResultIndex)];
        }
        public string GetId(int Binding)
        {
            if (BindingIndex.IndexOf(Binding) == -1) return "";
            return records[RecordIndex].Bindings[BindingIndex.IndexOf(Binding)];
        }
        public void Dispose()
        {
            BindingIndex.Clear();
            ResultsIndex.Clear();
            records.Clear();
        }
    }
    /// <summary>
    /// DocDBOperator is the basic interface of all operator processor function.
    /// It provides three basic interface about the statue of a operator processor function.
    /// And one interface to execute the operator. 
    /// </summary>
    internal interface DocDBOperator
    {
        bool Statue();
        void Open();
        void Close();
        object Next();
    }
    /// <summary>
    /// The most basic class for all operator processor function,
    /// which implements some of the basic interface.
    /// and provides some useful sturcture like buffer on both input and output sides
    /// </summary>
    internal abstract class DocDBOperatorProcessor : DocDBOperator
    {
        internal Queue<Record> InputBuffer;
        internal Queue<Record> OutputBuffer;
        internal int InputBufferSize;
        internal int OutputBufferSize;
        internal List<DocDBOperatorProcessor> ChildrenProcessor;
        internal bool statue;
        public bool Statue()
        {
            return statue;
        }
        public void Open()
        {
            statue = true;
        }
        public void Close()
        {
            statue = false;
        }
        public abstract object Next();
    }
}