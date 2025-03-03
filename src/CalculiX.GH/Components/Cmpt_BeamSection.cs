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

using Rhino.Geometry;

using Grasshopper.Kernel;

using Rhino;

namespace CalculiX.GH.Components
{
    public class Cmpt_BeamSection: GH_Component
    {
        public Cmpt_BeamSection()
            : base ("Beam Section", "BSec", "Create a beam section.", Api.ComponentCategory, "Model")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        string resultsPath = "";
        string[] simulationOutput = null;

        public double scale = 1.0; // assume SI units (meters)

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            pManager.AddTextParameter("Name", "N", "Name of beam section.", GH_ParamAccess.item, "BeamSection");
            pManager.AddTextParameter("Element Set", "ES", "Name of element set to apply this section to.", GH_ParamAccess.item, "all");
            pManager.AddNumberParameter("Width", "W", "Width of beam section.", GH_ParamAccess.item, 0.05 * scale);
            pManager.AddNumberParameter("Height", "H", "Height of beam section.", GH_ParamAccess.item, 0.1 * scale);
            pManager.AddVectorParameter("Direction", "D", "Default direction of beam section.", GH_ParamAccess.item, Vector3d.ZAxis);

            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Section", "S", "The resultant beam section.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            string name = "BeamSection";
            DA.GetData("Name", ref name);

            double width = 0.05, height = 0.1;
            DA.GetData("Width", ref width);
            DA.GetData("Height", ref height);

            Vector3d direction = Vector3d.Unset;
            DA.GetData("Direction", ref direction);

            string eset = "all";
            DA.GetData("Element Set", ref eset);

            var section = new BeamSection(name, width * scale, height * scale, "", eset, "RECT");

            if (direction != Vector3d.Unset)
            {
                direction.Unitize();
                section.Direction = direction;
            }
            DA.SetData("Section", new GH_FeSection(section));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.BeamSection;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("26b49505-d124-457f-9bb3-61438f0c9df9"); }
        }
    }
}
