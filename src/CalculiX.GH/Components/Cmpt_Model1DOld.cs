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

#if DEPRECATED

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
    public class Cmpt_Model1Dx: GH_Component
    {
        public Cmpt_Model1Dx()
            : base ("Model 1D (Old)", "M1Dx", "Create a FE model with 1D elements (beams).", Api.ComponentCategory, "Model")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        string resultsPath = "";
        string[] simulationOutput = null;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Beams", "B", "Beam curves that represent each 1D element.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Beam cross-section", "BX", "Width and height of beam cross-section.", GH_ParamAccess.list);

            pManager.AddMeshParameter("Supports", "S", "Meshes that define supports.", GH_ParamAccess.list);
            var stParam = pManager.AddNumberParameter("Support threshold", "ST", "Distance to detect support nodes.", GH_ParamAccess.item, 0.01);
            /*
            pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Elements", "E", "Elements as integer indices.", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Element orientations", "EO", "Element orientations as planes.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Node sets", "NS", "Node sets.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Element sets", "ES", "Element sets.", GH_ParamAccess.list);
            */
            var pathParam = pManager.AddTextParameter("Output path", "P", "Optional output path to export the .inp simulation file to.", GH_ParamAccess.item);
            pManager[pathParam].Optional = true;
            pManager[stParam].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Model path", "P", "Path to .inp simulation input file.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputPath = "";
            DA.GetData("Output path", ref inputPath);

            if (string.IsNullOrEmpty(inputPath))
            {
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var workingDirectory = Path.Combine(executingDirectory, "Temp");
                //var defaultResultsPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".frd");
                inputPath = Path.Combine(workingDirectory, Api.DefaultOutputName + ".inp");
            }

            bool SecondOrder = true;
            double mergeThreshold = 0.001;

            var model = new Model("TestModel");

            List<Curve> curves = new List<Curve>();
            DA.GetDataList("Beams", curves);

            List<Mesh> supportMeshes = new List<Mesh>();
            DA.GetDataList("Supports", supportMeshes);

            double threshold = 0.01;
            DA.GetData("Support threshold", ref threshold);


            List<double> beamCrossSection = new List<double>();
            DA.GetDataList("Beam cross-section", beamCrossSection);
            double width = 0.1, height = 0.2;

            if (beamCrossSection.Count > 1)
            {
                width = beamCrossSection[0];
                height = beamCrossSection[1];
            }



            int nodeIndex = 1, elementIndex = 1;

            var esetColumns = new List<int>();
            var esetBeams = new List<int>();

            foreach (Curve curve in curves)
            {
                int a = -1, b = -1, c = -1;

                foreach (var kvp in model.Nodes)
                {
                    if (kvp.Value.DistanceTo(curve.PointAtStart) < mergeThreshold)
                    {
                        a = kvp.Key;
                        break;
                    }
                }

                if (a == -1)
                {
                    model.Nodes[nodeIndex] = curve.PointAtStart;
                    a = nodeIndex;
                    nodeIndex++;
                }

                if (SecondOrder)
                {
                    // Add middle node
                    model.Nodes[nodeIndex] = curve.PointAt(curve.Domain.Mid);
                    c = nodeIndex;

                    nodeIndex++;
                }

                foreach (var kvp in model.Nodes)
                {
                    if (kvp.Value.DistanceTo(curve.PointAtEnd) < mergeThreshold)
                    {
                        b = kvp.Key;
                        break;
                    }
                }

                if (b == -1)
                {
                    model.Nodes[nodeIndex] = curve.PointAtEnd;
                    b = nodeIndex;
                    nodeIndex++;
                }

                if (SecondOrder)
                    model.Elements[elementIndex] = new ElementB32(elementIndex, a, c, b);
                else
                    model.Elements[elementIndex] = new ElementB31(elementIndex, a, b);

                var dir = curve.TangentAtStart;
                dir.Unitize();

                if (Math.Abs(dir * Vector3d.ZAxis) > 0.9999)
                    esetColumns.Add(elementIndex);
                else
                    esetBeams.Add(elementIndex);

                elementIndex++;
            }

            foreach (var mesh in supportMeshes)
            {
                model.NodeSetFromMeshProximity(mesh, "supports", threshold, true);
            }
            //model.NodeSets["Supports"] = model.Nodes.Where(x => x.Value.Z < 0.3).Select(x => x.Key).ToArray();

            model.ElementSets["beams"] = esetBeams.ToList();
            model.ElementSets["columns"] = esetColumns.ToList();
            model.ElementSets["all"] = model.Elements.Select(x => x.Key).ToList();

            // #####

            /*
            model.Nodes.Add(1, new Point3d(0, 0, 0));
            model.Nodes.Add(2, new Point3d(0, 0.5, 0.5));
            model.Nodes.Add(3, new Point3d(0, 1, 1));
            model.Nodes.Add(4, new Point3d(0, 1.5, 1.5));
            model.Nodes.Add(5, new Point3d(0, 2, 2));

            model.Elements.Add(1, new ElementB32(1, 1, 2, 3));
            model.Elements.Add(2, new ElementB32(2, 3, 4, 5));

            model.NodeSets.Add("all", new int[]{1,2,3,4,5});
            model.NodeSets.Add("supports", new int[]{1});

            model.ElementSets.Add("all", new int[]{1, 2});

            model.Normals.Add(new Tuple<int, int, Vector3d>(1, 1, Vector3d.XAxis));
            model.Normals.Add(new Tuple<int, int, Vector3d>(1, 2, Vector3d.XAxis));
            model.Normals.Add(new Tuple<int, int, Vector3d>(1, 3, Vector3d.XAxis));
            model.Normals.Add(new Tuple<int, int, Vector3d>(2, 3, Vector3d.XAxis));
            model.Normals.Add(new Tuple<int, int, Vector3d>(2, 4, Vector3d.XAxis));
            model.Normals.Add(new Tuple<int, int, Vector3d>(2, 5, Vector3d.XAxis));
            */

            //model.Distributions.Add("dist", new Dictionary<int, Plane>{{1, Plane.WorldXY}});

            //model.Orientations.Add("ori", "dist");

            var bsection = new BeamSection("BeamSection", width, height, "wood", "beams", "RECT");
            bsection.Direction = Vector3d.ZAxis;
            var csection = new BeamSection("ColumnSection", width, height, "wood", "columns", "RECT");
            csection.Direction = Vector3d.XAxis;

            var ssection = new SolidSection("SolidSection", "wood", "all", "ori");

            model.Sections.Add(bsection.Name, bsection);
            model.Sections.Add(csection.Name, csection);

            var material = new Material("wood");
            material.Properties.Add(new EngineeringConstants(new double[]{
              9700e6, 440e6, 220e6,
              0.35, 0.6, 0.55,
              400e6, 250e6, 25e6}));
            material.Properties.Add(new Density(450));
            material.Properties.Add(new Expansion(new double[] { 0, 0.003, 0.006 }));

            model.Materials.Add(material);


            var step = new Step(true);
            step.BoundaryConditions.Add(new BoundaryCondition("supports", 1, 3, 0));

            step.Loads.Add(new GravityLoad("all", new Vector3d(0, 0, -9.8)));

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
            get { return new Guid("1f3ad18c-4117-4862-a63b-7802650ad9c2"); }
        }
    }
}
#endif