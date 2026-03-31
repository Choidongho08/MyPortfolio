using Assets.Work.CDH.Code;
using UnityEditor;
using UnityEngine;

public class TableReload : MonoBehaviour
{
    [MenuItem("CS_Util/Table/CSV &F1", false, 1)]
    static public void ParserTableCsv()
    {
        Shared.TableMgr = new TableManager();
        Shared.TableMgr.Init();
        Shared.TableMgr.Save();
    }
}
