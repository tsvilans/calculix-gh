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
    public class Cmpt_Stress: GH_Component
    {
        public Cmpt_Stress()
            : base ("Stress", "Stress", "Get model stresses.", Api.ComponentCategory, "Results")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Node IDs", "N", "Ids of nodes to query.", GH_ParamAccess.list);
            //pManager[1].Optional = true;
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Von Mises", "VM", "Von Mises stress.", GH_ParamAccess.list);

            pManager.AddNumberParameter("SXX", "SXX", "Stress in the X-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SYY", "SYY", "Stress in the Y-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SZZ", "SZZ", "Stress in the Z-direction.", GH_ParamAccess.list);

            pManager.AddNumberParameter("SXY", "SXY", "Stress in the XY-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SYZ", "SYZ", "Stress in the YZ-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SZX", "SZX", "Stress in the ZX-plane.", GH_ParamAccess.list);

            pManager.AddNumberParameter("Signed Max", "SMAP", "Signed maximum absolute principal stress.", GH_ParamAccess.list);
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
            float[] sSigned, sMax, sMid, sMin;

            results.Fields["STRESS"].TryGetValue("SXX", out sxx);
            results.Fields["STRESS"].TryGetValue("SYY", out syy);
            results.Fields["STRESS"].TryGetValue("SZZ", out szz);
            results.Fields["STRESS"].TryGetValue("SXY", out sxy);
            results.Fields["STRESS"].TryGetValue("SYZ", out syz);
            results.Fields["STRESS"].TryGetValue("SZX", out szx);

            int N = new int[] { sxx.Length, syy.Length, szz.Length, sxy.Length, syz.Length, szx.Length }.Min();

            double[] vm = new double[N];
            for (int i = 0; i < N; i++)
            {
                vm[i] = Utility.CalculateVonMises(sxx[i], syy[i], szz[i], sxy[i], syz[i], szx[i]);

            }

            Utility.ComputePrincipalInvariants(sxx, syy, szz, sxy, syz, szx, out sSigned, out sMax, out sMid, out sMin);

            DA.SetDataList("Von Mises", vm.Select(x => new GH_Number(x)));
            DA.SetDataList("SXX", sxx.Select(x => new GH_Number(x)));
            DA.SetDataList("SYY", syy.Select(x => new GH_Number(x)));
            DA.SetDataList("SZZ", szz.Select(x => new GH_Number(x)));
            DA.SetDataList("SXY", sxy.Select(x => new GH_Number(x)));
            DA.SetDataList("SYZ", syz.Select(x => new GH_Number(x)));
            DA.SetDataList("SZX", szx.Select(x => new GH_Number(x)));
            DA.SetDataList("Signed Max", sSigned.Select(x => new GH_Number(x)));

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Stress_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4825b9a4-be14-43a5-8e03-5a35d1541948"); }
        }
    }
}
