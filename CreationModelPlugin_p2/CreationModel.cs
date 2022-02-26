using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin_p2
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;           

            Level level1 = SelectLevel(doc, "Уровень 1");
            Level level2 = SelectLevel(doc, "Уровень 1");         

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);

            double dx = width / 2;
            double dy = depth / 2;

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            {
                List<Wall> walls = CreateWall(doc, dx, dy, level1, level2);
                addDoor(doc, level1, walls[0]);
                addWindow(doc, level1, walls[1]);
                addWindow(doc, level1, walls[2]);
                addWindow(doc, level1, walls[3]);
            }            

            transaction.Commit();

            return Result.Succeeded;
        }

        private void addWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(f => f.Name.Equals("0406 x 0610 мм"))
                .Where(f => f.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ midpoint = (point1 + point2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();

            FamilyInstance window = doc.Create.NewFamilyInstance(midpoint, windowType, wall, level1, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            double height = UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(height);
        }

        private void addDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(f => f.Name.Equals("0915 x 2134 мм"))
                .Where(f => f.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ midpoint = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            FamilyInstance door = doc.Create.NewFamilyInstance(midpoint,doorType,wall,level1,Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }

        public Level SelectLevel(Document doc, string levelName)
        {
            List<Level> listLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level SelectLevel = listLevels
                .Where(x => x.Name.Equals(levelName))
                .OfType<Level>()
                .FirstOrDefault();
            return SelectLevel;
        }

        public List<Wall> CreateWall(Document doc, double dx, double dy, Level level1, Level level2)
        {
            List<Wall> walls = new List<Wall>();
            List<XYZ> points = new List<XYZ>();
            for (int i = 0; i < 4; i++)
            {
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                walls.Add(wall);
            }
            return walls;
        }


    }
}