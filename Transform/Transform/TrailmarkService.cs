using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Transform.Model;

namespace Transform
{
    public class TrailmarkService
    {
        static string[] SupportedJelTags = new[]
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

            "keml",
            "ktmp",
            "kt",
            "katl",
            "pc",
            "peml",
            "ptmp",
            "pt",
            "patl",
            "sc",
            "seml",
            "stmp",
            "st",
            "satl",
            "zc",
            "zeml",
            "ztmp",
            "zt",
            "zatl",
            "ll",
            "t",
            "ltmp",

            "lm",
            "km",
            "pm",
            "sm",
            "zm",
            "smz",
            "sgy",
            "stj",
            "ste",
            "stm",

            "palp",
            "salp",

            "but",
            "kbor",
            "pbor",
            "sbor",
            "zbor",
            "zut"
        };

        public void CreateTrailmarks(string sourceFilename, string targetFilename)
        {
            var ways = GetWays(sourceFilename);

            var test = ways.SelectMany(i => i.Tags).Distinct().ToList();

            AddNodesToWays(sourceFilename, ways);

            SetCoordinatesOfNodes(sourceFilename, ways);

            CalcalateDistanceOfWay(ways);

            var x = ways.Max(i => i.Tags.Count());
            var y = ways.Where(i => i.Tags.Count() == x).ToList();
            var z = ways
                .GroupBy(i => i.Tags.Count())
                .Select(i => new
                {
                    TagCount = i.Key,
                    NumberOfWays = i.Count()
                })
                .OrderBy(i => i.TagCount)
                .ToList();

            var nodesLevel4 = CreateTrailmarkNodes(ways, distanceBetweenNodes: 100, distanceBetweenMultileTags: 20, "l4_");
            var nodesLevel3 = CreateTrailmarkNodes(ways, distanceBetweenNodes: 250, distanceBetweenMultileTags: 50, "l3_");
            var nodesLevel2 = CreateTrailmarkNodes(ways, distanceBetweenNodes: 500, distanceBetweenMultileTags: 100, "l2_");
            var nodesLevel1 = CreateTrailmarkNodes(ways, distanceBetweenNodes: 1000, distanceBetweenMultileTags: 200, "l1_");

            var nodes = new List<Node>();
            nodes.AddRange(nodesLevel1);
            nodes.AddRange(nodesLevel2);
            nodes.AddRange(nodesLevel3);
            nodes.AddRange(nodesLevel4);

            WriteToFile(sourceFilename, targetFilename, nodes);
        }

        public List<Way> GetWays(string sourceFilename)
        {
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

                                    if (!SupportedJelTags.Contains(jelValue))
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

            return wayList;
        }

        public void AddNodesToWays(
             string sourceFilename,
             List<Way> wayList)
        {
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
        }

        public void SetCoordinatesOfNodes(
            string sourceFilename,
            List<Way> wayList)
        {
            var wayNodeIdList = wayList
               .SelectMany(i => i.Nodes)
               .Select(i => i.Id)
               .ToHashSet();

            var wayNodes = new List<Node>();

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

                                wayNodes.Add(new Node
                                {
                                    Id = nodeId,
                                    Lat = lat,
                                    Lon = lon
                                });
                            }
                        }
                    }
                }
            }


            var nodeQuery = wayList.SelectMany(i => i.Nodes)
                .Join(wayNodes, i => i.Id, j => j.Id, (i, j) => new { i, j });

            foreach (var item in nodeQuery)
            {
                item.i.Lat = item.j.Lat;
                item.i.Lon = item.j.Lon;
            }

        }

        public void CalcalateDistanceOfWay(
           List<Way> wayList)
        {
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
        }

        public List<Node> CreateTrailmarkNodes(
          List<Way> wayList,
          int distanceBetweenNodes,
          int distanceBetweenMultileTags,
          string tagPrefix)
        {
            var nodeList = new List<Node>();

            var waysWithDistance = wayList
                .Where(i => i.Distance > 0);

            foreach (var way in waysWithDistance)
            {
                var nodes = way.Nodes
                    .Where(i => i.Lat > 0)
                    .Where(i => i.Lon > 0)
                    .OrderBy(i => i.Index)
                    .ToArray();

                var offset = 0;

                int wayDistanceBetweenNodes;

                if (way.Distance < distanceBetweenNodes)
                {
                    wayDistanceBetweenNodes = (int)Math.Round(way.Distance / 2, 0);
                }
                else
                {
                    wayDistanceBetweenNodes = distanceBetweenNodes;
                }

                for (int i = 0; i < way.Tags.Count(); i++)
                {
                    var cumulativeDistance = 0d;

                    var nextNodeDistance = wayDistanceBetweenNodes + offset;

                    var tag = way.Tags[i];

                    for (int j = 1; j < nodes.Length; j++)
                    {
                        var previousNode = nodes[j - 1];
                        var node = nodes[j];

                        var newCumulativeDistance = cumulativeDistance + node.DistanceFromPreviousNode;

                        if (newCumulativeDistance >= way.Distance)
                        {
                            break;
                        }

                        if (newCumulativeDistance >= nextNodeDistance)
                        {
                            var diff = newCumulativeDistance - nextNodeDistance;
                            var fraction = diff / node.DistanceFromPreviousNode;
                            fraction = 1 - fraction;
                            Utils.CalculateIntermediatePoint(previousNode.Lat, previousNode.Lon, node.Lat, node.Lon, fraction, out var lat, out var lon);

                            nodeList.Add(new Node
                            {
                                Lat = lat,
                                Lon = lon,
                                Tags = new List<string>
                                {
                                    $"{tagPrefix}{tag}"
                                }
                            });

                            nextNodeDistance += wayDistanceBetweenNodes;
                        }

                        cumulativeDistance += node.DistanceFromPreviousNode;
                    }

                    offset += distanceBetweenMultileTags;
                }
            }

            return nodeList;
        }

        public void WriteToFile(
            string sourceFilename,
            string targetFilename,
            List<Node> nodeList)
        {
            var nodeIdCounter = 100000000000;

            var boundsNode = string.Empty;
            using (var reader = XmlReader.Create(sourceFilename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "bounds")
                        {
                            var element = XNode.ReadFrom(reader);
                            boundsNode = element.ToString();
                            break;
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(targetFilename, false))
            {
                sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
                sw.WriteLine("<osm version=\"0.6\" generator=\"\">");
                sw.WriteLine($"  {boundsNode}");

                var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ssZ");

                foreach (var node in nodeList)
                {
                    var nodeText = $"  <node id=\"{++nodeIdCounter}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{node.Lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{node.Lon.ToString(CultureInfo.InvariantCulture)}\">";

                    sw.WriteLine(nodeText);

                    foreach (var tag in node.Tags)
                    {
                        sw.WriteLine($"     <tag k=\"jel\" v=\"{tag}\" />");
                    }

                    sw.WriteLine("  </node>");
                }

                sw.WriteLine("</osm>");
            }
        }
    }
}
