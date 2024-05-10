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
using System.Xml.Linq;
using GmshCommon;
using Grasshopper.Kernel.Data;
using Grasshopper;
using static GmshCommon.Gmsh;
using GH_IO.Serialization;
using Rhino;

namespace CalculiX.GH.Components
{
    public class Cmpt_MeshCrv: GH_Component
    {
        public Cmpt_MeshCrv()
            : base ("Mesh Curves", "MeshCrv", "Mesh a series of curves into 1D elements.", Api.ComponentCategory, "Model")
        {
        }

        string resultsPath = "";
        string[] simulationOutput = null;

        public bool secondOrder = true;
        public double scale = 1.0; // assume SI units (meters)

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            pManager.AddCurveParameter("Curves", "C", "Curves to mesh.", GH_ParamAccess.list);
            //pManager.AddNumberParameter("MeshSizeMin", "min", "Minimum element size.", GH_ParamAccess.item, 0.5 / scale);
            pManager.AddNumberParameter("Max length", "max", "Maximum element size.", GH_ParamAccess.item, 4 / scale);
            pManager.AddBooleanParameter("Do intersections", "X", "Find and add intersection points between curves.", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Merge threshold", "T", "Nodes that are less than this distance apart will be merged.", GH_ParamAccess.item, 0.001 / scale);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
            //pManager[3].Optional = true;
            //pManager[4].Optional = true;
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        private void SetSecondOrder(object sender, EventArgs e)
        {
            secondOrder = !secondOrder;
            ExpireSolution(true);
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Second order", SetSecondOrder, true, secondOrder);

            base.AppendAdditionalComponentMenuItems(menu);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("secondOrder", secondOrder);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("secondOrder", ref secondOrder);
            return base.Read(reader);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes for the mesh.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Element indices for the mesh.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Parents", "P", "Parent index for each element.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);
            //Message = $"Scale: {scale}";

            var curves = new List<Curve>();
            DA.GetDataList("Curves", curves);
            if (curves.Count < 1) return;

            double mergeThreshold = 0.001 / scale;
            DA.GetData("Merge threshold", ref mergeThreshold);

            double maxLength = 0.5 / scale;
            DA.GetData("Max length", ref maxLength);

            if (curves.Count < 1) return;
            double searchRadius = mergeThreshold;

            // Break curves
            
            BreakCurves(curves, out List<Curve> segments, out List <int> parentIds, maxLength, 0.001 / scale);

            var nodes = new Dictionary<int, Point3d>();
            var elements = new Dictionary<int, int[]>();
            var parents = new Dictionary<int, int>();
            int nodeIndex = 1, elementIndex = 1;

            using (RTree rtree = new RTree())
            {
                for (int i = 0; i < segments.Count; ++i)
                {
                    var curve = segments[i];
                    if (curve == null) continue;

                    Point3d[] beamNodes;
                    if (secondOrder)
                        beamNodes = new Point3d[] { curve.PointAtStart, curve.PointAt(curve.Domain.Mid), curve.PointAtEnd };
                    else
                        beamNodes = new Point3d[] { curve.PointAtStart, curve.PointAtEnd };

                    var beamNodeIds = new int[beamNodes.Length];
                    bool found = false;
                    int foundId = 0;

                    for (int j = 0; j < beamNodes.Length; j++)
                    {
                        var res = rtree.Search(new Sphere(beamNodes[j], searchRadius), (object sender, RTreeEventArgs e) =>
                        {
                            found = true;
                            foundId = e.Id;
                            e.Cancel = true;
                        });

                        if (found)
                        {
                            beamNodeIds[j] = foundId;
                        }
                        else
                        {
                            rtree.Insert(beamNodes[j], nodeIndex);
                            beamNodeIds[j] = nodeIndex;
                            nodes.Add(nodeIndex, beamNodes[j]);
                            nodeIndex++;
                        }

                        found = false;
                        foundId = -1;
                    }

                    // Check for degenerate elements
                    bool degenerate = false;
                    for (int j = 1; j < beamNodeIds.Length; ++j)
                    {
                        if (beamNodeIds[j] == beamNodeIds[j - 1])
                        {
                            degenerate = true;
                            break;
                        }
                    }
                    if (degenerate)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Skipping degenerate element at index {elementIndex}, node {beamNodeIds[1]}");
                        continue;
                    }

                    /*
                    foreach (var kvp in nodes)
                    {
                        if (kvp.Value.DistanceTo(curve.PointAtStart) < mergeThreshold)
                        {
                            a = kvp.Key;
                            break;
                        }
                    }

                    if (a == -1)
                    {
                        rtree.Insert(curve.PointAtStart, nodeIndex);
                        nodes[nodeIndex] = curve.PointAtStart;
                        a = nodeIndex;
                        nodeIndex++;
                    }

                    if (secondOrder)
                    {
                        // Add middle node
                        rtree.Insert(curve.PointAt(curve.Domain.Mid), nodeIndex);

                        nodes[nodeIndex] = curve.PointAt(curve.Domain.Mid);
                        c = nodeIndex;

                        nodeIndex++;
                    }

                    foreach (var kvp in nodes)
                    {
                        if (kvp.Value.DistanceTo(curve.PointAtEnd) < mergeThreshold)
                        {
                            b = kvp.Key;
                            break;
                        }
                    }

                    if (b == -1)
                    {
                        rtree.Insert(curve.PointAtStart, nodeIndex);
                        nodes[nodeIndex] = curve.PointAtEnd;
                        b = nodeIndex;
                        nodeIndex++;
                    }
                    */


                    //if (secondOrder)
                    //    //elements[elementIndex] = new ElementB32(elementIndex, a, c, b);
                    //    elements[elementIndex] = new int[] { a, c, b };
                    //else
                    //    elements[elementIndex] = new int[] { a, b };
                    //    //elements[elementIndex] = new ElementB31(elementIndex, a, b);

                    parents[elementIndex] = parentIds[i];
                    elements[elementIndex] = beamNodeIds;
                    elementIndex++;
                }
            }

