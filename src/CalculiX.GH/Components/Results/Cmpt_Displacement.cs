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
    public class Cmpt_Displacement: GH_Component
    {
        public Cmpt_Displacement()
            : base ("Displacement", "Disp", "Get model displacements.", Api.ComponentCategory, "Results")
        { 
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Step", "S", "StepID.", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("D1", "D1", "Displacement in the X-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("D2", "D2", "Displacement in the Y-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("D3", "D3", "Displacement in the Z-direction.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;
            //var nodeIds = new List<int>();

            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            int stepId = 1;
            DA.GetData("Step", ref stepId);
            if (!results.Fields.ContainsKey(stepId))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Step {stepId} not found...");
                stepId = results.Fields.Keys.Last();
            }

            var step = results.Fields[stepId];
            Message = $"Step {stepId}";

            if (!step.ContainsKey("DISP"))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No displacement values found.");
                return;
            }


            float[] d1, d2, d3, all;


            step["DISP"].TryGetValue("D1", out d1);
            step["DISP"].TryGetValue("D2", out d2);
            step["DISP"].TryGetValue("D3", out d3);

            DA.SetDataList("D1", d1.Select(x => new GH_Number(x)));
            DA.SetDataList("D2", d2.Select(x => new GH_Number(x)));
            DA.SetDataList("D3", d3.Select(x => new GH_Number(x)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Displacement;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("d99bb7b7-424d-490a-8597-a104dfc15ad5"); }
        }
    }
}
