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
    public class Cmpt_MeshShell : GH_Component
    {
        public Cmpt_MeshShell()
            : base ("Mesh Shell", "MeshShell", "Mesh a closed shell mesh into 3D elements.", Api.ComponentCategory, "Model")
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

            pManager.AddBrepParameter("Mesh", "M", "Shell mesh to mesh.", GH_ParamAccess.list);
            pManager.AddNumberParameter("MeshSizeMin", "min", "Minimum element size.", GH_ParamAccess.item, 0.005 / scale);
            pManager.AddNumberParameter("MeshSizeMax", "max", "Maximum element size.", GH_ParamAccess.item, 0.05 / scale);
            pManager.AddBooleanParameter("Remesh", "R", "Remesh mesh geometry.", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes for the mesh.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Element indices for the mesh.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double size_min = 10.0, size_max = 100.0;
            bool create_geometry = false;

            DA.GetData("MeshSizeMin", ref size_min);
            DA.GetData("MeshSizeMax", ref size_max);
            DA.GetData("Remesh", ref create_geometry);

            Mesh mesh = null;

            DA.GetData("Mesh", ref mesh);

            if (mesh == null) return;

            Gmsh.InitializeGmsh();
            Gmsh.Clear();
            Gmsh.Logger.Start();

            var mesh_id = -1;

            // Add mesh data
            try
            {
                mesh_id = GmshCommon.GeometryExtensions.TransferMesh(mesh, create_geometry);
            }
            catch (Exception e)
            {
                string msg = Gmsh.Logger.GetLastError();

                var log = Gmsh.Logger.Get();
                foreach (string l in log)
                    msg += String.Format("\n    {0}", log);

                throw new Exception(msg);
            }

            if (mesh_id < 0) return;

            // Get 2D entities (the mesh we just transferred)
            Tuple<int, int>[] surfaceTags;
            Gmsh.Model.GetEntities(out surfaceTags, 2);

            var loop = Gmsh.Model.Geo.AddSurfaceLoop(surfaceTags.Select(x => x.Item2).ToArray());

            //var loop = Gmsh.Geo.AddSurfaceLoop(new int[] { mesh_id });
            var vol = Gmsh.Model.Geo.AddVolume(new int[] { loop });

            Gmsh.Model.Geo.Synchronize();

            // Set mesh sizes
            Gmsh.Option.SetNumber("Mesh.MeshSizeMin", size_min);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMax", size_max);

            Gmsh.Option.SetNumber("Mesh.SaveAll", 0);
            Gmsh.Option.SetNumber("Mesh.SaveGroupsOfElements", -1001);
            Gmsh.Option.SetNumber("Mesh.SaveGroupsOfNodes", 2);
            Gmsh.Option.SetNumber("Mesh.ElementOrder", secondOrder ? 2 : 1);

            // Generate mesh
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

            Gmsh.FinalizeGmsh();

            DA.SetDataTree(0, nodes);
            DA.SetDataTree(1, elements);

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
            get { return new Guid("2726fc53-dc82-451f-9f6f-391168c3908e"); }
        }
    }
}
