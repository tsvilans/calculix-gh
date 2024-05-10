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
using Rhino.Display;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using System.Drawing;
using Rhino;
using System.Data.SqlTypes;
using System.Windows.Forms;

namespace CalculiX.GH.Components
{
    public class Cmpt_VisualizeResults : GH_Component
    {
        public Cmpt_VisualizeResults()
            : base ("Viz Results", "VizR", "Display the results as a mesh with specific field as mesh colors.", Api.ComponentCategory, "Results")
        {
            activeComponents = new Dictionary<string, string>();
            activeField = string.Empty;

            //Params.ParameterChanged += Params_ParameterChanged;
            Params.ParameterSourcesChanged += Params_ParameterSourcesChanged;

            //Params.OnParametersChanged();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override BoundingBox ClippingBox
        {
            get {
                var bb = BoundingBox.Empty;
                if (originalMesh != null)
                {
                    bb.Union(originalMesh.GetBoundingBox(true));
                }
                if (deformedMesh != null)
                {
                    bb.Union(deformedMesh.GetBoundingBox(true));
                }

                return bb;
            }
        }

        int resultsParam = 0, gammaParam = 0, deformationParam = 0;
        IGH_Param fieldParam = null, componentParam = null;
        double scale = 1.0; // assuming SI units (meters)

        // Display mesh
        Mesh originalMesh = null, deformedMesh = null;
        Vector3f[] displacements = null;
        Line[] creasesOriginal, creasesDeformed;

        // Results and fields
        bool reload = false;
        FrdResults results = null;
        Dictionary<string, Dictionary<string, float[]>> fields = null;

        // Active field and components
        Dictionary<string, string> activeComponents = null;
        string activeField = ""; 

        // Connected field and component sources
        bool updateValues = false;
        GH_ValueList fieldValueList = null, componentValueList = null;

        // Colors
        Gradient visualizationGradient = null;
        System.Drawing.Color[] visualizationColors = null;

        private void UpdateComponentSources()
        {
            foreach (IGH_Param source in componentParam.Sources)
            {
                if (source is GH_ValueList)
                {
                    GH_ValueList vl = source as GH_ValueList;

                    List<string> requiredKeys = fields[activeField].Keys.ToList();
                    List<string> existingKeys = new List<string>();

                    foreach (var key in vl.ListItems)
                    {
                        existingKeys.Add(key.Name);
                    }
                    if (!existingKeys.SequenceEqual(requiredKeys))
                    {
                        vl.ListItems.Clear();

                        for (int i = 0; i < requiredKeys.Count; i++)
                        {
                            vl.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(requiredKeys[i], $"\"{requiredKeys[i]}\""));
                        }
                        vl.ListMode = GH_ValueListMode.DropDown;
                    }
                    // Reset Selection
                    var fieldIndex = requiredKeys.IndexOf(activeComponents[activeField]);
                    vl.SelectItem(fieldIndex);

                    //vl.CollectData();
                }
            }
        }

