using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;

namespace Pipeflow
{
    public sealed class DWG
    {
        public Document AcDoc { get; set; }
        public Editor AcEd { get; set; }
        public Database AcDb { get; set; }

        // Our instance
        private static DWG instance;

        private DWG()
        {
            updateDwg();
        }

        public static DWG getInstance()
        {
            if (instance == null)
                instance = new DWG();

            return instance;
        }

        public void updateDwg()
        {
            AcDoc = Application.DocumentManager.MdiActiveDocument;
            AcEd = AcDoc.Editor;
            AcDb = AcDoc.Database;
        }
    }
}
