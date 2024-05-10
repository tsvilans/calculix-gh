/*
 * Calculix.GH
 * Copyright 2024 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

using Grasshopper.Kernel;
using System.IO;
using System.Reflection;

using FrdReader;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace CalculiX.GH.Components
{
    public class Cmpt_LogKnotOrientations : GH_Component
    {
        public Cmpt_LogKnotOrientations()
            : base ("Log Orientations", "LOri", "Define element orientations according to a timber log model including knots.", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        Line[] linesL = null;
        Line[] linesR = null;
        double lineLength = 0.02; // meters
        double defaultKnotRadius = 0.007; // meters

        double TransitionZoneFactor = 1.5;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Log axis", "LA", "Line representing central axis of the log.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Knot axes", "KA", "Lines representing knot axes.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Knot radii", "KR", "Radii of each knot.", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[2].Optional = true;

            pManager.AddPointParameter("Nodes", "N", "Nodes as tree.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Elements as a DataTree.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Orientations", "O", "Orientations as planes.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var lineLengthActual = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem) * lineLength;


            Curve logAxis = null;
            List<Curve> knotsAxes = new List<Curve>();
            List<double> knotsRadii = new List<double>();
            GH_Structure<GH_Integer> elements;
            GH_Structure<GH_Point> nodes;

            DA.GetData("Log axis", ref logAxis);
            DA.GetDataList("Knot axes", knotsAxes);
            DA.GetDataList("Knot radii", knotsRadii);

            int nKnots = knotsAxes.Count;
            if (knotsRadii.Count > 0)
            {
                nKnots = (int)Math.Min(knotsRadii.Count, knotsAxes.Count);
            }
            else if (nKnots > 0)
            {
                knotsRadii = Enumerable.Repeat(RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem) * defaultKnotRadius, nKnots).ToList();
            }

            for (int i = 0; i < nKnots; ++i)
            {
                knotsAxes[i].Domain = new Interval(0, 1);
            }

            DA.GetDataTree(3, out nodes);
            DA.GetDataTree(4, out elements);

            if (logAxis == null) return;

            DataTree<GH_Plane> orientations = new DataTree<GH_Plane>();

            // 0. Construct log model
            // TODO



            // 1. Get element centroids
            var centroids = new DataTree<Point3d>();
            var samplePoints = new List<Point3d>();

            linesL = new Line[elements.Paths.Count];
            linesR = new Line[elements.Paths.Count];
            int counter = 0;

            foreach (GH_Path path in elements.Paths)
            {
                var elementNodes = elements[path].Select(x => nodes[new GH_Path(x.Value)][0].Value).ToArray();
                Point3d sum = Point3d.Origin;

                foreach (Point3d en in elementNodes)
                {
                    sum = sum + en;
                }

                sum = sum / elementNodes.Length;

                // 2. Construct orientation
                logAxis.ClosestPoint(sum, out double t);
                var cp = logAxis.PointAt(t);
                var ll = logAxis.TangentAt(t);
                var lr = sum - cp;

                // 3. Check against knot list
                double weightSum = 0;
                Vector3d lSum = Vector3d.Zero;
                Vector3d rSum = Vector3d.Zero;

                for (int i = 0; i < nKnots; ++i)
                {
                    double tt = 0;
                    var kaxis = knotsAxes[i];
                    var r = knotsRadii[i];

                    if (!kaxis.ClosestPoint(sum, out double kt, r * TransitionZoneFactor))
                        continue;

                    var k0 = r * kt; // kt should be normalized between 0 and 1
                    var k1 = k0 * TransitionZoneFactor;

                    var kcp = kaxis.PointAt(kt);

                    var kd = sum.DistanceTo(kcp);
                    if (kd < k0) tt = 1.0;
                    else if (kd > k1) continue;
                    else
                        tt = (kd - k0) / (k1 - k0);

                    var kl = kaxis.TangentAt(kt);
                    var kr = sum - kcp;

                    ll = ll * (1 - tt) + kl * tt;
                    lr = lr * (1 - tt) + kr * tt;
                    break;

                    //weightSum += tt;
                    //lSum += kl;
                    //rSum += kr;
                }

                // Assume single knot interaction right now
                //if (weightSum > 0.0)
                //{
                //    ll += lSum / weightSum;
                //    lr += rSum / weightSum;
                //}

                var ori = new Plane(sum, ll, lr);

                orientations.Add(new GH_Plane(ori), path);
                linesL[counter] = new Line(sum, ori.XAxis, lineLengthActual);
                linesR[counter] = new Line(sum, ori.YAxis, lineLengthActual);
                counter++;
            }

            DA.SetDataTree(0, orientations);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (linesL != null)
                args.Display.DrawLines(linesL, System.Drawing.Color.Red, 1);
            if (linesR != null)
            args.Display.DrawLines(linesR, System.Drawing.Color.Lime, 1);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Orientations_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("796c5453-0cd0-41eb-8170-767daca73bf5"); }
        }
    }
}
