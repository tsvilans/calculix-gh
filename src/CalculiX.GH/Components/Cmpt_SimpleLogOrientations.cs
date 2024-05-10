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
    public class Cmpt_SimpleLogOrientations: GH_Component
    {
        public Cmpt_SimpleLogOrientations()
            : base ("Simple Log Orientations", "LOri", "Define element orientations according to a simplified timber log model.", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        Line[] linesL = null;
        Line[] linesR = null;
        double lineLength = 0.02; // meters

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Log axis", "LA", "Line representing central axis of the log.", GH_ParamAccess.item);
           
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
            GH_Structure<GH_Integer> elements;
            GH_Structure<GH_Point> nodes;

            DA.GetData("Log axis", ref logAxis);
            DA.GetDataTree(1, out nodes);
            DA.GetDataTree(2, out elements);

            if (logAxis == null) return;

            DataTree<GH_Plane> orientations = new DataTree<GH_Plane>();

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
                var l = logAxis.TangentAt(t);
                var r = sum - cp;

                var ori = new Plane(sum, l, r);

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
            get { return new Guid("49c3dc58-26a7-451e-b0b9-5553da491e1e"); }
        }
    }
}
