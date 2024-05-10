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
using GH_IO.Serialization;
using Rhino;
using Grasshopper.Kernel.Types;

namespace CalculiX.GH.Components
{
    public class Cmpt_MeshBrep: GH_Component
    {
        public Cmpt_MeshBrep()
            : base ("Mesh Brep", "MeshBrep", "Mesh a solid Brep into 3D elements.", Api.ComponentCategory, "Model")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        string resultsPath = "";
        string[] simulationOutput = null;

        bool secondOrder = true;
        public double scale = 1.0; // assume SI units (meters)


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

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            pManager.AddBrepParameter("Brep", "B", "Brep to mesh.", GH_ParamAccess.list);
            pManager.AddNumberParameter("MeshSizeMin", "min", "Minimum element size.", GH_ParamAccess.item, 0.005 / scale);
            pManager.AddNumberParameter("MeshSizeMax", "max", "Maximum element size.", GH_ParamAccess.item, 0.05 / scale);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes for the mesh.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Indices for 3d volume elements.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Surfaces", "S", "Indices for 2d surface elements.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var breps = new List<Brep>();
            DA.GetDataList("Brep", breps);

            if (breps.Count < 1) return;

            var nameBase = "Brep";
            var totalVolume = 0.0;

            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var workingDirectory = Path.Combine(executingDirectory, "Temp");

            // 1. Export geometry as STEP
            var stp_options = new Rhino.FileIO.FileStpWriteOptions();
            stp_options.SplitClosedSurfaces = false;

            var stp_path = Path.Combine(workingDirectory, Api.DefaultOutputName + ".stp");
            var stp_doc = Rhino.RhinoDoc.CreateHeadless("");

            for (int i = 0; i < breps.Count; ++i)
            {
                var mvp = VolumeMassProperties.Compute(breps[i]);
                totalVolume += mvp.Volume;

                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = $"{nameBase}_{i:00}";
                attr.WireDensity = -1;

                stp_doc.Objects.AddBrep(breps[i], attr);
            }

            Rhino.FileIO.FileStp.Write(stp_path, stp_doc, stp_options);
            stp_doc.Dispose();

            // 1.9 Safety check - Get the rough size of the minimum allowable value for MeshSizeMax
            var minElementVolume = totalVolume / Api.MaxElementCount;
            var minElementEdgeSize = Math.Pow(minElementVolume * 8.485281374, 1.0/3.0);

            var meshSizeMin = 1.0;
            var meshSizeMax = 20.0;

            DA.GetData("MeshSizeMin", ref meshSizeMin);
            DA.GetData("MeshSizeMax", ref meshSizeMax);

            if (meshSizeMax < minElementEdgeSize)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Specified MeshSizeMax will generate a LOT of elements... "+
                    "Increasing size to limit" +
                    $" element count to < {Api.MaxElementCount} for your own good ;)");
            }

            meshSizeMax = Math.Max(meshSizeMax, minElementEdgeSize);

            // 2. Load into Gmsh and do meshing
            Gmsh.InitializeGmsh();

            Tuple<int, int>[] dimTags;
            Gmsh.Model.OCC.ImportShapes(stp_path, out dimTags, true, "stp");

            Gmsh.Model.OCC.Synchronize();

            Gmsh.Option.SetNumber("Mesh.MeshSizeMin", meshSizeMin);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMax", meshSizeMax);
            Gmsh.Option.SetNumber("Mesh.ElementOrder", secondOrder ? 2 : 1);
            Gmsh.Option.SetNumber("Mesh.SaveGroupsOfElements", -1001);
            Gmsh.Option.SetNumber("Mesh.SaveGroupsOfNodes", 2);

            Gmsh.Model.Generate(3);

            var nodes = new DataTree<GH_Point>();

            IntPtr[] nodeTags;
            double[] coords;

            // Get all nodes
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coords, -1, -1, true, false);

            for (int i = 0; i < nodeTags.Length; i++)
            {
                //nodeMap[nodeTags[i]] = i;

                var ii = i * 3;
                var node = new Point3d(coords[ii], coords[ii + 1], coords[ii + 2]);
                var path = new GH_Path((int)nodeTags[i]);

                if (nodes.PathExists(path)) continue;

                nodes.Add(new GH_Point(node), new GH_Path((int)nodeTags[i]));
            }

            int[] elementTypes;
            IntPtr[][] elementTags, elementNodeTags;


            var elements = new DataTree<GH_Integer>();

            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 3, -1);

            for (int i = 0; i < elementTypes.Length; ++i)
            {
                if (elementTypes[i] == 4)
                {
                    for (int j = 0; j < elementTags[i].Length; ++j)
                    {
                        var ii = j * 4;
                        var path = new GH_Path((int)elementTags[i][j]);
                        for (int k = 0; k < 4; ++k)
                            elements.Add(new GH_Integer((int)elementNodeTags[i][ii + k]), path);
                    }
                }
                else if (elementTypes[i] == 11)
                {
                    for (int j = 0; j < elementTags[i].Length; ++j)
                    {
                        var ii = j * 10;
                        var path = new GH_Path((int)elementTags[i][j]);
                        for (int k = 0; k < 10; ++k)
                            elements.Add(new GH_Integer((int)elementNodeTags[i][ii + k]), path);
                    }
                }
            }

            // Get surfaces
            Gmsh.Model.GetEntities(out dimTags, 2);

            var surfaces = new DataTree<GH_Integer>();
            foreach (var dimTag in dimTags)
            {
                var tag = dimTag.Item2;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 2, tag);

                for (int i = 0; i < elementTypes.Length; ++i)
                {
                    var nNodes = 0;
                    switch(elementTypes[i])
                    {
                        case (2):
                            nNodes = 3;
                            break;
                        case (3):
                            nNodes = 4;
                            break;
                        case (9):
                            nNodes = 6;
                            break;
                        case (10):
                            nNodes = 9;
                            break;
                        case (16):
                            nNodes = 8;
                            break;
                        default:
                            throw new Exception($"Unkown element type: {elementTypes[i]}");
                    }

                    for (int j = 0; j < elementTags[i].Length; ++j)
                    {
                        var ii = j * nNodes;
                        var path = new GH_Path(new int[] { tag, (int)elementTags[i][j] });
                        for (int k = 0; k < nNodes; ++k)
                            surfaces.Add(new GH_Integer((int)elementNodeTags[i][ii + k]), path);
                    }

                }
            }

            Gmsh.FinalizeGmsh();

            DA.SetDataTree(0, nodes);
            DA.SetDataTree(1, elements);
            DA.SetDataTree(2, surfaces);
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
            get { return new Guid("93b16a01-9eb6-49c3-a883-cb18037d2894"); }
        }
    }
}
