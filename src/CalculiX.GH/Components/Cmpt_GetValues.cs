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
using Eto.Drawing;

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
using GH_IO.Serialization;
using Grasshopper.Kernel.Components;
using Eto.Drawing;

namespace CalculiX.GH.Components
{


    public class Cmpt_GetValues : GH_Component
    {
        public Cmpt_GetValues()
            : base ("GetValues", "Val", "Get the results of a specific field and component.", Api.ComponentCategory, "Results")
        {
            activeComponents = new Dictionary<string, string>();
            activeField = string.Empty;

            //Params.ParameterChanged += Params_ParameterChanged;
            //Params.OnParametersChanged();
            Params.ParameterSourcesChanged += Params_ParameterSourcesChanged;
        }


        protected override System.Drawing.Bitmap Icon => Properties.Resources.Visualization_24x24;
        public override Guid ComponentGuid => new Guid("6681eefe-857a-4a15-82ce-e20b1b9ff335");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override BoundingBox ClippingBox
        {
            get {

                return BoundingBox.Empty;
            }
        }

        int resultsParam = 0;
        IGH_Param fieldParam = null, componentParam = null;
        double scale = 1.0; // assuming SI units (meters)

        // Step
        int activeStep = 1;

        // Results and fields
        bool reload = false;
        FrdResults results = null;
        Dictionary<string, Dictionary<string, ResultComponent>> fields = null;

        // Active field and components
        Dictionary<string, string> activeComponents = null;
        string activeField = ""; 

        // Connected field and component sources
        bool updateValues = false;
        GH_ValueList fieldValueList = null, componentValueList = null;

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

        public override bool Write(GH_IWriter writer)
        {
            if (activeField != null)
            {
                writer.SetString("active_field", activeField);

                string activeComponent = activeComponents.ContainsKey(activeField) ? activeComponents[activeField] : "";
                writer.SetString("active_component", activeComponent);

            }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("active_field", ref activeField);
            var component = "";
            reader.TryGetString("active_component", ref component);

            activeComponents[activeField] = component;
            return base.Read(reader);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            resultsParam = pManager.AddGenericParameter("Results", "R", "FrdResults object.", GH_ParamAccess.item);
            int stepParamIndex = pManager.AddIntegerParameter("Step", "S", "Step number to display.", GH_ParamAccess.item, 1);
            int fieldParamIndex = pManager.AddTextParameter("Field", "F", "Field to display on mesh.", GH_ParamAccess.item, "STRESS");
            int componentParamIndex = pManager.AddTextParameter("Component", "C", "Component of field to display on mesh.", GH_ParamAccess.item, "SXX");
            fieldParam = pManager[fieldParamIndex];
            componentParam = pManager[componentParamIndex];

            fieldParam.Optional = true;
            componentParam.Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "V", "Output values.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            FrdResults temp_results = null;
            scale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);

            // Get results
            if (!DA.GetData<FrdResults>("Results", ref temp_results))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to parse results.");
                return;
            }

            if (!object.ReferenceEquals(temp_results, results))
            {
                results = temp_results;
                updateValues = true;
            }

            if (results == null) return;
            if (results.Fields.Count < 1) return;

            // Get valid step
            int stepId = -1;
            DA.GetData("Step", ref stepId);

            if (!results.Fields.ContainsKey(stepId))
            {
                stepId = results.Fields.Last().Key;
            }

            if (stepId != activeStep)
            {
                updateValues = true;
            }

            activeStep = stepId;

            // Update values
            if (updateValues)
                PopulateFields(results, activeStep);

            // Get valid field
            if (string.IsNullOrEmpty(activeField) || !results.Fields[activeStep].ContainsKey(activeField))
            {
                activeField = results.Fields[activeStep].First().Key;
            }

            // First, handle the UI
            string temp_field = "", temp_component = "", new_field = activeField;
            activeComponents.TryGetValue(activeField, out string new_component);

            if (string.IsNullOrEmpty(new_component))
            {
                new_component = results.Fields[activeStep][activeField].First().Key;
            }

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
                Message = $"{activeStep} : {ac}";

                fields.TryGetValue(af, out Dictionary<string, ResultComponent> components);
                if (components == null) return;

                components.TryGetValue(ac, out ResultComponent component);
                if (component == null) return;

                var values = component.Values;
                var max = Math.Max(Math.Abs(values.Min()), Math.Abs(values.Max()));

                DA.SetDataList("Values", component.Values);
            }
        }

        public void PopulateFields(FrdResults results, int stepId)
        {
            fields = new Dictionary<string, Dictionary<string, ResultComponent>>();
            var step = results.Fields[stepId];
 
            foreach(var kvp in step)
            {
                var fieldName = kvp.Key;
                var fieldData = new Dictionary<string, ResultComponent>();


                foreach (var kvp2 in step[fieldName])
                {
                    fieldData.Add(kvp2.Key, new ResultComponent(kvp2.Value));
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
                float[] sxx = stressField["SXX"].Values,
                    syy = stressField["SYY"].Values,
                    szz = stressField["SZZ"].Values,
                    sxy = stressField["SXY"].Values,
                    syz = stressField["SYZ"].Values,
                    szx = stressField["SZX"].Values;

                float[] vm = Utility.CalculateVonMises(sxx, syy, szz, sxy, syz, szx);
                Utility.ComputePrincipalInvariants(sxx, syy, szz, sxy, syz, szx, out sSigned, out sMax, out sMid, out sMin);

                fields["STRESS"].Add("VONMISES", new ResultComponent(vm));
                fields["STRESS"].Add("SIGNED", new ResultComponent(sSigned));
            }

            // Calculate strain principal invariants and von Mises
            if (fields.ContainsKey("TOSTRAIN"))
            {
                var strainField = fields["TOSTRAIN"];

                float[] eSigned, eMax, eMid, eMin;
                float[] exx = strainField["EXX"].Values,
                    eyy = strainField["EYY"].Values,
                    ezz = strainField["EZZ"].Values,
                    exy = strainField["EXY"].Values,
                    eyz = strainField["EYZ"].Values,
                    ezx = strainField["EZX"].Values;

                float[]vm = Utility.CalculateVonMises(exx, eyy, ezz, exy, eyz, ezx);
                Utility.ComputePrincipalInvariants(exx, eyy, ezz, exy, eyz, ezx, out eSigned, out eMax, out eMid, out eMin);

                fields["TOSTRAIN"].Add("VONMISES", new ResultComponent(vm));
                fields["TOSTRAIN"].Add("SIGNED", new ResultComponent(eSigned));
            }

            // Calculate displacement vector and scale displacements to model units
            if (fields.ContainsKey("DISP"))
            {
                var dispField = fields["DISP"];

                if (scale != 1.0)
                {
                    for (int i = 0; i < results.Nodes.Count; ++i)
                    {
                        dispField["D1"].Values[i] = dispField["D1"].Values[i] * (float)scale;
                        dispField["D2"].Values[i] = dispField["D2"].Values[i] * (float)scale;
                        dispField["D3"].Values[i] = dispField["D3"].Values[i] * (float)scale;
                    }
                }

                float[] all = new float[results.Nodes.Count];

                for (int i = 0; i < results.Nodes.Count; ++i)
                {
                    var dv = new Vector3f(dispField["D1"].Values[i], dispField["D2"].Values[i], dispField["D3"].Values[i]);
                    all[i] = dv.Length;
                }

                fields["DISP"].Add("ALL", new ResultComponent(all));
            }
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
                fieldValueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 31);
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
                componentValueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 91);
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
    }
}
