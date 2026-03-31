namespace Assets.Work.CDH.Code
{
    public class Shared
    {
        public static TableManager TableMgr;

        public static TableManager InitTableMgr()
        {
            if (TableMgr == null)
            {
                TableMgr = new TableManager();
                TableMgr.Init();
            }

            return TableMgr;
        }
    }
}