        private void Params_ParameterSourcesChanged(object sender, GH_ParamServerEventArgs e)
        {
            // Handle field parameter
            if ((e.ParameterSide == GH_ParameterSide.Input) && (e.ParameterIndex == Params.Input.IndexOf(fieldParam)))
            {
                foreach (IGH_Param source in e.Parameter.Sources)
                {
                    if (source is GH_ValueList)
                    {
                        GH_ValueList vl = source as GH_ValueList;

                        List<string> requiredKeys = fields.Keys.ToList();
                        List<string> existingKeys = new List<string>();

                        foreach (var key in vl.ListItems)
                        {
                            existingKeys.Add(key.Name);
                        }
                        if (!existingKeys.SequenceEqual(requiredKeys))
                        {
                            vl.ListItems.Clear();

                            for (int i = 0; i < requiredKeys.Count; i++)
                            {
                                vl.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(requiredKeys[i], $"\"{requiredKeys[i]}\""));
                            }
                            vl.ListMode = GH_ValueListMode.DropDown;
                        }
                        // Reset Selection
                        var fieldIndex = requiredKeys.IndexOf(activeField);
                        vl.SelectItem(fieldIndex);

                        //vl.CollectData();
                    }
                }
            }
            // Handle component parameter
            else if ((e.ParameterSide == GH_ParameterSide.Input) && (e.ParameterIndex == Params.Input.IndexOf(componentParam)))
            {
                foreach (IGH_Param source in e.Parameter.Sources)
                {
                    if (source is GH_ValueList)
                    {
                        GH_ValueList vl = source as GH_ValueList;

                        List<string> requiredKeys = fields[activeField].Keys.ToList();
                        List<string> existingKeys = new List<string>();

                        foreach (var key in vl.ListItems)
                        {
                            existingKeys.Add(key.Name);
                        }
                        if (!existingKeys.SequenceEqual(requiredKeys))
                        {
                            vl.ListItems.Clear();

                            for (int i = 0; i < requiredKeys.Count; i++)
                            {
                                vl.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(requiredKeys[i], $"\"{requiredKeys[i]}\""));
                            }
                            vl.ListMode = GH_ValueListMode.DropDown;
                        }
                        // Reset Selection
                        var fieldIndex = requiredKeys.IndexOf(activeComponents[activeField]);
                        vl.SelectItem(fieldIndex);

                        //vl.CollectData();
                    }
                }
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            resultsParam = pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);

            int fieldParamIndex = pManager.AddTextParameter("Field", "F", "Field to display on mesh.", GH_ParamAccess.item, "STRESS");
            int componentParamIndex = pManager.AddTextParameter("Component", "C", "Component of field to display on mesh.", GH_ParamAccess.item, "SXX");
            gammaParam = pManager.AddNumberParameter("Gamma", "G", "Gamma value to apply to display colours.", GH_ParamAccess.item, 1.0);
            deformationParam = pManager.AddNumberParameter("Deformation", "D", "Deformation factor for mesh, where 1.0 is the true deformation and 0 is no deformation.", GH_ParamAccess.item, 1.0);

            fieldParam = pManager[fieldParamIndex];
            componentParam = pManager[componentParamIndex];

