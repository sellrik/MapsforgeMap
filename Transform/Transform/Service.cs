using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace Transform
{
    public class Service
    {
        public void CopyTagsFromRelationToWay(string sourceFilename, string targetFilename)
        {
            var wayList = new HashSet<KeyValuePair<string, string>>(); // Way id, tag

            using (var reader = XmlReader.Create(sourceFilename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "relation")
                        {
                            var element = XNode.ReadFrom(reader) as XElement;
                            if (element != null)
                            {
                                var jelElement = element
                                    .Elements()
                                    .Where(i => i.Name == "tag")
                                    .Where(i => i.Attribute("k").Value == "jel")
                                    .FirstOrDefault();

                                if (jelElement != null)
                                {
                                    var memberWays = element
                                        .Elements()
                                        .Where(i => i.Name == "member")
                                        .Where(i => i.Attribute("type").Value == "way");

                                    foreach (var item in memberWays)
                                    {
                                        var wayId = item.Attribute("ref").Value;
                                        wayList.Add(new KeyValuePair<string, string>(wayId, jelElement.ToString()));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var wayIdList = wayList
                .Select(i => i.Key)
                .Distinct()
                .ToHashSet();

            using (var sw = new StreamWriter(targetFilename, false))
            using (var sr = new StreamReader(sourceFilename))
            {
                var wayId = string.Empty;
                var isWayPartOfRelation = false;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (line.Trim().StartsWith("<way"))
                    {
                        var attributes = GetAttributes(line);
                        wayId = attributes.FirstOrDefault(i => i.Key == "id").Value;

                        if (wayIdList.Contains(wayId))
                        {
                            isWayPartOfRelation = true;
                        }
                        else
                        {
                            isWayPartOfRelation = false;
                        }
                    }
                    else if (line.Trim().EndsWith("</way>"))
                    {
                        if (isWayPartOfRelation)
                        {
                            var tags = wayList.Where(i => i.Key == wayId);
                            foreach (var tag in tags)
                            {
                                sw.WriteLine($"\t{tag.Value}");
                            }
                        }

                        wayId = string.Empty;
                        isWayPartOfRelation = false;
                    }

                    sw.WriteLine(line);
                }
            }
        }

        public void CopyTagsFromRelationToNode(string sourceFilename, string targetFilename)
        {
            var supportedJelTags = new[]
            {
                "k",
                "k+",
                "k3",
                "k4",
                "kq",
                "kb",
                "kl",
                "kpec",
                "p",
                "p+",
                "p3",
                "p4",
                "pq",
                "pb",
                "pl",
                "s",
                "s+",
                "s3",
                "s4",
                "sq",
                "sb",
                "sl",
                "z",
                "z+",
                "z3",
                "z4",
                "zq",
                "zb",
                "zl",
                "kc",
                "pc",
                "sc",
                "zc"
            };

            var wayList = new HashSet<KeyValuePair<string, string>>(); // Way id, tag

            using (var reader = XmlReader.Create(sourceFilename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "relation")
                        {
                            var element = XNode.ReadFrom(reader) as XElement;
                            if (element != null)
                            {
                                var jelElement = element
                                    .Elements()
                                    .Where(i => i.Name == "tag")
                                    .Where(i => i.Attribute("k").Value == "jel")
                                    .FirstOrDefault();

                                if (jelElement != null)
                                {
                                    var jelValue = jelElement.Attribute("v").Value;

                                    if (!supportedJelTags.Contains(jelValue))
                                    {
                                        continue;
                                    }

                                    var memberWays = element
                                        .Elements()
                                        .Where(i => i.Name == "member")
                                        .Where(i => i.Attribute("type").Value == "way");

                                    foreach (var item in memberWays)
                                    {
                                        var wayId = item.Attribute("ref").Value;
                                        wayList.Add(new KeyValuePair<string, string>(wayId, jelValue));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var wayIdList = wayList
                .Select(i => i.Key)
                .Distinct()
                .ToHashSet();


            //var nodeList = new List<(string WayId, string NodeId, string Tag, int WayNodeCount, int NodeIndexInWay)>();
            var nodeList = new List<(string NodeId, string Tag, int WayNodeCount, int NodeIndexInWay)>();

            using (var reader = XmlReader.Create(sourceFilename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "way")
                        {
                            var element = XNode.ReadFrom(reader) as XElement;
                            if (element != null)
                            {
                                var wayId = element.Attribute("id")?.Value;
                                if (wayIdList.Contains(wayId))
                                {
                                    var nodes = element
                                        .Elements()
                                        .Where(i => i.Name == "nd");

                                    var wayNodeCount = nodes.Count();
                                    var wayNodeCounter = 0;

                                    foreach (var item in nodes)
                                    {
                                        var nodeId = item.Attribute("ref").Value;

                                        var ways = wayList
                                            .Where(i => i.Key == wayId);

                                        foreach (var way in ways)
                                        {
                                            if (!default(KeyValuePair<string, string>).Equals(way))
                                            {
                                                //nodeList.Add((wayId, nodeId, way.Value, wayNodeCount, wayNodeCounter));
                                                nodeList.Add((nodeId, way.Value, wayNodeCount, wayNodeCounter));
                                            }
                                        }

                                        wayNodeCounter++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            nodeList = nodeList
                .Distinct()
                .ToList();

            var nodeIdList = nodeList
                .Select(i => i.NodeId)
                .Distinct()
                .ToHashSet();

            var nodeIdCounter = 0;

            using (var sw = new StreamWriter(targetFilename, false))
            using (var sr = new StreamReader(sourceFilename))
            {
                sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
                sw.WriteLine("<osm version=\"0.6\" generator=\"\">");

                var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ssZ");

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (line.Trim().StartsWith("<node"))
                    {
                        var attributes = GetAttributes(line);
                        var nodeId = attributes.FirstOrDefault(i => i.Key == "id").Value;

                        if (nodeIdList.Contains(nodeId))
                        {
                            var nodes = nodeList.Where(i => i.NodeId == nodeId);

                            if (nodes.Any())
                            {
                                nodeIdCounter--;

                                var nodeAttributes = GetAttributes(line);
                                var lat = nodeAttributes.FirstOrDefault(i => i.Key == "lat").Value;
                                var lon = nodeAttributes.FirstOrDefault(i => i.Key == "lon").Value;

                                var nodeText = $"  <node id=\"{nodeIdCounter}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{lat}\" lon=\"{lon}\">";
                               
                                sw.WriteLine(nodeText);

                                foreach (var node in nodes)
                                {
                                    sw.WriteLine($"     <tag k=\"jel\" v=\"{node.Tag}\" />");
                                }

                                sw.WriteLine("  </node>");
                            }
                        }
                    }
                    else
                    {
                        if (line.Contains("<bounds"))
                        {
                            sw.WriteLine(line);
                        }
                    }
                }

                sw.WriteLine("</osm>");
            }
        }

        List<KeyValuePair<string, string>> GetAttributes(string line)
        {
            var result = new List<KeyValuePair<string, string>>();

            var attributes = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < attributes.Length; i++)
            {
                var split = attributes[i].Replace("\"", "").Replace("/>", "").Replace(">", "").Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length > 1)
                {
                    var name = split[0];
                    var value = split[1];
                    result.Add(new KeyValuePair<string, string>(name, value));
                }
            }

            return result;
        }
    }
}

