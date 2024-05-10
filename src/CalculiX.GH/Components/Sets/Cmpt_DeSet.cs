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
    public class Cmpt_DeSet : GH_Component
    {
        public Cmpt_DeSet()
            : base ("Deconstruct Set", "DeSet", "Deconstruct a element or node set into its tags.", Api.ComponentCategory, "Sets")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Set", "S", "Node or element set.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of set.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tags", "T", "Node or element tags.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_FeSet ghSet = null;
            GenericSet feSet = null;

            DA.GetData("Set", ref ghSet);

            if (ghSet == null) return;
            feSet = ghSet.Value;

            DA.SetData("Name", feSet.Name);
            DA.SetDataList("Tags", feSet.Tags);
           }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.DeconstructSet;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("c27991bf-7f1b-46c4-826e-373a108f6f97"); }
        }
    }
}
