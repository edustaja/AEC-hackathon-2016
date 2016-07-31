using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace RealBIM
{
    public class Worker
    {
        public void WriteInstallOrder()
        {
            Picker p = new Picker();
            int value = 30;
            while (true)
            {
                try
                {
                    ModelObject m = p.PickObject(Picker.PickObjectEnum.PICK_ONE_PART);
                    m.SetUserProperty(UDA_ORDER, ++value);
                }

                catch
                {
                    break;
                }
            }

            new Model().CommitChanges("Build order updated");

        }

        // Define other methods and classes here

        const string UDA_ORDER = "SW_BUILD_ORDER";
        const string DATA_FILE = @"C:\Users\FIJYRT\Desktop\Hackaton\ids.json";
        const string CAMERA_OVERIEW_DATA = @"C:\Users\FIJYRT\Desktop\Hackaton\cameraOverview.json";

        public void AnimateBuild()
        {
            var m = new Model();

            var allBeams = ReadGuidFromJson().Select(g => m.SelectModelObject(new Tekla.Structures.Identifier(g))).Where(o => o != null).ToList();

            ModelObjectVisualization.SetTemporaryState(allBeams, new Color(0, 0, 0, 0.1));

            var order = allBeams.ToDictionary(b => b, b =>
            {
                int bValue = 0;
                if (b.GetUserProperty(UDA_ORDER, ref bValue) == false) return int.MaxValue;

                return bValue;
            });

            var orderedBeams = allBeams.Where(o => order[o] < int.MaxValue).OrderBy(o => order[o]);

            foreach (ModelObject o in orderedBeams)
            {
                for (double d = 0; d <= 0.8; d += 0.1d)
                {
                    ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { o }, new Color(0, 0.5, 0.5, d));
                    Thread.Sleep(10);
                }
            }

            //for (int i = 0; i < 100; i++)
            //{
            //    ModelObject mo = allBeams.FirstOrDefault(o =>
            //    {
            //        int bValue = 0;
            //        if (o?.GetUserProperty(UDA_ORDER, ref bValue) == false) return false;

            //        return bValue == i;
            //    });

            //    if (mo == null) continue;

            //    for (double d = 0; d <= 0.8; d += 0.1d)
            //    {
            //        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { mo }, new Color(0, 0.5, 0.5, d));
            //        Thread.Sleep(1);
            //    }
            //}


            ModelObjectVisualization.ClearAllTemporaryStates();
        }

        public void RestoreCamera(string file)
        {
            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            ViewCamera c = new ViewCamera();
            c.View = view;

            c.Select();

            var cameraData = JsonConvert.DeserializeObject<CameraData>(File.ReadAllText(file));

            c.DirectionVector = cameraData.Direction;
            c.UpVector = cameraData.Up;
            c.Location = cameraData.Location;
            c.ZoomFactor = cameraData.ZoomFactor;

            c.Modify();

        }

        public void ZoomToPart(Guid guid, double zf)
        {
            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            ViewCamera c = new ViewCamera();
            c.View = view;

            c.Select();

            var m = new Model();
            Part p = (Part)m.SelectModelObject(new Tekla.Structures.Identifier(guid));

            double x = 0;
            double y = 0;
            double z = 0;

            p.GetReportProperty("COG_X", ref x);
            p.GetReportProperty("COG_Y", ref y);
            p.GetReportProperty("COG_Z", ref z);

            Point target = new Point(x, y, z);

            Vector direction = new Vector(target - c.Location);
            direction.Normalize();
            c.DirectionVector = direction;
            Vector side = direction.Cross(new Vector(0, 0, 1));
            c.UpVector = side.Cross(direction);
            c.UpVector.Normalize();

            double val = c.DirectionVector.Dot(c.UpVector);

            c.Modify();

            double zoomFator = c.ZoomFactor;

            for (double d = 1; zoomFator * d > zf; d -= 0.01)
            {
                c.ZoomFactor = zoomFator * d;
                c.Modify();

                Thread.Sleep(10);
            }
        }
        public void TurnToPart(Guid guid, int steps)
        {
            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            ViewCamera c = new ViewCamera();
            c.View = view;

            c.Select();

            var m = new Model();
            Part p = (Part)m.SelectModelObject(new Tekla.Structures.Identifier(guid));

            double x = 0;
            double y = 0;
            double z = 0;

            p.GetReportProperty("COG_X", ref x);
            p.GetReportProperty("COG_Y", ref y);
            p.GetReportProperty("COG_Z", ref z);

            Point target = new Point(x, y, z);

            Vector direction = new Vector(target - c.Location);
            direction.Normalize();

            double val = c.DirectionVector.Dot(c.UpVector);

            Vector originalDirection = c.DirectionVector;



            double diff = 1D / (double)steps;
            double f = 0;
            while (f < 1)
            {
                f += diff;

                Vector d = new Vector((1 - f) * originalDirection + f * direction);
                d.Normalize();
                c.DirectionVector = d;
                Vector side = d.Cross(new Vector(0, 0, 1));
                c.UpVector = side.Cross(d);
                c.UpVector.Normalize();
                c.Modify();

                Thread.Sleep(50);
            }

            c.Modify();

        }

        public void ZoomOverview()
        {
            RestoreCamera(CAMERA_OVERIEW_DATA);
            RestoreCamera(CAMERA_OVERIEW_DATA);
        }

        public void WriteCameraData()
        {
            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            ViewCamera c = new ViewCamera();
            c.View = view;

            c.Select();

            File.WriteAllText(CAMERA_OVERIEW_DATA, JsonConvert.SerializeObject(new CameraData
            {
                Up = c.UpVector,
                Direction = c.DirectionVector,
                Location = c.Location,
                ZoomFactor = c.ZoomFactor
            }));

        }



        public void HighlightPartWithNeigbours(Guid id)
        {
            var m = new Model();

            var allBeams = ReadGuidFromJson().Select(g => m.SelectModelObject(new Tekla.Structures.Identifier(g))).Where(p => p != null).ToList();

            ModelObject hBeam = m.SelectModelObject(new Tekla.Structures.Identifier(id));

            int value = 0;
            hBeam.GetUserProperty(UDA_ORDER, ref value);

            ModelObjectVisualization.SetTemporaryState(allBeams, new Color(0, 0, 0, 0.1));

            Solid oSolid = ((Part)hBeam).GetSolid();
            Vector tolerance = new Vector(oSolid.MaximumPoint - oSolid.MinimumPoint) * 0.3;
            AABB bb = new AABB(oSolid.MinimumPoint - tolerance, oSolid.MaximumPoint + tolerance);

            var order = allBeams.ToDictionary(b => b, b =>
            {
                int bValue = 0;
                if (b.GetUserProperty(UDA_ORDER, ref bValue) == false) return int.MaxValue;

                return bValue;
            });


            var nearBeamsInOrder = allBeams.Where(p =>
            {
                if (p == null || !(p is Part)) return false;
                Solid sSolid = ((Part) p).GetSolid();
                AABB bb2 = new AABB(sSolid.MinimumPoint, sSolid.MaximumPoint);

                int d = 0;
                if (!p.GetUserProperty(UDA_ORDER, ref d)) return false;

                return bb.Collide(bb2);
            }).OrderBy(p => order[p]).ToList();

            var draw = new GraphicsDrawer();

            foreach (ModelObject modelObject in nearBeamsInOrder)
            {

                double x = 0;
                double y = 0;
                double z = 0;

                if (!modelObject.GetReportProperty("COG_X", ref x) ||
                !modelObject.GetReportProperty("COG_Y", ref y) ||
                !modelObject.GetReportProperty("COG_Z", ref z))
                    Console.WriteLine();

                Point target = new Point(x, y, z);

                int udaValue = 0;

                if (modelObject.GetUserProperty(UDA_ORDER, ref udaValue))
                {
                    draw.DrawText(target, udaValue.ToString(), new Color(0, 0, 0));
                }

                if (modelObject.Identifier.GUID == hBeam.Identifier.GUID)
                {
                    for (double d = 0; d <= 1; d += 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> {modelObject},
                            new Color(1, 0, 0, d));
                        Thread.Sleep(50);
                    }

                    for (double d = 1; d >= 0; d -= 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> {modelObject},
                            new Color(1, 0, 0, d));
                        Thread.Sleep(50);
                    }

                    for (double d = 0; d <= 1; d += 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { modelObject },
                            new Color(1, 0, 0, d));
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    for (double d = 0; d <= 1; d += 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { modelObject }, new Color(0.5, 0.5, 0.5, d));
                        Thread.Sleep(50);
                    }

                    for (double d = 1; d >= 0; d -= 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { modelObject }, new Color(0.5, 0.5, 0.5, d));
                        Thread.Sleep(50);
                    }

                    for (double d = 0; d <= 1; d += 0.1d)
                    {
                        ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { modelObject }, new Color(0.5, 0.5, 0.5, d));
                        Thread.Sleep(50);
                    }
                }

            }

            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            view.WorkArea = new AABB(view.WorkArea);
            view.Modify();


            ModelObjectVisualization.ClearAllTemporaryStates();
        }

        public void HighlightPartWithInstalled(Guid id, int count)
        {
            var m = new Model();

            var allBeams = ReadGuidFromJson().Select(g => m.SelectModelObject(new Tekla.Structures.Identifier(g))).ToList();

            ModelObject hBeam = m.SelectModelObject(new Tekla.Structures.Identifier(id));

            allBeams.Remove(hBeam);

            int value = 0;
            hBeam.GetUserProperty(UDA_ORDER, ref value);

            ModelObjectVisualization.SetTemporaryState(allBeams, new Color(0, 0, 0, 0.1));

            Solid oSolid = ((Part)hBeam).GetSolid();
            Vector tolerance = new Vector(oSolid.MaximumPoint - oSolid.MinimumPoint) * 0.1;
            AABB bb = new AABB(oSolid.MinimumPoint - tolerance, oSolid.MaximumPoint + tolerance);

            ModelObjectVisualization.SetTemporaryState(allBeams.Where(p =>
            {
                if (p == null) return false;
                int v = 0;
                if (!p.GetUserProperty(UDA_ORDER, ref v)) return false;
                return v < value;
            }).ToList(), new Color(0, 0.5, 0.5, 0.5));

            ModelObjectVisualization.SetTemporaryState(allBeams.Where(p =>
            {
                if (p == null || !(p is Part)) return false;
                Solid sSolid = ((Part)p).GetSolid();
                AABB bb2 = new AABB(sSolid.MinimumPoint, sSolid.MaximumPoint);
                return bb.Collide(bb2);
            }).ToList(), new Color(0.0, 0.0, 0.5, 0.2));

            for (int i = 0; i < count; i++)
            {
                for (double d = 0; d <= 1; d += 0.1d)
                {
                    ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { hBeam }, new Color(1, 0, 0, d));
                    Thread.Sleep(50);
                }

                for (double d = 1; d >= 0; d -= 0.1d)
                {
                    ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { hBeam }, new Color(1, 0, 0, d));
                    Thread.Sleep(50);
                }

            }

            ModelObjectVisualization.ClearAllTemporaryStates();
        }

        public void HighlightPart(Guid id, int count)
        {
            var m = new Model();

            var allBeams = ReadGuidFromJson().Select(g => m.SelectModelObject(new Tekla.Structures.Identifier(g))).ToList();

            ModelObject hBeam = m.SelectModelObject(new Tekla.Structures.Identifier(id));

            allBeams.Remove(hBeam);


            ModelObjectVisualization.SetTemporaryState(allBeams, new Color(0, 0, 0, 0.1));


            for (int i = 0; i < count; i++)
            {
                for (double d = 0; d <= 1; d += 0.1d)
                {
                    ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { hBeam }, new Color(1, 0, 0, d));
                    Thread.Sleep(50);
                }

                for (double d = 1; d >= 0; d -= 0.1d)
                {
                    ModelObjectVisualization.SetTemporaryState(new List<ModelObject> { hBeam }, new Color(1, 0, 0, d));
                    Thread.Sleep(50);
                }

            }

            ModelObjectVisualization.ClearAllTemporaryStates();
        }

        public void WriteGuidFromSelected()
        {
            var all = new Tekla.Structures.Model.UI.ModelObjectSelector().GetSelectedObjects();
            all.SelectInstances = true;
            var allBeams = new List<ModelObject>();

            foreach (ModelObject b in all)
                allBeams.Add(b);

            File.WriteAllText(DATA_FILE, JsonConvert.SerializeObject(allBeams.Select(b => b.Identifier.GUID).ToList()));

        }

        public List<Guid> ReadGuidFromJson()
        {
            return JsonConvert.DeserializeObject<List<Guid>>(File.ReadAllText(DATA_FILE));
        }

        public class CameraData
        {
            public Tekla.Structures.Geometry3d.Point Location { get; set; }
            public Tekla.Structures.Geometry3d.Vector Direction { get; set; }
            public Tekla.Structures.Geometry3d.Vector Up { get; set; }
            public double ZoomFactor { get; set; }
        }

        public void RotateCamera(Guid guid)
        {
            Part p = (Part)new Model().SelectModelObject(new Tekla.Structures.Identifier(guid));

            double x = 0;
            double y = 0;
            double z = 0;

            p.GetReportProperty("COG_X", ref x);
            p.GetReportProperty("COG_Y", ref y);
            p.GetReportProperty("COG_Z", ref z);

            Point target = new Point(x, y, z);

            var visibleViews = Tekla.Structures.Model.UI.ViewHandler.GetVisibleViews();
            visibleViews.MoveNext();
            var view = visibleViews.Current;

            ViewCamera c = new ViewCamera();
            c.View = view;

            c.Select();

            double radius = Distance.PointToPoint(c.Location, target);
            double startAngle = Math.Atan(c.DirectionVector.Y / c.DirectionVector.X);

            Point origin = target;

            //double zDirection = c.DirectionVector.Z;
            double zPosition = c.Location.Z;

            for (double a = 0; a < Math.PI * 2; a += Math.PI / 20)
            {
                double angle = startAngle + a;

                if (angle > Math.PI * 2)
                    angle -= Math.PI * 2;

                Point position = origin - new Vector(Math.Cos(angle) * radius, Math.Sin(angle) * radius, 0);

                position.Z = zPosition;

                Vector direction = new Vector(origin - position);
                direction.Normalize();

                c.Location = position;
                c.DirectionVector = direction;
                Vector side = direction.Cross(new Vector(0, 0, 1));
                c.UpVector = side.Cross(direction);
                c.UpVector.Normalize();
                c.Modify();



                Thread.Sleep(50);

            }
            c.Modify();

        }
    }
}
