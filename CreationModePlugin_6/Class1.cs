using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CreationModePlugin_6
{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        private static int i;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document; // ссылка на документ

            Level level1, level2;
            TakeLevels(doc, out level1, out level2);
            CreateWalls(doc, level1, level2);
            return Result.Succeeded;
        }

        private static void CreateWalls(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters); // ширина
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters); // глубина
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>(); // массив точек
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>(); // массив в кт создаем стены

            Transaction ts = new Transaction(doc, "Построение стен"); // внутри транзакции помещаем цикл кт будет создавать стены
            ts.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]); // отрезок
                Wall wall = Wall.Create(doc, line, level1.Id, false); // стена
                walls.Add(wall); // массив стен
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id); // привязываем стену к уровню 2 (с помощью WALL_HEIGHT_TYPE)
            }

            CreateDoor(doc, level1, walls[0]);
            CreateWindow(doc, level1, walls[1]);
            CreateWindow(doc, level1, walls[2]);
            CreateWindow(doc, level1, walls[3]);
            AddRoof(doc, level2, walls);

            ts.Commit();
        }

        private static void AddRoof(Document doc, Level level2, List<Wall> walls) // создаем крышу
        {
            RoofType roofType = new FilteredElementCollector(doc) //крыша
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            //            double wallWidth = walls[0].Width; 
            //            double dw = wallWidth / 2; 
            //            List<XYZ> points = new List<XYZ>(); 
            //            points.Add(new XYZ(-dw, -dw, 0));
            //            points.Add(new XYZ(dw, -dw, 0));
            //            points.Add(new XYZ(dw, dw, 0));
            //            points.Add(new XYZ(-dw, dw, 0));
            //            points.Add(new XYZ(-dw, -dw, 0));

            //            Application application = doc.Application;
            //            CurveArray footprint = application.Create.NewCurveArray();
            //            for (int i = 0; i < 4; i++)
            //            {
            //                LocationCurve curve = walls[i].Location as LocationCurve;
            //                XYZ p1 = curve.Curve.GetEndPoint(0);  
            //                XYZ p2 = curve.Curve.GetEndPoint(1);
            //                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]); 
            //                footprint.Append(line);
            //            }
            //            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            //            FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2,
            //            roofType, out footPrintToModelCurveMapping);

            //            foreach (ModelCurve m in footPrintToModelCurveMapping)
            //            {
            //                footprintRoof.set_DefinesSlope(m, true);
            //                footprintRoof.set_SlopeAngle(m, 0.5);
            //            }
             
            // Плоская крыша до середины стены

            //Application application = doc.Application;
            //CurveArray footprint = application.Create.NewCurveArray();

            //for (int i=0; i<4; i++)
            //{
            //    LocationCurve curve = walls[i].Location as LocationCurve;
            //    footprint.Append(curve.Curve);
            //}
            //ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            //FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, 
            //    out footPrintToModelCurveMapping);

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-16.73, -8.53, 13.12), new XYZ(-16.73, 0, 19.69)));
            curveArray.Append(Line.CreateBound(new XYZ(-16.73, 0, 19.69), new XYZ(-16.73, 8.53, 13.12)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20),
                new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, -16.73, 16.73);
        }


        private static void CreateWindow(Document doc, Level level1, Wall wall) //создаем окна
        {
            FamilySymbol winType = new FilteredElementCollector(doc) //окна
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!winType.IsActive)
                winType.Activate();

            var window = doc.Create.NewFamilyInstance(point, winType, wall, level1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(900, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }


        private static void CreateDoor(Document doc, Level level1, Wall wall)  //создаем дверь
        {
            FamilySymbol doorType = new FilteredElementCollector(doc) //дверь
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors) 
                .OfType<FamilySymbol>() 
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

           
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        private static void TakeLevels(Document doc, out Level level1, out Level level2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level)) 
                .OfType<Level>()
                .ToList();

            level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
        }
    }
}
