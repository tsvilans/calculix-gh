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
    public class Cmpt_NodeSetCreate : GH_Component
    {
        public Cmpt_NodeSetCreate()
            : base ("Node Set - Create", "NSet", "Create a node set from a list of node IDs (tags).", Api.ComponentCategory, "Sets")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of node set.", GH_ParamAccess.item, "Node Set");
            pManager.AddIntegerParameter("Tags", "T", "Node tags to include.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Node Set", "NS", "Resultant node set.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Node Set";
            List<int> tags = new List<int>();

            DA.GetData("Name", ref name);
            DA.GetDataList("Tags", tags);
   
            DA.SetData("Node Set", new GH_FeSet(new NodeSet(name, tags)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.NodeSetNew;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("ea79d9e7-3f59-486c-b8ba-faa13c76553c"); }
        }
    }
}
