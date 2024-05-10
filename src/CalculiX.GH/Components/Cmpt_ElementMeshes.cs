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
using System.Diagnostics;

namespace CalculiX.GH.Components
{
    public class Cmpt_ElementMeshes : GH_Component
    {
        public Cmpt_ElementMeshes()
            : base ("Element Mesh", "EleM", "Get the mesh of each element.", Api.ComponentCategory, "Results")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "Mesh for each solid element in model.", GH_ParamAccess.list);
            pManager.AddMeshFaceParameter("Faces", "F", "Faces of all elements as a DataTree.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Indices", "I", "Indices of all elements as a DataTree.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;
            DataTree<MeshFace> outputFaces = new DataTree<MeshFace>();
            DataTree<int> outputIndices = new DataTree<int>();



            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            Dictionary<int, Point3d> outputNodes = new Dictionary<int, Point3d>();

            foreach (var node in results.Nodes)
            {
                outputNodes.Add(node.Id, new Point3d(node.X, node.Y, node.Z));
            }

            var meshes = new List<Mesh>();


            foreach (var element in results.Elements)
            {
                outputIndices.AddRange(element.Indices, new GH_Path(element.Id));
                var mesh = new Mesh();
                Dictionary<int, int> nodeMap = new Dictionary<int, int>();

                for(int i = 0; i < element.Indices.Length; ++i)
                {
                    mesh.Vertices.Add(outputNodes[element.Indices[i]]);
                    nodeMap[element.Indices[i]] = i;
                }

                var faces = Utility.GetElementVisualizationFaces(element);
                foreach (var face in faces)
                {
                    if (face.Length == 3)
                    {
                        mesh.Faces.AddFace(nodeMap[face[0]], nodeMap[face[1]], nodeMap[face[2]]);
                        //outputFaces.Add(new MeshFace(face[0], face[1], face[2]), new GH_Path(element.Id));
                        outputFaces.Add(new MeshFace(nodeMap[face[0]], nodeMap[face[1]], nodeMap[face[2]]), new GH_Path(element.Id));

                    }
                    else if (face.Length == 4)
                    {
                        mesh.Faces.AddFace(nodeMap[face[0]], nodeMap[face[1]], nodeMap[face[2]], nodeMap[face[3]]);
                        //outputFaces.Add(new MeshFace(face[0], face[1], face[2], face[3]), new GH_Path(element.Id));
                        outputFaces.Add(new MeshFace(nodeMap[face[0]], nodeMap[face[1]], nodeMap[face[2]], nodeMap[face[3]]), new GH_Path(element.Id));
                    }

                }

                meshes.Add(mesh);
            }

            DA.SetDataList("Meshes", meshes);
            DA.SetDataTree(1, outputFaces);
            DA.SetDataTree(2, outputIndices);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.ElementMesh_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("ba67d159-1e45-46dd-bb9e-9ec16fb8a31b"); }
        }
    }
}
