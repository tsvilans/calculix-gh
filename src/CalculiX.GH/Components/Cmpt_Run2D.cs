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

namespace CalculiX.GH.Components
{
    public class Cmpt_Run2D: GH_Component
    {
        public Cmpt_Run2D()
            : base ("Run 2D", "R2D", "Run a simulation with 2D elements (beams).", Api.ComponentCategory, "Simulate")
        { 
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Results path", "P", "Filepath of .frd results file.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Results path", "P", "Filepath of .frd results file.", GH_ParamAccess.item);
            pManager.AddTextParameter("Output", "O", "Console output from simulation.", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Do something

            var outputName = "output";
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var workingDirectory = Path.Combine(executingDirectory, "Temp");
            var outputPath = Path.Combine(workingDirectory, outputName + ".inp");
            var resultsPath = Path.Combine(workingDirectory, outputName + ".frd");

            var ccxPath = Path.Combine(executingDirectory, "ccx_static.exe");

            if (!File.Exists(ccxPath)) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Couldn't find CalculiX. Should be at '{ccxPath}'...");
                return;
            }

            if (!Directory.Exists(workingDirectory)) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Creating working directory at '{workingDirectory}'...");
                Directory.CreateDirectory(workingDirectory);
            }

            string consoleOutput = "";

            using (System.Diagnostics.Process p = new System.Diagnostics.Process())
            {
                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
                info.FileName = ccxPath;
                info.WorkingDirectory = workingDirectory;
                info.Arguments = " -i " + outputName;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.StartInfo = info;
                p.Start();
                consoleOutput = p.StandardOutput.ReadToEnd();

            }

            DA.SetData("Results path", resultsPath);
            DA.SetData("Output", consoleOutput);
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
            get { return new Guid("db0f25b0-c833-48ab-b517-bf995bcd0478"); }
        }
    }
}
