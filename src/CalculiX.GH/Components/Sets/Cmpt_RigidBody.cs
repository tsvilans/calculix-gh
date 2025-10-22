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
    public class Cmpt_RigidBody : GH_Component
    {
        public Cmpt_RigidBody()
            : base ("Rigid Body", "RigB", 
                  "Make a rigid body constraint for a node set.", Api.ComponentCategory, "Sets")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("NodeSet", "Ns", "Node set name.", GH_ParamAccess.item);
            pManager.AddPointParameter("Ref point", "RP", "Reference point for the constraint.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Rigid Body", "RB", "Resultant rigid body constraint.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Ref Point", "RP", "Resultant reference point.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string nodeSetName = "";
            Point3d point = Point3d.Unset;

            if (!DA.GetData("NodeSet", ref nodeSetName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node set name is necessary.");
                return;
            }

            if (!DA.GetData("Ref point", ref point))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Reference point is necessary.");
                return;
            }

            var referencePoint = new ReferencePoint(System.Guid.NewGuid().ToString(), point, nodeSetName);
            var rigidBody = new RigidBody("RigidBody", referencePoint);

            DA.SetData("Rigid body", new GH_FeConstraint(rigidBody));
            DA.SetData("Ref Point", new GH_FeReferencePoint(referencePoint));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.NodeSetPointProximity;

        public override Guid ComponentGuid => new Guid("963E9DDB-64C5-4F7E-9D6A-DFB2AFAC387D");
    }
}