            fieldParam.Optional = true;
            componentParam.Optional = true;
            pManager[gammaParam].Optional = true;
            pManager[deformationParam].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddIntegerParameter("Nodes", "N", "Indices of active visualization nodes.", GH_ParamAccess.list);
            //pManager.AddIntegerParameter("Faces", "F", "Visualization faces as a DataTree.", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Mesh", "M", "Output deformed mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults temp_results = null;
            scale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);

            if (!DA.GetData<FrdResults>("Results", ref temp_results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            if (!object.ReferenceEquals(temp_results, results))
            {
                results = temp_results;
                updateValues = true;

                //GetDisplacements(results);
                RebuildMesh(results);
            }

            if (results == null) return;

            double displacementFactor = 1.0;
            DA.GetData("Deformation", ref displacementFactor);


            if (displacements != null)
            {
                deformedMesh = originalMesh.DuplicateMesh();
                var N = (int)Math.Min(displacements.Length, deformedMesh.Vertices.Count);
                for (int i = 0; i < N; ++i)
                {
                    deformedMesh.Vertices[i] = deformedMesh.Vertices[i] + displacements[i] * (float)displacementFactor;
                }
                creasesDeformed = deformedMesh.ExtractCreases(0.3).ToArray();
            }

            double gamma = 1.0;
            DA.GetData("Gamma", ref gamma);

            // First, handle the UI
            string temp_field = "", temp_component = "", new_field = activeField, new_component = activeComponents[activeField];

            DA.GetData("Field", ref temp_field);
            DA.GetData("Component", ref temp_component);

            if (fields.ContainsKey(temp_field))
            {
                new_field = temp_field;
                new_component = activeComponents[new_field];
            }

            if (fields[new_field].ContainsKey(temp_component))
                new_component = temp_component;

            if (new_field != activeField)
            {
                activeField = new_field;
                activeComponents[activeField] = new_component;
                UpdateComponentSources();
                updateValues = true;
                //ExpireSolution(true);
            }

            if (updateValues || new_component != activeComponents[activeField])
            {
                updateValues = false;
                if (fields[activeField].ContainsKey(new_component))
                    activeComponents[activeField] = new_component;

                string af = activeField, ac = activeComponents[activeField];
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{af} : {ac} (new)");
                Message = $"{ac}";

                fields.TryGetValue(af, out Dictionary<string, float[]> components);
                if (components == null) return;

                components.TryGetValue(ac, out float[] values);
                if (values == null) return;

                var max = Math.Max(Math.Abs(values.Min()), Math.Abs(values.Max()));

                if ((af == "DISP" && ac == "ALL") || ac == "VONMISES")
                {
                    visualizationGradient = new UnsignedGradient(max);
                    visualizationGradient.Stops = new System.Drawing.Color[]
                    {
                    System.Drawing.Color.Blue,
                    System.Drawing.Color.White,
                    //System.Drawing.Color.Lime,
                    //System.Drawing.Color.Yellow,
                    System.Drawing.Color.Red
                    };
                }
                else
                {
                    visualizationGradient = new SignedGradient(-max, max);
                    visualizationGradient.Stops = new System.Drawing.Color[]
                    {
                    System.Drawing.Color.Blue,
                    System.Drawing.Color.White,
                    System.Drawing.Color.Red
                    };
                }

                visualizationColors = new System.Drawing.Color[values.Length];
                for (int i = 0; i < values.Length; ++i)
                {
                    //visualizationColors[i] = visualizationGradient.GetValue(ApplyGamma(values[i], gamma));
                    visualizationColors[i] = visualizationGradient.GetValue(values[i]);
                }
            }

            if (deformedMesh != null && visualizationColors != null)
            {
                deformedMesh.VertexColors.Clear();
                deformedMesh.VertexColors.AppendColors(visualizationColors);
            }

            if (deformedMesh != null)
                DA.SetData("Mesh", deformedMesh);
        }

        protected double[] ApplyGamma(double[] data, double gamma)
        {
            return data.Select(x => 1.0 - Math.Pow(1.0 - x, gamma)).ToArray();
        }

        protected double ApplyGamma(double data, double gamma)
        {
            return 1.0 - Math.Pow(1.0 - data, gamma);
        }

        protected void RebuildMesh(FrdResults results)
        {
            var comparer = new CompareIntArraySlow();
            //var cellNeighbours = new Dictionary<int[], int>(nTetra, comparer);

            var cellSet = new HashBucket<int[]>(comparer);

            foreach (var element in results.Elements)
            {
                foreach (var face in Utility.GetElementVisualizationFaces(element))
                {
                    if (face.Length > 0)
                        cellSet.Add(face);
                }
            }

            var faces = cellSet.GetUnique();

            // Get visualization nodes
            var nodeSet = new HashSet<int>();
            foreach (var face in faces)
            {
                foreach (var f in face)
                {
                    nodeSet.Add(f);
                }
            }

            // Create visualization node list and remap visualization faces
            var nodeMap = new Dictionary<int, int>();
            var originalNodeMap = new Dictionary<int, Point3d>();
            var vizNodes = new List<int>();

            int counter = 0;
            int index = 0;

            foreach (var node in results.Nodes)
            {
                if (nodeSet.Contains(node.Id))
                {
                    nodeMap[node.Id] = counter;
                    vizNodes.Add(index);
                    originalNodeMap[node.Id] = new Point3d(node.X, node.Y, node.Z) * scale;
                    counter++;
                }
                index++;
            }

            //for (int i = 0; i < faces.Count; ++i)
            //{
            //    for (int j = 0; j < faces[i].Length; ++j)
            //    {
            //        faces[i][j] = nodeMap[faces[i][j]];
            //    }
            //}

            //var nodeMap2 = new Dictionary<int, Point3d>();
            var distortedNodeMap = new Dictionary<int, Point3d>();


            //for (int i = 0; i < results.Nodes.Count; ++i)
            //{
            //    var node = results.Nodes[i];
            //    nodeMap[node.Id] = new Point3d(node.X, node.Y, node.Z);
            //}

            //for (int i = 0; i < vizNodes.Count; ++i)
            //{
            //    var node = results.Nodes[vizNodes[i]];
            //    nodeMap2[i] = new Point3d(node.X, node.Y, node.Z) * scale;
            //}

            originalMesh = Utility.CreateShellMesh(originalNodeMap, faces);
            creasesOriginal = originalMesh.ExtractCreases(0.3).ToArray();

            PopulateFields(results, vizNodes);

            originalMesh.UnifyNormals();
        }

        public void PopulateFields(FrdResults results, List<int> indices)
        {
            fields = new Dictionary<string, Dictionary<string, float[]>>();
            /*
            fields = new Dictionary<string, Dictionary<string, float[]>>
            {
                {
                    "STRESS", new Dictionary<string, float[]>
                    {
                        { "SXX", null },
                        { "SYY", null },
                        { "SZZ", null },
                        { "SXY", null },
                        { "SYZ", null },
                        { "SZX", null },
                    }
                },
                {
                    "TOSTRAIN", new Dictionary<string, float[]>
                    {
                        { "EXX", null },
                        { "EYY", null },
                        { "EZZ", null },
                        { "EXY", null },
                        { "EYZ", null },
                        { "EZX", null },
                    }
                },
                {
                    "DISP", new Dictionary<string, float[]>
                    {
                        { "D1", null },
                        { "D2", null },
                        { "D3", null },
                    }
                },
                {
                    "ERROR", new Dictionary<string, float[]>
                    {
                        { "STR (%)", null }
                    }
                }
            };



            var fieldList = fields.Keys.ToArray();

            for (int i = 0; i < fieldList.Length; ++i)
            {
                var fieldName = fieldList[i];
                var fieldData = fields[fieldList[i]];

                if (!results.Fields.ContainsKey(fieldName))
                    continue;

                var componentList = fields[fieldName].Keys.ToArray();
                for (int j = 0; j < componentList.Length; ++j)
                {
                    var componentName = componentList[j];
                    results.Fields[fieldName].TryGetValue(componentName, out float[] temp);

                    fields[fieldName][componentName] = indices.Select(x => temp[x]).ToArray();
                }
            }
            */

            foreach(var kvp in results.Fields)
            {
                var fieldName = kvp.Key;
                var fieldData = new Dictionary<string, float[]>();


                foreach (var kvp2 in results.Fields[fieldName])
                {
                    fieldData.Add(kvp2.Key, indices.Select(x => kvp2.Value[x]).ToArray());
                }
                if (fieldData.Count > 0)
                {
                    fields.Add(fieldName, fieldData);

                    if (!activeComponents.ContainsKey(fieldName))
                    {
                        activeComponents[fieldName] = fieldData.Keys.First();
                    }
                }
            }

            if(string.IsNullOrEmpty(activeField) && fields.Count > 0)
            {
                activeField = fields.Keys.First();
            }

            // Calculate stress principal invariants and von Mises
            if (fields.ContainsKey("STRESS"))
            {
                var stressField = fields["STRESS"];

                float[] sSigned, sMax, sMid, sMin;
                float[] sxx = stressField["SXX"],
                    syy = stressField["SYY"],
                    szz = stressField["SZZ"],
                    sxy = stressField["SXY"],
                    syz = stressField["SYZ"],
                    szx = stressField["SZX"];

                float[] vm = Utility.CalculateVonMises(sxx, syy, szz, sxy, syz, szx);
                Utility.ComputePrincipalInvariants(sxx, syy, szz, sxy, syz, szx, out sSigned, out sMax, out sMid, out sMin);

                fields["STRESS"].Add("VONMISES", vm);
                fields["STRESS"].Add("SIGNED", sSigned);
            }

            // Calculate strain principal invariants and von Mises
            if (fields.ContainsKey("TOSTRAIN"))
            {
                var strainField = fields["TOSTRAIN"];

                float[] eSigned, eMax, eMid, eMin;
                float[] exx = strainField["EXX"],
                    eyy = strainField["EYY"],
                    ezz = strainField["EZZ"],
                    exy = strainField["EXY"],
                    eyz = strainField["EYZ"],
                    ezx = strainField["EZX"];

                float[]vm = Utility.CalculateVonMises(exx, eyy, ezz, exy, eyz, ezx);
                Utility.ComputePrincipalInvariants(exx, eyy, ezz, exy, eyz, ezx, out eSigned, out eMax, out eMid, out eMin);

                fields["TOSTRAIN"].Add("VONMISES", vm);
                fields["TOSTRAIN"].Add("SIGNED", eSigned);
            }

            // Calculate displacement vector and scale displacements to model units
            if (fields.ContainsKey("DISP"))
            {
                var dispField = fields["DISP"];
                displacements = new Vector3f[originalMesh.Vertices.Count];

                if (scale != 1.0)
                {
                    for (int i = 0; i < displacements.Length; ++i)
                    {
                        dispField["D1"][i] = dispField["D1"][i] * (float)scale;
                        dispField["D2"][i] = dispField["D2"][i] * (float)scale;
                        dispField["D3"][i] = dispField["D3"][i] * (float)scale;
                    }
                }

                float[] all = new float[displacements.Length];

                for (int i = 0; i < displacements.Length; ++i)
                {
                    displacements[i] = new Vector3f(dispField["D1"][i], dispField["D2"][i], dispField["D3"][i]);
                    all[i] = displacements[i].Length;
                }

                fields["DISP"].Add("ALL", all);
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (deformedMesh != null)
            {
                //var material = new DisplayMaterial(System.Drawing.Color.White);
                //args.Display.DrawMeshShaded(deformedMesh, material);
                args.Display.DrawMeshFalseColors(deformedMesh);
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (creasesOriginal != null)
                args.Display.DrawLines(creasesOriginal, System.Drawing.Color.White, 1);
            if (creasesDeformed != null)
                args.Display.DrawLines(creasesDeformed, System.Drawing.Color.Black, 1);
        } 

        protected override void BeforeSolveInstance()
        {
            if (fields == null) return;

            if (fieldValueList == null)
            {
                if (fieldParam.Sources.Count == 0)
                {
                    fieldValueList = new GH_ValueList();
                }
                else
                {
                    foreach (var source in fieldParam.Sources)
                    {
                        if (source is GH_ValueList) fieldValueList = source as GH_ValueList;
                        return;
                    }
                }

                fieldValueList.CreateAttributes();
                fieldValueList.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 31);
                fieldValueList.ListItems.Clear();

                foreach (string fieldName in fields.Keys)
                {
                    fieldValueList.ListItems.Add(new GH_ValueListItem(fieldName, $"\"{fieldName}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(fieldValueList, false);
                fieldParam.AddSource(fieldValueList);
                fieldParam.CollectData();
            }

            if (componentValueList == null)
            {
                if (componentParam.Sources.Count == 0)
                {
                    componentValueList = new GH_ValueList();
                }
                else
                {
                    foreach (var source in componentParam.Sources)
                    {
                        if (source is GH_ValueList) componentValueList = source as GH_ValueList;
                        return;
                    }
                }

                componentValueList.CreateAttributes();
                componentValueList.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 61);
                componentValueList.ListItems.Clear();

                foreach (string componentName in fields[activeField].Keys)
                {
                    componentValueList.ListItems.Add(new GH_ValueListItem(componentName, $"\"{componentName}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(componentValueList, false);
                componentParam.AddSource(componentValueList);
                componentParam.CollectData();
            }
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
            get { return new Guid("7466fcc1-f219-420c-815d-d542e97a1a1c"); }
        }
    }
}
