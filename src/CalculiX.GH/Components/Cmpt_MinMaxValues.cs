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
    public class Cmpt_MinMaxValues: GH_Component
    {
        public Cmpt_MinMaxValues()
            : base ("MinMax", "MinMax", "Get minimum and maximum values from a list.", Api.ComponentCategory, "Results")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Numbers", "N", "List of numbers.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Minimum", "Min", "Minimum value in list.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum", "Max", "Maximum value in list.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var valueList = new List<double>();
            DA.GetDataList("Numbers", valueList);

            DA.SetData("Minimum", valueList.Min());
            DA.SetData("Maximum", valueList.Max());
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.MinMax;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("44641ab0-dd4a-423a-90a7-90376989af98"); }
        }
    }
}
