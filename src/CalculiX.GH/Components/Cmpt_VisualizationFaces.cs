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

namespace CalculiX.GH.Components
{
    public class Cmpt_VisualizationFaces : GH_Component
    {
        public Cmpt_VisualizationFaces()
            : base ("Viz Faces", "VizF", "Get the visible faces of all elements for constructing a visualization mesh.", Api.ComponentCategory, "Results")
        { 
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Nodes", "N", "Indices of active visualization nodes.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Faces", "F", "Visualization faces as a DataTree.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults results = null;
            DataTree<int> outputFaces = new DataTree<int>();



            if (!DA.GetData<FrdResults>("Results", ref results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            var comparer = new CompareIntArraySlow();
            //var cellNeighbours = new Dictionary<int[], int>(nTetra, comparer);

            var cellSet = new HashBucket<int[]>(comparer);

            foreach (var element in results.Elements)
            {
                foreach (var face in Utility.GetElementVisualizationFaces(element))
                {
                    //var cell = new int[face.Length];
                    //Array.Copy(face, cell, face.Length);
                    //Array.Sort(cell);
                    cellSet.Add(face);
                }
            }

            var faces = cellSet.GetUnique();

            // Get visualization nodes
            var nodeSet = new HashSet<int>();
            foreach (var face in faces)
            {
                foreach(var f in face)
                {
                    nodeSet.Add(f);
                }
            }

            // Create visualization node list and remap visualization faces
            var nodeMap = new Dictionary<int, int>();
            var vizNodes = new List<int>();

            int counter = 0;
            int index = 0;

            foreach (var node in results.Nodes)
            {
                if (nodeSet.Contains(node.Id))
                {
                    nodeMap[node.Id] = counter;
                    vizNodes.Add(index);
                    counter++;
                }
                index++;
            }

            for(int i = 0; i < faces.Count; ++i)
            {
                for (int j = 0; j < faces[i].Length; ++j)
                {
                    faces[i][j] = nodeMap[faces[i][j]];
                }
            }

            /*
            faces.Sort((f0, f1) =>
            {
                int xdiff = f0[0].CompareTo(f1[0]);
                if (xdiff != 0) return xdiff;
                else
                {
                    int ydiff = f0[1].CompareTo(f1[1]);
                    if (ydiff != 0) return ydiff;
                    else
                        return f0[2].CompareTo(f1[2]);
                }
            });
            */

            GH_Path path = new GH_Path(0);
            for(int i = 0; i < faces.Count; ++i)
            {
                outputFaces.AddRange(faces[i], path);
                path = path.Increment(0);
            }


            DA.SetDataList("Nodes", vizNodes.Select(x => new GH_Integer(x)));
            DA.SetDataTree(1, outputFaces);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Visualization_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("ab7562ee-2e71-48aa-94ef-44005fb61dbd"); }
        }
    }
}
