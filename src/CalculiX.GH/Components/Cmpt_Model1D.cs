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
using System.Windows.Forms;

using Rhino.Geometry;

using Grasshopper.Kernel;
using System.IO;
using System.Reflection;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Render.ChangeQueue;
using System.Security.Cryptography;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types.Transforms;
using Rhino;

namespace CalculiX.GH.Components
{
    public class Cmpt_Model1D: GH_Component
    {
        public Cmpt_Model1D()
            : base ("Model 1D", "M1D", "Create a FE model with 1D elements (beams).", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        int nodesParam, elementsParam, normalsParam, sectionParam, nsetParam, esetParam, loadParam, bcParam;

        string resultsPath = "";
        string[] simulationOutput = null;
        public bool doOrtho = false;
        double scale = 1.0; // assuming SI units (meters)

        private void SetOrtho(object sender, EventArgs e)
        {
            doOrtho = !doOrtho;
            ExpireSolution(true);

        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Ortho", SetOrtho, true, doOrtho);

            base.AppendAdditionalComponentMenuItems(menu);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("doOrtho", doOrtho);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("doOrtho", ref doOrtho);
            return base.Read(reader);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            nodesParam = pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.tree);
            elementsParam = pManager.AddIntegerParameter("Elements", "E", "Elements as integer indices.", GH_ParamAccess.tree);
            normalsParam = pManager.AddVectorParameter("Element normals", "EN", "Element normals as vectors.", GH_ParamAccess.tree);
            sectionParam = pManager.AddGenericParameter("Section", "S", "Beam section.", GH_ParamAccess.list);


            nsetParam = pManager.AddGenericParameter("Node sets", "NS", "Node sets.", GH_ParamAccess.list);
            esetParam = pManager.AddGenericParameter("Element sets", "ES", "Element sets.", GH_ParamAccess.list);

            loadParam = pManager.AddGenericParameter("Loads", "L", "Loads.", GH_ParamAccess.list);
            bcParam = pManager.AddGenericParameter("Boundary conditions", "BC", "Boundary conditions of model.", GH_ParamAccess.list);

            var pathParam = pManager.AddTextParameter("Output path", "P", "Optional output path to export the .inp simulation file to.", GH_ParamAccess.item);

            pManager[normalsParam].Optional = true;
            pManager[sectionParam].Optional = true;
            pManager[nsetParam].Optional = true;
            pManager[esetParam].Optional = true;
            pManager[loadParam].Optional = true;
            pManager[bcParam].Optional = true;
            pManager[pathParam].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Model path", "P", "Path to .inp simulation input file.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);

            var model = new Model("Model1D");
            double width = 0.05, height = 0.15;

            var sectionSizes = new List<double>();

            List<GH_FeSection> ghSections = new List<GH_FeSection>();
            List<BeamSection> sections = new List<BeamSection>();

            if (DA.GetDataList("Section", ghSections))
            {
                foreach (var ghSection in ghSections)
                {

                    if (ghSection != null)
                    {
                        if (ghSection.Value is BeamSection)
                            sections.Add(ghSection.Value as BeamSection);
                    }
                }
            }

            //if (DA.GetDataList("Section", sectionSizes) && sectionSizes.Count > 1)
            //{
            //    width = sectionSizes[0] * scale;
            //    height = sectionSizes[1] * scale;
            //}

            var inputPath = "";
            DA.GetData("Output path", ref inputPath);

            string workingDirectory, executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(inputPath))
            {
                workingDirectory = Path.Combine(executingDirectory, "Temp");
                //var defaultResultsPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".frd");
                inputPath = Path.Combine(workingDirectory, model.Name + ".inp");
            }
            else
            {
                workingDirectory = Path.GetDirectoryName(inputPath);
            }

            // Get inputs
            GH_Structure<GH_Point> nodes;
            GH_Structure<GH_Integer> elements;
            GH_Structure<GH_Vector> normals;
            List<GH_FeSet> nodeSets = new List<GH_FeSet>();
            List<GH_FeSet> elementSets = new List<GH_FeSet>();
            List<GH_FeLoad> loads = new List<GH_FeLoad>();
            List<GH_FeBoundaryCondition> bconditions = new List<GH_FeBoundaryCondition>();

            DA.GetDataTree(nodesParam, out nodes);
            DA.GetDataTree(elementsParam, out elements);
            DA.GetDataTree(normalsParam, out normals);
            DA.GetDataList(nsetParam, nodeSets);
            DA.GetDataList(esetParam, elementSets);
            DA.GetDataList(loadParam, loads);
            DA.GetDataList(bcParam, bconditions);

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

