using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CsvReader
{
    private System.String[,] Arr_Grid;

    public CsvReader()
    {
    }

    public CsvReader(System.String[,] _Grid)
    {
        Arr_Grid = _Grid;
    }

    public System.String[,] Grid
    {
        get { return Arr_Grid; }
    }

    public CsvReader Parse(UnityEngine.TextAsset _TextAsset, bool _Debug)
    {
        Parse(_TextAsset, _Debug);

        return this;
    }

    public CsvReader Parse(string _Text, bool _Debug, int _Encode = 0)
    {
        Arr_Grid = SplitCsvGrid(_Text, _Encode);

        if (_Debug)
            DebugOutPutGrid();

        return this;
    }

    public void DebugOutPutGrid()
    {
        System.String textoutput = "";

        for (int i = 0; i < Arr_Grid.GetUpperBound(1); i++)
        {
            for (int j = 0; j < Arr_Grid.GetUpperBound(0); j++)
            {
                textoutput += Arr_Grid[j, i];
                textoutput += "|";

                textoutput += "\n";
            }
        }

        Debug.Log(textoutput);
    }

    public int Column
    {
        get { return Arr_Grid.GetUpperBound(0); }
    }

    public int Row
    {
        get { return Arr_Grid.GetUpperBound(1); }
    }

    public System.String[] GetRowArray(int _Row)
    {
        System.String[] arr = new System.String[Column];

        for (int i = 0; i < Column; ++i)
        {
            arr[i] = Arr_Grid[i, Row];
        }

        return arr;
    }

    public bool IsData(int _Row, int _Col)
    {
        string s = Arr_Grid[_Col, _Row];

        if ((s == null) || (s == ""))
            return false;

        return true;
    }

    public int GetInt(int _Row, int _Col)
    {
        string s = Arr_Grid[_Col, _Row];

        if ((s == null) || (s == ""))
            return 0;

        return (int)System.Convert.ToInt32(s);
    }

    private int CurCol = 0;

    public bool ResetRow(int _Row, int _StartCol)
    {
        CurCol = _StartCol;

        string s = Arr_Grid[_StartCol, _Row];

        if (s == null)
            return false;

        if (Arr_Grid[_StartCol, _Row] == "")
            return false;

        return true;
    }

    public void Get(int _Row, ref bool _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = false;
            return;
        }

        _Val = ((int)System.Convert.ToInt32(s) != 0);
    }

    public void Get(int _Row, ref int _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = 0;
            return;
        }

        _Val = (int)System.Convert.ToInt32(s);
    }

    public void Get(int _Row, ref float _Val)
    {
        string s = Arr_Grid[CurCol, _Row];
        ++CurCol;

        if (string.IsNullOrEmpty(s))
        {
            _Val = 0f;
            return;
        }

        if (!float.TryParse(s, out _Val))
        {
            _Val = 0f;
        }
    }

    public void Get(int _Row, ref long _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = 0;
            return;
        }

        _Val = (long)System.Convert.ToInt64(s);
    }

    public void Get(int _Row, ref string _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = "";
            return;
        }

        _Val = s;
    }

    public void GetLineStrings(int _Row, ref List<string> _Val)
    {
        if (_Val == null)
        {
            _Val = new();
        }

        int colCount = Arr_Grid.GetLength(0);

        List<string> rowList = new();

        for (int x = 0; x < colCount; x++)
        {
            string str = Arr_Grid[x, _Row];

            if (!string.IsNullOrEmpty(str))
            {
                rowList.Add(str);
            }
        }

        if (rowList.Count > 0)
        {
            _Val = rowList;
        }
    }

    public void Get(int _Row, ref int[] _Val, int _Cnt)
    {
        for (int i = 0; i < _Cnt; ++i)
        {
            string s = Arr_Grid[CurCol, _Row];

            ++CurCol;

            if ((s == null) || (s == ""))
            {
                _Val[i] = 0;
                continue;
            }

            _Val[i] = (int)System.Convert.ToInt32(s);
        }
    }

    public void Get(int _Row, float _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = 0;
            return;
        }

        _Val = (float)System.Convert.ToSingle(s);
    }

    public void Get(int _Row, ref float[] _Val, int _Cnt)
    {
        for (int i = 0; i < _Cnt; ++i)
        {
            string s = Arr_Grid[CurCol, _Row];

            ++CurCol;

            if ((s == null) || (s == ""))
            {
                _Val[i] = 0;
                continue;
            }

            _Val[i] = (float)System.Convert.ToInt32(s);
        }
    }

    public void Get(int _Row, string _Val)
    {
        string s = Arr_Grid[CurCol, _Row];

        ++CurCol;

        if ((s == null) || (s == ""))
        {
            _Val = "";
            return;
        }

        _Val = s;
    }

    public void Get(int _Row, ref string[] _Val, int _Cnt)
    {
        for (int i = 0; i < _Cnt; ++i)
        {
            string s = Arr_Grid[CurCol, _Row];

            ++CurCol;

            if ((s == null) || (s == ""))
            {
                _Val[i] = "";
                continue;
            }

            _Val[i] = s;
        }
    }

    public CsvReader Find(int _FieldIndex, System.String _Value)
    {
        List<int> listindex = new List<int>();

        for (int i = 0; i < Arr_Grid.GetUpperBound(1); ++i)
        {
            if (_Value != Arr_Grid[_FieldIndex, i])
                continue;

            listindex.Add(i);
        }

        if (0 == listindex.Count)
            return null;

        System.String[,] arrnewgrid = new System.String[Arr_Grid.GetUpperBound(0) + 1, listindex.Count + 1];

        for (int i = 0; i < Arr_Grid.GetUpperBound(0); ++i)
        {
            for (int j = 0; j < listindex.Count; ++i)
            {
                arrnewgrid[i, j] = Arr_Grid[i, listindex[j]];
            }
        }

        return new CsvReader(arrnewgrid);
    }

    public System.String FindValue(int _FieldIndex, System.String _Value, System.Object _Field)
    {
        return Find(_FieldIndex, _Value).Grid[System.Convert.ToInt32(_Field), 0];
    }

    System.String[,] SplitCsvGrid(System.String _CsvText, int _Encode)
    {
        if (2 == _Encode)
            _CsvText = _CsvText.Replace("\t", ",");

        bool FindNewLine = false;
        int FindStartIndex = 0;
        int FindEndIndex = 0;

        List<string> list = new List<string>();

        for (int i = 0; i < _CsvText.Length; ++i)
        {
            if (_CsvText[i] == '"')
            {
                if (FindNewLine == false)
                {
                    FindStartIndex = i;
                    list.Add(_CsvText.Substring(FindEndIndex, FindStartIndex - FindEndIndex));
                    FindNewLine = true;
                }
                else if (FindNewLine == true)
                {
                    FindEndIndex = i + 1;

                    string parcing = _CsvText.Substring(FindEndIndex, FindEndIndex - FindStartIndex);

                    parcing = parcing.Replace("\"", "");
                    parcing = parcing.Replace("\n", "\\z");
                    list.Add(parcing);
                    FindNewLine = false;
                }
            }
        }

        if (list.Count > 0)
        {
            list.Add(_CsvText.Substring(FindEndIndex, _CsvText.Length - 1 - FindEndIndex));

            _CsvText = "";

            for (int i = 0; i < list.Count; ++i)
            {
                _CsvText += list[i];
            }
        }

        System.String[] lines = _CsvText.Split("\n"[0]);

        int width = 0;

        for (int i = 0; i < lines.Length; ++i)
        {
            System.String[] row = SplitCsvLine(lines[i]);
            width = UnityEngine.Mathf.Max(width, row.Length);
        }

        System.String[,] outputgrid = new System.String[width + 1, lines.Length + 1];

        for (int y = 0; y < lines.Length; y++)
        {
            lines[y] = lines[y].Replace("/,", "asdf!@#$");

            System.String[] row = SplitCsvLine(lines[y]);

            for (int x = 0; x < row.Length; x++)
            {
                row[x] = row[x].Replace("asdf!@#$", ",");

                outputgrid[x, y] = row[x];

                outputgrid[x, y] = outputgrid[x, y].Replace(@"\n", "\n");
                outputgrid[x, y] = outputgrid[x, y].Replace(@"\z", "\n");
                outputgrid[x, y] = outputgrid[x, y].Replace("\"\"", "\"");
            }
        }

        return outputgrid;
    }

    System.String[] SplitCsvLine(System.String _line)
    {
        return (from System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(_line, @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)", System.Text.RegularExpressions.RegexOptions.ExplicitCapture) select m.Groups[1].Value).ToArray();
    }
}
