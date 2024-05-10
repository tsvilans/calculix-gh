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
    public class Cmpt_Strain: GH_Component
    {
        public Cmpt_Strain()
            : base ("Strain", "Strain", "Get model strains.", Api.ComponentCategory, "Results")
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
            pManager.AddNumberParameter("Von Mises", "VM", "Von Mises strain.", GH_ParamAccess.list);

            pManager.AddNumberParameter("EXX", "EXX", "Strain in the X-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("EYY", "EYY", "Strain in the Y-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("EZZ", "EZZ", "Strain in the Z-direction.", GH_ParamAccess.list);

            pManager.AddNumberParameter("EXY", "EXY", "Strain in the XY-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("EYZ", "EYZ", "Strain in the YZ-plane.", GH_ParamAccess.list);
            pManager.AddNumberParameter("EZX", "EZX", "Strain in the ZX-plane.", GH_ParamAccess.list);

            pManager.AddNumberParameter("Signed Max", "SMAP", "Signed maximum absolute principal strain.", GH_ParamAccess.list);
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

            //DA.GetDataList("Node IDs", nodeIds);

            if (!results.Fields.ContainsKey("TOSTRAIN"))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No strain values found.");
                return;
            }

            float[] exx, eyy, ezz, exy, eyz, ezx;
            float[] eSigned, eMax, eMid, eMin;

            results.Fields["TOSTRAIN"].TryGetValue("EXX", out exx);
            results.Fields["TOSTRAIN"].TryGetValue("EYY", out eyy);
            results.Fields["TOSTRAIN"].TryGetValue("EZZ", out ezz);
            results.Fields["TOSTRAIN"].TryGetValue("EXY", out exy);
            results.Fields["TOSTRAIN"].TryGetValue("EYZ", out eyz);
            results.Fields["TOSTRAIN"].TryGetValue("EZX", out ezx);

            int N = new int[] { exx.Length, eyy.Length, ezz.Length, exy.Length, eyz.Length, ezx.Length }.Min();

            double[] vm = new double[N];
            for (int i = 0; i < N; i++)
            {
                vm[i] = Utility.CalculateVonMises(exx[i], eyy[i], ezz[i], exy[i], eyz[i], ezx[i]);

            }

            Utility.ComputePrincipalInvariants(exx, eyy, ezz, exy, eyz, ezx, out eSigned, out eMax, out eMid, out eMin);

            DA.SetDataList("Von Mises", vm.Select(x => new GH_Number(x)));
            DA.SetDataList("EXX", exx.Select(x => new GH_Number(x)));
            DA.SetDataList("EYY", eyy.Select(x => new GH_Number(x)));
            DA.SetDataList("EZZ", ezz.Select(x => new GH_Number(x)));
            DA.SetDataList("EXY", exy.Select(x => new GH_Number(x)));
            DA.SetDataList("EYZ", eyz.Select(x => new GH_Number(x)));
            DA.SetDataList("EZX", ezx.Select(x => new GH_Number(x)));
            DA.SetDataList("Signed Max", eSigned.Select(x => new GH_Number(x)));

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Strain_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("1f85f82d-728a-4080-a7c3-428683f08e8e"); }
        }
    }
}