            // 3. Add elements
            foreach (GH_Path path in elements.Paths)
            {
                if (elements[path].Count < 2) continue;
                id = path.Indices[0];

                var elementIndices = elements[path].Select(x => x.Value).ToArray();
                switch (elementIndices.Length)
                {
                    case (2):
                        model.Elements.Add(id, new ElementB31(id, elementIndices[0], elementIndices[1]));
                        break;
                    case (3):
                        model.Elements.Add(id, new ElementB32(id, elementIndices[0], elementIndices[1], elementIndices[2]));
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

            // Sort elements into beams or columns
            var beamSet = new ElementSet("beamElements");
            var columnSet = new ElementSet("columnElements");

            foreach (var kvp in model.Elements)
            {
                var ele = kvp.Value;
                var axis = model.Nodes[ele.Indices[0]] - model.Nodes[ele.Indices[ele.Indices.Length - 1]];
                axis.Unitize();

                if (Math.Abs(axis * Vector3d.ZAxis) > 0.9999)
                    columnSet.Tags.Add(ele.Id);
                else
                    beamSet.Tags.Add(ele.Id);
            }

            model.ElementSets.Add(beamSet.Name, beamSet.Tags);
            model.ElementSets.Add(columnSet.Name, columnSet.Tags);

            // 4. Add an element set for all elements
            model.ElementSets.Add("all", model.Elements.Select(x => x.Key).ToList());
            model.NodeSets.Add("all", model.Nodes.Select(x => x.Key).ToList());

            
            // 6. Add element orientations
            var distro = new Dictionary<int, Plane>();
            foreach (var kvp in model.Elements)
            {
                if (kvp.Value is ElementB31 || kvp.Value is ElementB32)
                {
                    distro.Add(kvp.Key, model.GetOrientationFor1dElement(kvp.Value, Vector3d.ZAxis));
                }
            }

            model.Distributions.Add("distro", distro);
            model.Orientations.Add("ori", "distro");

            // 9. Add material
            Material material;
            if (doOrtho)
            {
                material = new Material("@TOMMY_WOODORTHO");
                material.Properties.Add(new UserMaterial(
                  new double[] { 9700e6, 400e6, 220e6, 0.35, 0.6, 0.55, 400e6, 250e6, 25e6 }));
                material.Properties.Add(new Density(480.0));
            }
            //else if (do_ortho)
            //{
            //    //material = new Material("@TOMMY_WOODISO");
            //    material = new Material("WOODISO");
            //    material.Properties.Add(new UserMaterial(
            //      new double[] { 9700e6, 0.35 }));
            //    material.Properties.Add(new Density(480.0));
            //}
            else
            {
                
                material = new Material("WOODISO");
                material.Properties.Add(new Elastic(new double[] { 9700e6, 0.4 }));
                //material.Properties.Add(new EngineeringConstants(
                //  new double[] { 9700e6, 400e6, 220e6, 0.35, 0.6, 0.55, 400e6, 250e6, 25e6 }));
                material.Properties.Add(new Density(480.0));
                //material.Properties.Add(new Expansion(new double[] { 0, 0.003, 0.007 }));
                
            }

            model.Materials.Add(material);

            // 10. Add beam section
            if (sections.Count > 0)
            {
                var beamHashSet = new HashSet<int>(beamSet.Tags);
                var columnHashSet = new HashSet<int>(columnSet.Tags);

                foreach (var section in sections)
                {
                    if (string.IsNullOrEmpty(section.ElementSet)) continue;
                    section.Material = material.Name;
                    section.Orientation = "ori";

                    if (model.ElementSets.ContainsKey(section.ElementSet))
                    {
                        var sectionSet = new HashSet<int>(model.ElementSets[section.ElementSet]);

                        beamHashSet = beamHashSet.Except(sectionSet).ToHashSet();
                        columnHashSet = columnHashSet.Except(sectionSet).ToHashSet();

                        model.Sections.Add(section.Name, section);
                    }
                }
                model.ElementSets[beamSet.Name] = beamHashSet.ToList();
                model.ElementSets[columnSet.Name] = columnHashSet.ToList();
            }

            // Add default sections for elements without a beam section
            model.Sections.Add("beamSection", new BeamSection("beamSection", width, height, material.Name, beamSet.Name, "RECT") 
            { Direction = Vector3d.ZAxis, Orientation= "ori" });
            model.Sections.Add("columnSection", new BeamSection("columnSection", width, height, material.Name, columnSet.Name, "RECT") 
            { Direction = Vector3d.XAxis, Orientation = "ori" });

            // Export properties for custom material
            var propModel = new PropertyMap();
            foreach(var kvp in model.Distributions["distro"])
            {
                propModel.Properties.Add(new ElementOrientationProperty() 
                { 
                    ElementId = kvp.Key, 
                    XAxis = kvp.Value.XAxis, 
                    YAxis = kvp.Value.YAxis });
            }

            propModel.Write(System.IO.Path.Combine(workingDirectory, "orientations.prop"));

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
                return Properties.Resources.Model1D_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("a2492a36-9736-42a9-aeb1-7921e597449c "); }
        }
    }
}
