
using Assets.Work.CDH.Code.Table;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TableManager
{
    public Table_Map Map = new();
    public Table_SecurityLevel SecurityLevel = new();

    public void Init()
    {
#if UNITY_EDITOR
        Map.Init_Csv("TestMap", 0, 0);
        SecurityLevel.Init_Csv("SecurityLevelDocument", 1, 0);
#else
        Map.Init_Binary("TestMap");
        SecurityLevel.Init_Binary("SecurityLevelDocument");
#endif
    }

    public void Save()
    {
        Map.Save_Binary("TestMap");
        SecurityLevel.Save_Binary("SecurityLevelDocument");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}
