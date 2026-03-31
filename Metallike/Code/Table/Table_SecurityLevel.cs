using System;
using System.Collections.Generic;

namespace Assets.Work.CDH.Code.Table
{
    public class Table_SecurityLevel : Table_Base
    {
        [Serializable]
        public class SecurityLevelData
        {
            public int Level;
            public float TargetValue;
        }

        public List<SecurityLevelData> List = new();

        public List<SecurityLevelData> Get()
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

            for (int row = _StartRow; row < reader.Row; ++row)
            {
                SecurityLevelData info = new SecurityLevelData();
                if (Read(reader, info, row, _StartCol) == false)
                    break;

                List.Add(info);
            }
        }

        protected bool Read(CsvReader reader, SecurityLevelData info, int row, int col)
        {
            if (reader.ResetRow(row, col) == false)
                return false;

            reader.Get(row, ref info.Level);
            reader.Get(row, ref info.TargetValue);

            return true;
        }
    }
}
