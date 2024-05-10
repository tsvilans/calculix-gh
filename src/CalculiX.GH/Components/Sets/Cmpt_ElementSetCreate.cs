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
    public class Cmpt_ElementSetCreate : GH_Component
    {
        public Cmpt_ElementSetCreate()
            : base ("Element Set - Create", "ESet", 
                  "Create an element set from a list of element IDs (tags).", Api.ComponentCategory, "Sets")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of element set.", GH_ParamAccess.item, "Element Set");
            pManager.AddIntegerParameter("Tags", "T", "Element tags to include.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element Set", "ES", "Resultant element set.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Element Set";
            List<int> tags = new List<int>();

            DA.GetData("Name", ref name);
            DA.GetDataList("Tags", tags);
   
            DA.SetData("Element Set", new GH_FeSet(new ElementSet(name, tags)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.ElementSetNew;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("78b5380f-13dd-411a-a026-88cfddd91bec"); }
        }
    }
}
