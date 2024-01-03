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

namespace CalculiX.GH.Components
{
    public class Cmpt_GetNodesAndElements : GH_Component
    {
        public Cmpt_GetNodesAndElements()
            : base ("Nodes Elements", "NE", "Get all nodes and elements in model.", Api.ComponentCategory, "Results")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes as tree. Tree path corresponds to node ID.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Elements as tree. Tree path corresponds to element ID.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;
            DataTree<GH_Point> outputNodes = new DataTree<GH_Point>();
            DataTree<int> outputElements = new DataTree<int>();

            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            foreach (var node in  results.Nodes) 
            {
                outputNodes.Add(new GH_Point(new Point3d(node.X, node.Y, node.Z)), new GH_Path(node.Id));
            }

            foreach (var element in results.Elements)
            {
                outputElements.AddRange(element.Indices, new GH_Path(element.Id));
            }

            DA.SetDataTree(0, outputNodes);
            DA.SetDataTree(1, outputElements);
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
            get { return new Guid("ad1f078f-6f09-4129-9092-6893e4f6c1af"); }
        }
    }
}
