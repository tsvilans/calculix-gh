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
    public class Cmpt_RunCCX: GH_Component
    {
        public Cmpt_RunCCX()
            : base ("Run CCX", "CCX", "Run CalculiX with specified model.", Api.ComponentCategory, "CalculiX")
        { 
        }

        string resultsPath = "";
        string[] simulationOutput = null;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input path", "P", "Path to .inp simulation file.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "Run simulation.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Threads", "T", "Number of threads to use (0: maximum available).", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Results path", "P", "Path to .frd simulation results file.", GH_ParamAccess.item);
            pManager.AddTextParameter("Output", "O", "Console output from simulation.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var run = false;
            DA.GetData("Run", ref run);
            if (!run)
            {
                if (!string.IsNullOrEmpty(resultsPath))
                    DA.SetData("Results path", resultsPath);
                return;
            }

            var inputPath = "";
            if (!DA.GetData("Input path", ref inputPath) || string.IsNullOrEmpty(inputPath) || !System.IO.File.Exists(inputPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Input file '{inputPath}' does not exist!");
                return;
            }

            var inputName = System.IO.Path.GetFileNameWithoutExtension(inputPath);
            var inputDirectory = System.IO.Path.GetDirectoryName(inputPath);

            var outputName = Api.DefaultOutputName;
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var workingDirectory = Path.Combine(executingDirectory, "Temp");

            workingDirectory = inputDirectory;

            resultsPath = Path.Combine(workingDirectory, inputName + ".frd");

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


            int numThreads = 0;

            DA.GetData("Threads", ref numThreads);

            if (numThreads == 0)
            {
                numThreads = Environment.ProcessorCount;
            }


            using (System.Diagnostics.Process p = new System.Diagnostics.Process())
            {
                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
                info.FileName = ccxPath;
                info.WorkingDirectory = workingDirectory;
                info.Arguments = " -i " + inputName;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.EnvironmentVariables.Add("OMP_NUM_THREADS", numThreads.ToString());
                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.StartInfo = info;
                p.Start();
                simulationOutput = p.StandardOutput.ReadToEnd().Split(new string[] { System.Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            }

            if (!string.IsNullOrEmpty(resultsPath))
                DA.SetData("Results path", resultsPath);

            if (simulationOutput != null)
                DA.SetDataList("Output", simulationOutput);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.RunCCX_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("db0f25b0-c833-48ab-b517-bf995bcd0478"); }
        }
    }
}
