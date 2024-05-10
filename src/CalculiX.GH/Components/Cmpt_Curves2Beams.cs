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
using Grasshopper.Kernel.Types.Transforms;
using Rhino;

namespace CalculiX.GH.Components
{
    public class Cmpt_Curves2Beams: GH_Component
    {
        public Cmpt_Curves2Beams()
            : base ("Curves2Beams", "C2B", "Intersect and divide curves and output them as segments.", Api.ComponentCategory, "Model")
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        double scale = 1.0; // assume SI units (meters)

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Curves to intersect and discretize.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Length", "L", "Maximum length of segment.", GH_ParamAccess.item, 1.0 / scale);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Segments", "S", "Discretized segments.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            var tt = new DataTree<double>();

            double divLength = 1.0;
            DA.GetData("Length", ref divLength) ;

            double epsilon = 0.01;
            List<Curve> curves = new List<Curve>();

            DA.GetDataList("Curves", curves);

            // Get total length and check for insane number of elements
            double totalLength = 0.0;
            foreach (Curve curve in curves)
            {
                totalLength += curve.GetLength();
            }

            if (totalLength / divLength > Api.MaxElementCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Number of element exceeds maximum of {Api.MaxElementCount}! "
                    + "Adjusting division length to keep things sane...");
                divLength = totalLength / Api.MaxElementCount;
            }


            for (int i = 1; i < curves.Count; ++i)
            {
                Curve c0 = curves[i];
                if (c0 == null || c0.GetLength() < epsilon) continue;

                for (int j = 0; j < i; ++j)
                {
                    Curve c1 = curves[j];
                    if (c1 == null) continue;
                    if (c1.GetLength() < epsilon) continue;

                    var cxx = Rhino.Geometry.Intersect.Intersection.CurveCurve(c0, c1, 0.001, 0.001);
                    foreach (var cx in cxx)
                    {
                        tt.Add(cx.ParameterA, new GH_Path(i));
                        tt.Add(cx.ParameterB, new GH_Path(j));
                    }
                }
            }

            for (int i = 0; i < curves.Count; ++i)
            {
                Curve c0 = curves[i];
                if (c0 == null) continue;
                if (c0.GetLength() < epsilon) continue;

                var branch = tt.Branch(new GH_Path(i));

                var nDivs = (int)Math.Ceiling(c0.GetLength() / divLength);
                var divs = c0.DivideByCount(nDivs, false);

                for (int j = 0; j < divs.Length; ++j)
                {
                    bool valid = true;
                    for (int k = 0; k < branch.Count; ++k)
                    {
                        var lengthInterval = new Interval(branch[k], divs[j]);
                        lengthInterval.MakeIncreasing();

                        if (c0.GetLength(lengthInterval) < (divLength / 2))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        tt.Add(divs[j], new GH_Path(i));
                    }
                }

                //tt.AddRange(divs, new GH_Path(i));
            }

            var allCurves = new List<Curve>();

            foreach (GH_Path path in tt.Paths)
            {
                var branch = tt.Branch(path);
                var index = path.Indices[0];
                Curve curve = curves[index];
                if (curve == null) continue;
                if (curve.GetLength() < epsilon) continue;


                var kill = new bool[branch.Count];

                for (int i = 0; i < branch.Count; ++i)
                {
                    if (Math.Abs(branch[i] - curve.Domain.Min) < epsilon ||
                      Math.Abs(branch[i] - curve.Domain.Max) < epsilon)
                    {
                        kill[i] = true;
                    }
                }

                for (int i = branch.Count - 1; i >= 0; --i)
                {
                    if (kill[i]) branch.RemoveAt(i);
                }

                if (branch.Count < 1)
                    allCurves.Add(curve);
                else
                    allCurves.AddRange(curve.Split(branch));
            }

            DA.SetDataList("Segments", allCurves);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Default_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3ec981b6-fb44-466d-9c92-b6b3457377c6"); }
        }
    }
}
