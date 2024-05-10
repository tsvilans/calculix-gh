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
using System.Drawing.Drawing2D;

namespace CalculiX.GH.Components
{
    public class Cmpt_NodeSetMesh : GH_Component
    {
        public Cmpt_NodeSetMesh()
            : base ("Node Set - Mesh Proximity", "NSetMesh", 
                  "Make a node set of all nodes within certain distance of mesh.", Api.ComponentCategory, "Sets")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of node set.", GH_ParamAccess.item, "Node Set");
            pManager.AddPointParameter("Nodes", "No", "Nodes as a tree.", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Mesh", "M", "Mesh to test for proximity.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "Maximum distance to test for proximity.", GH_ParamAccess.item, 0.1);

            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Node Set", "NS", "Resultant node set.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Node Set";
            GH_Structure<GH_Point> nodes;
            Mesh mesh = null;
            double maxDistance = 0.1;

            DA.GetData("Name", ref name);
            DA.GetDataTree(1, out nodes);
            DA.GetData("Mesh", ref mesh);
            DA.GetData("Distance", ref maxDistance);

            var nset = new NodeSet(name);
            
            foreach (var path in nodes.Paths)
            {
                var branch = nodes[path];
                if (branch.Count < 1) continue;

                int res = mesh.ClosestPoint(branch[0].Value, out Point3d cp, maxDistance);
                if (res >= 0) nset.Tags.Add(path.Indices[0]);
            }

            DA.SetData("Node Set", new GH_FeSet(nset));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.NodeSetMeshProximity;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("74e1e814-a227-442c-a665-5b6fe8b9859d"); }
        }
    }
}
