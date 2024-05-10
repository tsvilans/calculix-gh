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
    public class Cmpt_TemperatureBC : GH_Component
    {
        public Cmpt_TemperatureBC()
            : base ("Boundary Condition - Temperature", "TempBC", 
                  "Create a prescribed temperature boundary condition.", 
                  Api.ComponentCategory, "Loads / BC")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Node Set", "NS", "Name of node set to constrain.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Temperature", "T", "Prescribed change in temperature.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Boundary Condition", "BC", "The resultant boundary condition object.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string nsetName = "default";
            double temp = 0.0;

            DA.GetData("Node Set", ref nsetName);
            DA.GetData("Temperature", ref temp);
   
            DA.SetData("Boundary Condition", new GH_FeBoundaryCondition(new BoundaryCondition(nsetName, 11, 11, temp)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.BoundaryConditionTemperature;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("686bda05-f2d9-44cb-a767-631e3e39cae3"); }
        }
    }
}
