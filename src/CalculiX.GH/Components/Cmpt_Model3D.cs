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

#if DEPRECATED

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
    public class Cmpt_Model3D: GH_Component
    {
        public Cmpt_Model3D()
            : base ("Model 3D", "M3D", "Create a FE model with 3D elements (volumes).", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        string resultsPath = "";
        string[] simulationOutput = null;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Elements as integer indices.", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Element orientations", "EO", "Element orientations as planes.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Node sets", "NS", "Node sets.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Element sets", "ES", "Element sets.", GH_ParamAccess.list);

            var pathParam = pManager.AddTextParameter("Output path", "P", "Optional output path to export the .inp simulation file to.", GH_ParamAccess.item);
            pManager[pathParam].Optional = true;
            
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Model path", "P", "Path to .inp simulation input file.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputPath = "";
            DA.GetData("Output path", ref inputPath);

            if (string.IsNullOrEmpty(inputPath))
            {
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var workingDirectory = Path.Combine(executingDirectory, "Temp");
                //var defaultResultsPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".frd");
                inputPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".inp");
            }

            // TODO: Build model.



            DA.SetData("Model path", inputPath);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Model3D_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("65b150fa-d9a5-43ec-9ee2-986862224d37"); }
        }
    }
}
#endif