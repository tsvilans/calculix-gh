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
    public class Cmpt_MeshCreases: GH_Component
    {
        public Cmpt_MeshCreases()
            : base ("Mesh Creases", "MCr", "Get creases (sharp edges) of a mesh.", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        Line[] creases = null;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Threshold", "T", "Threshold dihedral angle between adjacent faces to qualify as crease.", GH_ParamAccess.item, 0.7857);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Creases", "C", "Creased edges as lines.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            double threshold = 0.7857;

            if (!DA.GetData("Mesh", ref mesh) || mesh == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to get mesh.");
                return;
            }

            DA.GetData("Threshold", ref threshold);

            creases = mesh.ExtractCreases(threshold).ToArray();

            DA.SetDataList("Creases", creases.Select(x => new GH_Line(x)));
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (creases != null)
                args.Display.DrawLines(creases, System.Drawing.Color.White, 1);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.MeshCrease;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("DDB1FB67-9B90-4D33-8094-D9C744E75031"); }
        }
    }
}
