using System;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;

namespace Pipeflow
{
    public class Main
    {
        const float DISTANCE = 0.2F;

        private static ObjectIdCollection allCurvesChecked = null;

        private static DWG currentDwg = DWG.getInstance();

        [CommandMethod("PipeFlow")]
        public static void PipeFlow()
        {
            currentDwg.updateDwg();

            allCurvesChecked = new ObjectIdCollection();

            PromptSelectionOptions obj = new PromptSelectionOptions();
            obj.MessageForAdding = "Selecione as polylines ou linhas de chegada:";

            PromptSelectionResult curve = GetCurve(obj);

            if (curve.Status != PromptStatus.OK)
                return;

            ObjectIdCollection objCollection = new ObjectIdCollection(curve.Value.GetObjectIds());


            using (Transaction trans = currentDwg.AcDb.TransactionManager.StartTransaction())
            {
                Recursion(objCollection);

                trans.Commit();
            }
        }

        private static void Recursion(ObjectIdCollection curves)
        {
            if (curves == null) return;

            foreach (ObjectId obj in curves)
            {
                Recursion(GetCloseCurve(obj));
            }
        }

        private static ObjectIdCollection GetCloseCurve(ObjectId currentCurve)
        {
            PromptSelectionResult res = currentDwg.AcEd.SelectCrossingPolygon(GetLineSelectPoints(currentCurve), GetLinesFilter());

            if (res.Status == PromptStatus.Error)
                return new ObjectIdCollection();

            Curve curve = currentCurve.GetObject(OpenMode.ForWrite) as Curve;

            curve.Color = ZwSoft.ZwCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 1);

            allCurvesChecked.Add(currentCurve);

            return RemoveInvalidLines(res, currentCurve);
        }

        private static Point3dCollection GetLineSelectPoints(ObjectId currentLine)
        {
            Point3dCollection polygonPoints = new Point3dCollection();

            using (Transaction trans = currentDwg.AcDoc.TransactionManager.StartTransaction())
            {
                Curve curve = trans.GetObject(currentLine, OpenMode.ForRead) as Curve;

                double angleInDegrees = 0;

                // Implementing 135

                angleInDegrees = 135 * (Math.PI / 180);

                polygonPoints.Add(new Point3d(curve.StartPoint.X + DISTANCE * Math.Cos(angleInDegrees), curve.StartPoint.Y + DISTANCE * Math.Sin(angleInDegrees), 0));

                // Implementing 45
                angleInDegrees = 45 * (Math.PI / 180);

                polygonPoints.Add(new Point3d(curve.StartPoint.X + DISTANCE * Math.Cos(angleInDegrees), curve.StartPoint.Y + DISTANCE * Math.Sin(angleInDegrees), 0));

                // Implementing -45
                angleInDegrees = -45 * (Math.PI / 180);

                polygonPoints.Add(new Point3d(curve.StartPoint.X + DISTANCE * Math.Cos(angleInDegrees), curve.StartPoint.Y + DISTANCE * Math.Sin(angleInDegrees), 0));

                // Implementing -135
                angleInDegrees = -135 * (Math.PI / 180);

                polygonPoints.Add(new Point3d(curve.StartPoint.X + DISTANCE * Math.Cos(angleInDegrees), curve.StartPoint.Y + DISTANCE * Math.Sin(angleInDegrees), 0));
            }


            return polygonPoints;
        }

        private static PromptSelectionResult GetCurve(PromptSelectionOptions obj)
        {
            return currentDwg.AcEd.GetSelection(obj, GetLinesFilter());
        }

        private static ObjectIdCollection RemoveInvalidLines(PromptSelectionResult currentSelection, ObjectId objCurrentLine)
        {
            ObjectIdCollection objCol = new ObjectIdCollection(currentSelection.Value.GetObjectIds());
            ObjectIdCollection finalResult = new ObjectIdCollection();

            objCol.Remove(objCurrentLine);

            using (Transaction trans = currentDwg.AcDoc.TransactionManager.StartTransaction())
            {
                Curve currentCurve = trans.GetObject(objCurrentLine, OpenMode.ForRead) as Curve;

                foreach (ObjectId id in objCol)
                {
                    Curve newCuve = trans.GetObject(id, OpenMode.ForRead) as Curve;

                    if (!(allCurvesChecked.Contains(id) || isStartPointMeting(currentCurve, newCuve)))
                        finalResult.Add(id);
                }
            }

            return finalResult;
        }

        private static bool isStartPointMeting(Curve currentLine, Curve newLine)
        {
            return currentLine.StartPoint.DistanceTo(newLine.StartPoint) < currentLine.StartPoint.DistanceTo(newLine.EndPoint);
        }

        private static SelectionFilter GetLinesFilter()
        {
            TypedValue[] typedValue = new TypedValue[5];

            typedValue.SetValue(new TypedValue((int)DxfCode.Operator, "<or"), 0);
            typedValue.SetValue(new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), 1);
            typedValue.SetValue(new TypedValue((int)DxfCode.Start, "LINE"), 2);
            typedValue.SetValue(new TypedValue((int)DxfCode.Start, "POLYLINE"), 3);
            typedValue.SetValue(new TypedValue((int)DxfCode.Operator, "or>"), 4);

            return new SelectionFilter(typedValue);
        }

    }
}
