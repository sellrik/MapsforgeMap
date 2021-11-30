using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using Transform.Model;
using System.Globalization;

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

        public void Test(string sourceFilename, string targetFilename)
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

            var wayList = new List<Way>();

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

                                        var way = wayList.FirstOrDefault(i => i.Id == wayId);

                                        if (way == null)
                                        {
                                            way = new Way
                                            {
                                                Id = wayId
                                            };

                                            wayList.Add(way);
                                        }

                                        if (!way.Tags.Contains(jelValue))
                                        {
                                            way.Tags.Add(jelValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var wayIdList = wayList
                .Select(i => i.Id)
                .ToHashSet();

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
                                    var way = wayList.FirstOrDefault(i => i.Id == wayId);

                                    if (way == null)
                                    {
                                        continue; // ?
                                    }

                                    var nodes = element
                                        .Elements()
                                        .Where(i => i.Name == "nd")
                                        .ToArray();

                                    var wayNodeCount = nodes.Count();

                                    for (int i = 0; i < wayNodeCount; i++)
                                    {
                                        var node = nodes.ElementAt(i);

                                        var nodeId = node.Attribute("ref").Value;

                                        way.Nodes.Add(new WayNode
                                        {
                                            Id = nodeId,
                                            Index = i
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var wayNodeIdList = wayList
                .SelectMany(i => i.Nodes)
                .Select(i => i.Id)
                .ToHashSet();

            using (var reader = XmlReader.Create(sourceFilename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "node")
                        {
                            var element = XNode.ReadFrom(reader) as XElement;
                            if (element != null)
                            {
                                var nodeId = element.Attribute("id").Value;

                                if (!wayNodeIdList.Contains(nodeId))
                                {
                                    continue;
                                }

                                var latText = element.Attribute("lat").Value;
                                var lonText = element.Attribute("lon").Value;

                                var lat = double.Parse(latText, CultureInfo.InvariantCulture);
                                var lon = double.Parse(lonText, CultureInfo.InvariantCulture);

                                var nodes = wayList
                                    .SelectMany(i => i.Nodes)
                                    .Where(i => i.Id == nodeId);

                                foreach (var node in nodes)
                                {
                                    node.Lat = lat;
                                    node.Lon = lon;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var way in wayList)
            {
                var nodes = way.Nodes
                    .Where(i => i.Lat > 0)
                    .Where(i => i.Lon > 0)
                    .OrderBy(i => i.Index)
                    .ToArray();

                var distanceOfWay = 0d;

                for (int i = 1; i < nodes.Length; i++)
                {
                    var previousNode = nodes[i - 1];
                    var node = nodes[i];

                    var distanceBetweenNodes = Utils.CalculateDistance(previousNode.Lat, previousNode.Lon, node.Lat, node.Lon);
                    node.DistanceFromPreviousNode = distanceBetweenNodes;
                    distanceOfWay += distanceBetweenNodes;
                }

                way.Distance = distanceOfWay;
            }

            // TODO: ez hogy lehet? A térkép területén kívülre esnek?

            //var x = wayList
            //    .SelectMany(i => i.Nodes)
            //    .Where(i => i.Lat == 0)
            //    .ToList();

            //var y = wayList
            //    .Where(i => i.Nodes.Any(j =>  j.Lat == 0))
            //    .ToList();

            //var z = wayList
            //    .Where(i => i.Nodes.Where(j => j.Lat > 0 && j.Lon > 0).Count() > 1)
            //    .Where(i => i.Distance == 0);

            var waysWithDistance = wayList
                .Where(i => i.Distance > 0);

            var waysWithoutDistance1 = wayList
                .Where(i => i.Nodes.Any())
                .Where(i => i.Distance <= 0)
                .ToList();

            var waysWithoutDistance2 = wayList
                .Where(i => i.Nodes.Count > 1)
                .Where(i => i.Distance <= 0)
                .ToList();

            var waysWithMultipleTags = wayList
               .Where(i => i.Tags.Count() > 1)
               .ToList();

            var nodeList = new List<Node>();
            var nodeIdCounter = 0;

            var distanceBetweenTags = 100;

            foreach (var way in waysWithDistance)
            {
                var nodes = way.Nodes
                    .Where(i => i.Lat > 0)
                    .Where(i => i.Lon > 0)
                    .OrderBy(i => i.Index)
                    .ToArray();

                var halfOfDistance = way.Distance / 2;
                var tagOffset = 0;

                for (int i = 0; i < way.Tags.Count(); i++)
                {
                    var cumulativeDistance = 0d;

                    var nextTagDistance = distanceBetweenTags + tagOffset;
                    var tag = way.Tags[i];

                    for (int j = 1; j < nodes.Length; j++)
                    {
                        var previousNode = nodes[j - 1];
                        var node = nodes[j];

                        // Middle point of the way:
                        //var newcumulativeDistance = cumulativeDistance + node.DistanceFromPreviousNode;
                        //if (newcumulativeDistance >= halfOfDistance)
                        //{
                        //    var diff = newcumulativeDistance - halfOfDistance;
                        //    var fraction = diff / node.DistanceFromPreviousNode;
                        //    Utils.CalculateIntermediatePoint(previousNode.Lat, previousNode.Lon, node.Lat, node.Lon, fraction, out var lat, out var lon);

                        //    nodeList.Add(new Node
                        //    {
                        //        Id = (--nodeIdCounter).ToString(),
                        //        Lat = lat,
                        //        Lon = lon,
                        //        Tags = way.Tags
                        //    });

                        //    break;
                        //}
                        //else
                        //{
                        //    cumulativeDistance += node.DistanceFromPreviousNode;
                        //}

                        var newCumulativeDistance = cumulativeDistance + node.DistanceFromPreviousNode;

                        if (newCumulativeDistance >= way.Distance)
                        {
                            break;
                        }

                        if (newCumulativeDistance >= nextTagDistance)
                        {
                            var diff = newCumulativeDistance - nextTagDistance;
                            var fraction = diff / node.DistanceFromPreviousNode;
                            fraction = 1 - fraction;
                            Utils.CalculateIntermediatePoint(previousNode.Lat, previousNode.Lon, node.Lat, node.Lon, fraction, out var lat, out var lon);

                            nodeList.Add(new Node
                            {
                                Id = (--nodeIdCounter).ToString(),
                                Lat = lat,
                                Lon = lon,
                                Tags = new List<string>
                                {
                                    tag
                                }
                            });

                            nextTagDistance += distanceBetweenTags;
                        }

                        cumulativeDistance += node.DistanceFromPreviousNode;
                    }

                    tagOffset += 10;
                }
            }

            using (var sw = new StreamWriter(targetFilename, false))
            {
                sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
                sw.WriteLine("<osm version=\"0.6\" generator=\"\">");
                // TODO: bounds?

                var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ssZ");

                foreach (var node in nodeList)
                {
                    var nodeText = $"  <node id=\"{node.Id}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{node.Lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{node.Lon.ToString(CultureInfo.InvariantCulture)}\">";

                    sw.WriteLine(nodeText);

                    foreach (var tag in node.Tags)
                    {
                        sw.WriteLine($"     <tag k=\"jel\" v=\"{tag}\" />");
                    }

                    sw.WriteLine("  </node>");
                }

                //while (!sr.EndOfStream)
                //{
                //    var line = sr.ReadLine();

                //    if (line.Trim().StartsWith("<node"))
                //    {
                //        var attributes = GetAttributes(line);
                //        var nodeId = attributes.FirstOrDefault(i => i.Key == "id").Value;

                //        if (nodeIdList.Contains(nodeId))
                //        {
                //            var nodes = nodeList.Where(i => i.NodeId == nodeId);

                //            if (nodes.Any())
                //            {
                //                nodeIdCounter--;

                //                var nodeAttributes = GetAttributes(line);
                //                var lat = nodeAttributes.FirstOrDefault(i => i.Key == "lat").Value;
                //                var lon = nodeAttributes.FirstOrDefault(i => i.Key == "lon").Value;

                //                var nodeText = $"  <node id=\"{nodeIdCounter}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{lat}\" lon=\"{lon}\">";

                //                sw.WriteLine(nodeText);

                //                foreach (var node in nodes)
                //                {
                //                    sw.WriteLine($"     <tag k=\"jel\" v=\"{node.Tag}\" />");
                //                }

                //                sw.WriteLine("  </node>");
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (line.Contains("<bounds"))
                //        {
                //            sw.WriteLine(line);
                //        }
                //    }
                //}

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