            var nodeTree = new DataTree<Point3d>();
            var elementTree = new DataTree<int>();
            var parentTree = new DataTree<int>();

            foreach (var kvp in nodes)
            {
                nodeTree.Add(kvp.Value, new GH_Path(kvp.Key));
            }

            foreach (var kvp in elements)
            {
                elementTree.AddRange(kvp.Value, new GH_Path(kvp.Key));
            }

            foreach (var kvp in parents)
            {
                parentTree.Add(kvp.Key, new GH_Path(kvp.Value));
                //parentTree.Add(kvp.Value, new GH_Path(kvp.Key));
            }

            DA.SetDataTree(0, nodeTree);
            DA.SetDataTree(1, elementTree);
            DA.SetDataTree(2, parentTree);
        }

        private void BreakCurves(List<Curve> curves, out List<Curve> segments, out List<int> parentIds, double maxLength, double maxDistance)
        {

            double epsilon = 0.01;

            // Explode complex curves and maintain parent relationships
            var firstParents = new List<int>();
            var exploded = new List<Curve>();

            for (int i = 0; i < curves.Count; ++i)
            {
                var explode = curves[i].DuplicateSegments();
                exploded.AddRange(explode);
                firstParents.AddRange(Enumerable.Repeat(i, explode.Length));
            }

            curves = exploded;


            // Get total length and check for insane number of elements
            double totalLength = 0.0;
            foreach (Curve curve in curves)
            {
                totalLength += curve.GetLength();
            }

            if (totalLength / maxLength > Api.MaxElementCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Number of element exceeds maximum of {Api.MaxElementCount}! "
                    + "Adjusting division length to keep things sane...");
                maxLength = totalLength / Api.MaxElementCount;
            }

            // Find all intersections between curve pairs
            var tt = new DataTree<double>();
            for (int i = 1; i < curves.Count; ++i)
            {
                Curve c0 = curves[i];
                if (c0 == null || c0.GetLength() < epsilon) continue;

                for (int j = 0; j < i; ++j)
                {
                    Curve c1 = curves[j];
                    if (c1 == null) continue;
                    if (c1.GetLength() < epsilon) continue;

                    var cxx = Rhino.Geometry.Intersect.Intersection.CurveCurve(c0, c1, maxDistance, maxDistance);
                    foreach (var cx in cxx)
                    {
                        tt.Add(cx.ParameterA, new GH_Path(i));
                        tt.Add(cx.ParameterB, new GH_Path(j));
                    }
                }
            }

            // Split curves according to division length
            for (int i = 0; i < curves.Count; ++i)
            {
                var path = new GH_Path(i);
                tt.EnsurePath(path);

                Curve c0 = curves[i];
                if (c0 == null || c0.GetLength() < epsilon) continue;

                var branch = tt.Branch(path);

                var nDivs = (int)Math.Ceiling(c0.GetLength() / maxLength);
                var divs = c0.DivideByCount(nDivs, false);

                for (int j = 0; j < divs.Length; ++j)
                {
                    bool valid = true;
                    for (int k = 0; k < branch.Count; ++k)
                    {
                        var lengthInterval = new Interval(branch[k], divs[j]);
                        lengthInterval.MakeIncreasing();

                        if (c0.GetLength(lengthInterval) < (maxLength / 2))
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
            }

            // Gather all segments into one list
            segments = new List<Curve>();
            parentIds = new List<int>();

            foreach (GH_Path path in tt.Paths)
            {
                var branch = tt.Branch(path);
                var index = path.Indices[0];
                Curve curve = curves[index];
                if (curve == null || curve.GetLength() < epsilon) continue;

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
                {
                    parentIds.Add(firstParents[path.Indices[0]]);
                    segments.Add(curve);
                }
                else
                {
                    var curveSegments = curve.Split(branch);
                    parentIds.AddRange(Enumerable.Repeat(firstParents[path.Indices[0]], curveSegments.Length));
                    segments.AddRange(curveSegments);
                }
            }

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
            get { return new Guid("9df88b30-bd7b-4539-9d69-fb9871bbe7a7"); }
        }
    }
}
