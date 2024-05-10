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
    public class Cmpt_CreateBC : GH_Component
    {
        public Cmpt_CreateBC()
            : base ("Boundary Condition - Create", "BC", 
                  "Create a boundary condition manually by specifying the degrees of freedom and changes in values.", 
                  Api.ComponentCategory, "Loads / BC")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Node Set", "NS", "Name of node set to constrain.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Start", "S", "First degree of freedom constrained.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("End", "E", "Last degree of freedom constrained.", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Magnitude", "M", "Magnitude of the prescribed displacement.", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Boundary Condition", "BC", "The resultant boundary condition object.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string nsetName = "default";
            int startDOF = 1, endDOF = 1;
            double magnitude = 0.0;


            DA.GetData("Node Set", ref nsetName);
            DA.GetData("Start", ref startDOF);
            DA.GetData("End", ref endDOF);
            DA.GetData("Magnitude", ref magnitude);
   
            DA.SetData("Boundary Condition", new GH_FeBoundaryCondition(new BoundaryCondition(nsetName, startDOF, endDOF, magnitude)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.BoundaryConditionNew;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8d8fdde6-3803-49fd-a62b-a887386e469b"); }
        }
    }
}
