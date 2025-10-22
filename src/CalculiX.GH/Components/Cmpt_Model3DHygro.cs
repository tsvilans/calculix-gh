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


using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Types.Transforms;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH.Components
{
    public class Cmpt_Model3DHygro: GH_Component
    {
        public Cmpt_Model3DHygro()
            : base ("Model 3D Hygro", "M3D", "Create a FE model with 3D elements (volumes) for simulating mechanical and hygroscopic behaviour.", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        int nodesParam, elementsParam, orientationsParam, nsetParam, esetParam, loadParam, bcParam, refParam, constraintsParam;

        string resultsPath = "";
        string[] simulationOutput = null;

        double scale = 1.0; // assuming SI units (meters)

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            nodesParam = pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.tree);
            elementsParam = pManager.AddIntegerParameter("Elements", "E", "Elements as integer indices.", GH_ParamAccess.tree);
            orientationsParam = pManager.AddPlaneParameter("Element orientations", "EO", "Element orientations as planes.", GH_ParamAccess.tree);
            nsetParam = pManager.AddGenericParameter("Node sets", "NS", "Node sets.", GH_ParamAccess.list);
            esetParam = pManager.AddGenericParameter("Element sets", "ES", "Element sets.", GH_ParamAccess.list);
            refParam = pManager.AddGenericParameter("Reference points", "RP", "Reference points.", GH_ParamAccess.list);

            loadParam = pManager.AddGenericParameter("Loads", "L", "Loads.", GH_ParamAccess.list);
            bcParam = pManager.AddGenericParameter("Boundary conditions", "BC", "Boundary conditions of model.", GH_ParamAccess.list);
            constraintsParam = pManager.AddGenericParameter("Constraints", "CN", "Constraints.", GH_ParamAccess.list);

            var pathParam = pManager.AddTextParameter("Output path", "P", "Optional output path to export the .inp simulation file to.", GH_ParamAccess.item);

            pManager[orientationsParam].Optional = true;
            pManager[nsetParam].Optional = true;
            pManager[esetParam].Optional = true;
            pManager[loadParam].Optional = true;
            pManager[bcParam].Optional = true;
            pManager[refParam].Optional = true;
            pManager[constraintsParam].Optional = true;
            pManager[pathParam].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Model path", "P", "Path to .inp simulation input file.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var model = new Model("Model3dHygro");
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            var inputPath = "";
            DA.GetData("Output path", ref inputPath);

            if (string.IsNullOrEmpty(inputPath))
            {
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var workingDirectory = Path.Combine(executingDirectory, "Temp");
                //var defaultResultsPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".frd");
                inputPath = Path.Combine(workingDirectory, model.Name + ".inp");
            }

            // Get inputs

            GH_Structure<GH_Point> nodes;
            GH_Structure<GH_Integer> elements;
            GH_Structure<GH_Plane> orientations;
            List<GH_FeSet> nodeSets = new List<GH_FeSet>();
            List<GH_FeSet> elementSets = new List<GH_FeSet> ();
            List<GH_FeLoad> loads = new List<GH_FeLoad>();
            List<GH_FeConstraint> constraints = new List<GH_FeConstraint>();
            List<GH_FeReferencePoint> refPoints = new List<GH_FeReferencePoint>();
            List<GH_FeBoundaryCondition> bconditions = new List<GH_FeBoundaryCondition>();

            DA.GetDataTree(nodesParam, out nodes);
            DA.GetDataTree(elementsParam, out elements);
            DA.GetDataTree(orientationsParam, out orientations);
            DA.GetDataList(nsetParam, nodeSets);
            DA.GetDataList(esetParam, elementSets);
            DA.GetDataList(loadParam, loads);
            DA.GetDataList(bcParam, bconditions);
            DA.GetDataList(refParam, refPoints);
            DA.GetDataList(constraintsParam, constraints);

            // 2. Add nodes

            int id;
            foreach (GH_Path path in nodes.Paths)
            {
                if (nodes[path].Count < 1) continue;
                var pt = nodes[path][0].Value;
                id = path.Indices[0];

                model.Nodes.Add(id, pt * scale);
            }

            foreach (GH_FeSet ghnset in nodeSets)
            {
                var nset = ghnset.Value;
                if (!model.NodeSets.ContainsKey(nset.Name))
                {
                    model.NodeSets.Add(nset.Name, new List<int>());

                }

                model.NodeSets[nset.Name].AddRange(nset.Tags);
            }

            foreach (GH_FeReferencePoint refPoint in refPoints)
            {
                if (refPoint != null)
                    model.ReferencePoints[refPoint.Value.Name] = refPoint.Value;
            }

            // 3. Add elements
            foreach (GH_Path path in elements.Paths)
            {
                if (elements[path].Count < 4) continue;
                id = path.Indices[0];

                var elementIndices = elements[path].Select(x => x.Value).ToArray();
                switch (elementIndices.Length)
                {
                    case (4):
                        model.Elements.Add(id, new ElementC3D4(id, elementIndices));
                        break;
                    case (10):
                        // <eyeroll> Gmsh!!!
                        var temp = elementIndices[9];
                        elementIndices[9] = elementIndices[8];
                        elementIndices[8] = temp;

                        model.Elements.Add(id, new ElementC3D10(id, elementIndices));
                        break;
                    default:
                        break;
                }
            }

            foreach (GH_FeSet gheset in elementSets)
            {
                var eset = gheset.Value;
                if (!model.ElementSets.ContainsKey(eset.Name))
                {
                    model.ElementSets.Add(eset.Name, new List<int>());

                }

                model.ElementSets[eset.Name].AddRange(eset.Tags);
            }

            // 4. Add an element set for all elements
            model.ElementSets.Add("all", model.Elements.Select(x => x.Key).ToList());
            model.NodeSets.Add("all", model.Nodes.Select(x => x.Key).ToList());


            // 6. Add element orientations
            var distro = new Dictionary<int, Plane>();
            foreach (GH_Path path in orientations.Paths)
            {
                if (orientations[path].Count < 1) continue;
                var plane = orientations[path][0].Value;
                id = path.Indices[0];
                distro.Add(id, plane);
            }

            model.Distributions.Add("distro", distro);
            model.Orientations.Add("ori", "distro");

            // 9. Add material
            var material = new Material("spruce");
            material.Properties.Add(new EngineeringConstants(
              new double[] { 9700e6, 400e6, 220e6, 0.35, 0.6, 0.55, 400e6, 250e6, 25e6 }));
            material.Properties.Add(new Density(450.0));
            material.Properties.Add(new Expansion(new double[] { 0, 0.003, 0.007 }));

            model.Materials.Add(material);

            // 10. Add solid section
            model.Sections.Add("section", new SolidSection("section", material.Name, "all", "ori"));

            //model.InitialConditions.Add(new InitialTemperature("all", 0));

            foreach (GH_FeConstraint constraint in constraints)
            {
                if (constraint != null)
                    model.Constraints.Add(constraint.Value);
            }

            // 11. Add simulation step
            var step = new Step(true);

            // 12. Add loads
            foreach (var ghload in loads)
            {
                step.Loads.Add(ghload.Value);
            }

            // 13. Add boundary conditions
            foreach (var bc in bconditions)
                step.BoundaryConditions.Add(bc.Value);

            // 15. Add simulation step to model
            model.Steps.Add(step);

            model.Export(inputPath);


            DA.SetData("Model path", inputPath);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.ModelHygro_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4fe32e04-55d2-43b7-8823-89b3cbd0df82"); }
        }
    }
}
