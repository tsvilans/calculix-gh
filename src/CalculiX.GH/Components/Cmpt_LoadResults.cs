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

namespace CalculiX.GH.Components
{
    public class Cmpt_LoadResults: GH_Component
    {
        public Cmpt_LoadResults()
            : base ("Load Results", "Res", "Load results from a .frd file.", Api.ComponentCategory, "Results")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Results path", "P", "Filepath of .frd results file.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "The simulation results.", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string frdPath = "";
            DA.GetData("Results path", ref frdPath);

            if (string.IsNullOrEmpty(frdPath) || !System.IO.File.Exists(frdPath)) 
            {
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var workingDirectory = Path.Combine(executingDirectory, "Temp");

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Results file cannot be found. Check if it exists. Could be in '{workingDirectory}'...");
                return; 
            }

            // Do something with the results
            FrdResults results = new FrdResults();
            results.Read(frdPath);

            DA.SetData("Results", new GH_FrdResults(results));
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
            get { return new Guid("9f42fa9f-6b89-4281-b087-0073991b54aa"); }
        }
    }
}
