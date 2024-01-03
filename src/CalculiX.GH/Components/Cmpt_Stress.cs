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
    public class Cmpt_Stresses: GH_Component
    {
        public Cmpt_Stresses()
            : base ("Stress", "Stress", "Get model stresses.", Api.ComponentCategory, "Results")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("SXX", "SXX", "Stress in the X-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SYY", "SYY", "Stress in the Y-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SZZ", "SZZ", "Stress in the Z-direction.", GH_ParamAccess.list);

            pManager.AddNumberParameter("SXY", "SXY", "Stress in the XY-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SYZ", "SYZ", "Stress in the YZ-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SZX", "SZX", "Stress in the ZX-plane.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;

            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            if (!results.Fields.ContainsKey("STRESS"))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No stress values found.");
                return;
            }

            float[] sxx, syy, szz, sxy, syz, szx;

            results.Fields["STRESS"].TryGetValue("SXX", out sxx);
            results.Fields["STRESS"].TryGetValue("SYY", out syy);
            results.Fields["STRESS"].TryGetValue("SZZ", out szz);
            results.Fields["STRESS"].TryGetValue("SXY", out sxy);
            results.Fields["STRESS"].TryGetValue("SYZ", out syz);
            results.Fields["STRESS"].TryGetValue("SZX", out szx);

            DA.SetDataList("SXX", sxx.Select(x => new GH_Number(x)));
            DA.SetDataList("SYY", syy.Select(x => new GH_Number(x)));
            DA.SetDataList("SZZ", szz.Select(x => new GH_Number(x)));
            DA.SetDataList("SXY", sxy.Select(x => new GH_Number(x)));
            DA.SetDataList("SYZ", syz.Select(x => new GH_Number(x)));
            DA.SetDataList("SZX", szx.Select(x => new GH_Number(x))); 

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
            get { return new Guid("4825b9a4-be14-43a5-8e03-5a35d1541948"); }
        }
    }
}
