﻿/*
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

namespace CalculiX.GH.Components
{
    public class Cmpt_Deformations: GH_Component
    {
        public Cmpt_Deformations()
            : base ("Deform", "Def", "Get model deformations.", Api.ComponentCategory, "Results")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Factor", "F", "Deformation multiplication factor.", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Faces", "F", "Visualization faces as a DataTree.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Deformed mesh.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vectors", "V", "Deformation vectors.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;
            double displacementFactor = 1.0;
            GH_Structure<GH_Integer> faces;

            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }
            DA.GetData("Factor", ref displacementFactor);
            DA.GetDataTree(2, out faces);

            var nodeMap = new Dictionary<int, Point3d>();
            var distortedNodeMap = new Dictionary<int, Point3d>();


            for (int i = 0; i < results.Nodes.Count; ++i)
            {
                var node = results.Nodes[i];
                nodeMap[node.Id] = new Point3d(node.X, node.Y, node.Z);
            }

            Vector3d[] displacementVectors = null;

            if (results.Fields.ContainsKey("DISP"))
            {
                displacementVectors = Utility.GetVectors(results, "DISP", "D1", "D2", "D3");

                for (int i = 0; i < results.Nodes.Count; ++i)
                {
                    distortedNodeMap[results.Nodes[i].Id] = nodeMap[results.Nodes[i].Id] + displacementVectors[i] * displacementFactor;
                }
            }

            var mesh = Utility.CreateShellMesh(distortedNodeMap, faces);

            DA.SetData("Mesh", mesh);
            DA.SetDataList("Vectors", displacementVectors);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("e2117fba-5c1d-4312-80b1-2454a81972d7"); }
        }
    }
}
