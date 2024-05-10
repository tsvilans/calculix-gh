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
    public class Cmpt_GLoad : GH_Component
    {
        public Cmpt_GLoad()
            : base ("Load - Gravity", "LGrav", "Create a gravity load spread out over a set of elements.", Api.ComponentCategory, "Loads / BC")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Element Set", "NS", "Name of the element set to apply load to.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector", "V", "Direction of load. Its magnitude is the magnitude of the load.", GH_ParamAccess.item, new Vector3d(0,0,-9.8));
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Load", "L", "The resultant gravity load object.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string esetName = "default";
            Vector3d force = new Vector3d();

            DA.GetData("Element Set", ref esetName);
            DA.GetData("Vector", ref force);

            var load = new GravityLoad(esetName, force);
   
            DA.SetData("Load", new GH_FeLoad(load));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.LoadGravity;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("40ea44e2-2dd4-4e96-a99c-fa978da8a0d3"); }
        }
    }
}
