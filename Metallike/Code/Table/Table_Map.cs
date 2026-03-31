using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets.Work.CDH.Code.Table
{
    public class Table_Map : Table_Base
    {
        [Serializable]
        public class MapData
        {
            public int MapId;
            public string[] GroupNames;
            public List<List<string>> Map;
        }

        public List<MapData> List = new();

        public List<MapData> GetMaps()
        {
            return List;
        }

        public int GetCount()
        {
            return List.Count;
        }

        public void Init_Binary(string _Name)
        {
            Load_Binary(_Name, ref List);
        }

        public void Save_Binary(string _Name)
        {
            Save_Binary(_Name, List);
        }

        public void Init_Csv(string _Name, int _StartRow, int _StartCol)
        {
            CsvReader reader = GetCSVReader(_Name);

            MapData info = new MapData();

            for (int row = _StartRow; row < reader.Row; ++row)
            {
                if (Read(reader, info, row, _StartCol) == false)
                    break;
            }

            List.Add(info);
        }

        protected bool Read(CsvReader _Reader, MapData _Info, int _Row, int _Col)
        {
            if (_Reader.ResetRow(_Row, _Col) == false)
                return false;

            if(_Row == 0)
            {
                _Info.MapId = _Reader.GetInt(_Row, 0);
            }
            else if(_Row == 1)
            {
                if (_Info.GroupNames == null)
                    _Info.GroupNames = new string[5];

                for (int i = 0; i < 5; ++i)
                {
                    _Reader.Get(_Row, ref _Info.GroupNames[i]);
                }
            }
            else
            {
                if (_Info.Map == null)
                    _Info.Map = new();

                List<string> strs = new();
                _Reader.GetLineStrings(_Row, ref strs);
                _Info.Map.Add(strs);
            }

            return true;
        }
    }
}
