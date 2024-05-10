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
    public class Cmpt_DisplacementBC : GH_Component
    {
        public Cmpt_DisplacementBC()
            : base ("Boundary Condition - Displacement", "DispBC", 
                  "Create a prescribed displacement boundary condition.", 
                  Api.ComponentCategory, "Loads / BC")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Node Set", "NS", "Name of node set to constrain.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector", "V", "Vector of the prescribed displacement.", GH_ParamAccess.item, Vector3d.Zero);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Boundary Condition", "BC", "The resultant boundary condition object.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string nsetName = "default";
            Vector3d vec = Vector3d.Zero;

            DA.GetData("Node Set", ref nsetName);
            DA.GetData("Vector", ref vec);
   
            DA.SetDataList("Boundary Condition", new GH_FeBoundaryCondition[]{
                new GH_FeBoundaryCondition(new BoundaryCondition(nsetName, 1, 1, vec.X)),
                new GH_FeBoundaryCondition(new BoundaryCondition(nsetName, 2, 2, vec.Y)),
                new GH_FeBoundaryCondition(new BoundaryCondition(nsetName, 3, 3, vec.Z)),
            });
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.BoundaryConditionDisplacement;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("261c2ebd-a9a3-4b81-ba94-ffa654e2afeb"); }
        }
    }
}
