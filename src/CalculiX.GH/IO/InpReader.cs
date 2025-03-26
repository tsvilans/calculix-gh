using Rhino.Geometry;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace CalculiX.IO
{
    public static class InpReader
    { 
        private enum ReadMode
        {
            None,
            Node,
            Element,
            ElementSet,
            NodeSet
        }

        public static Model Read(string inpPath)
        {
            var basename = System.IO.Path.GetFileNameWithoutExtension(inpPath);
            var model = new Model(basename);

            Read(model, inpPath);

            return model;
        }

        public static void Read(Model model, string inpPath)
        {
            var lines = System.IO.File.ReadAllLines(inpPath);

            var mode = ReadMode.None;
            string currentElementType = "null";
            string currentSetName = "null";

            string[] tok = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("**")) continue;
                else if (line.StartsWith("*"))
                {
                    tok = line.Substring(1).Split(',');

                    if (string.Compare(tok[0], "Node", true) == 0)
                        mode = ReadMode.Node;
                    else if (string.Compare(tok[0], "Element", true) == 0)
                    {
                        currentElementType = tok[1].Split('=')[1];
                        mode = ReadMode.Element;
                    }
                    else if (string.Compare(tok[0], "Nset", true) == 0)
                    {
                        currentSetName = tok[1].Split('=')[1];
                        model.NodeSets[currentSetName] = new List<int>();
                        mode = ReadMode.NodeSet;
                    }
                    else if (string.Compare(tok[0], "Elset", true) == 0)
                    {
                        currentSetName = tok[1].Split('=')[1];
                        model.ElementSets[currentSetName] = new List<int>();
                        // currentIndices = new List<int>();
                        mode = ReadMode.ElementSet;
                    }
                    else if (string.Compare(tok[0], "Include", true) == 0)
                    {
                        var includeName = tok[1].Split('=')[1];
                        Console.WriteLine($"Including {includeName}...");

                        var directory = new System.IO.FileInfo(inpPath).Directory.FullName;
                        var includePath = System.IO.Path.Combine(directory, includeName);

                        Read(model, includePath);
                    }
                    else
                        mode = ReadMode.None;

                    continue;
                }

                switch (mode)
                {
                    case (ReadMode.Node):
                        tok = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var nodeTag = int.Parse(tok[0]);

                        var node = new Point3d(
                            double.Parse(tok[1]),
                            double.Parse(tok[2]),
                            double.Parse(tok[3]));

                        model.Nodes.Add(nodeTag, node);

                        break;

                    case (ReadMode.Element):
                        tok = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var nums = tok.Select(x => int.Parse(x)).ToArray();
                        var etags = new int[nums.Length - 1];
                        Array.Copy(nums, 1, etags, 0, etags.Length);

                        var elementId = nums[0];

                        switch (currentElementType)
                        {
                            case ("C3D4"):
                                model.Elements.Add(elementId, new ElementC3D4(elementId, etags));
                                break;
                            case ("C3D10"):
                                model.Elements.Add(elementId, new ElementC3D10(elementId, etags));
                                break;
                            case ("B31"):
                                model.Elements.Add(elementId, new ElementB31(elementId, etags[0], etags[1]));
                                break;
                            case ("B32"):
                                model.Elements.Add(elementId, new ElementB32(elementId, etags[0], etags[1], etags[2]));
                                break;
                            default:
                                break;
                        }

                        break;

                    case (ReadMode.ElementSet):
                        tok = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < tok.Length; ++i)
                        {
                            model.ElementSets[currentSetName].Add(int.Parse(tok[i]));
                        }

                        break;
                    case (ReadMode.NodeSet):
                        tok = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < tok.Length; ++i)
                        {
                            model.NodeSets[currentSetName].Add(int.Parse(tok[i]));
                        }

                        break;
                    default:
                        break;

                }
            }
        }
    } 
}
