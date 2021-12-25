using Newtonsoft.Json;
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
        private readonly ConfigModel _config;

        public TrailmarkService()
        {
            var json = File.ReadAllText("config.json");
            _config = JsonConvert.DeserializeObject<ConfigModel>(json);
        }

        public void CreateTrailmarks(string sourceFilename, string targetFilename)
        {
            var ways = GetWays(sourceFilename);

            //var test = ways.SelectMany(i => i.Tags).Distinct().ToList();

            AddNodesToWays(sourceFilename, ways);

            SetCoordinatesOfNodes(sourceFilename, ways);

            CalcalateDistanceOfWay(ways);

            //var x = ways.Max(i => i.Tags.Count());
            //var y = ways.Where(i => i.Tags.Count() == x).ToList();
            //var z = ways
            //    .GroupBy(i => i.Tags.Count())
            //    .Select(i => new
            //    {
            //        TagCount = i.Key,
            //        NumberOfWays = i.Count()
            //    })
            //    .OrderBy(i => i.TagCount)
            //    .ToList();

            var nodesLevel4 = CreateTrailmarkNodes(ways, distanceBetweenTrailmarks: 100, distanceBetweenMultileTags: 20, "l4_");
            var nodesLevel3 = CreateTrailmarkNodes(ways, distanceBetweenTrailmarks: 250, distanceBetweenMultileTags: 50, "l3_");
            var nodesLevel2 = CreateTrailmarkNodes(ways, distanceBetweenTrailmarks: 500, distanceBetweenMultileTags: 100, "l2_");
            var nodesLevel1 = CreateTrailmarkNodes(ways, distanceBetweenTrailmarks: 1000, distanceBetweenMultileTags: 200, "l1_");

            var nodes = new List<Node>();
            nodes.AddRange(nodesLevel1);
            nodes.AddRange(nodesLevel2);
            nodes.AddRange(nodesLevel3);
            nodes.AddRange(nodesLevel4);

            WriteToFile(sourceFilename, targetFilename, nodes, ways);
        }

        public List<Way> GetWays(string sourceFilename)
        {
            var wayList = new List<Way>();
            var unkownJelTags = new List<string>();

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

                                    if (!_config.Trailmarks.Any(i => i.Jel == jelValue))
                                    {
                                        if (!unkownJelTags.Contains(jelValue))
                                        {
                                            unkownJelTags.Add(jelValue);
                                        }

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
                var cumulativeDistance = 0d;

                for (int i = 1; i < nodes.Length; i++)
                {
                    var previousNode = nodes[i - 1];
                    var node = nodes[i];

                    var distanceBetweenNodes = Utils.CalculateDistance(previousNode.Lat, previousNode.Lon, node.Lat, node.Lon);
                    cumulativeDistance += distanceBetweenNodes;

                    node.DistanceFromPreviousNode = distanceBetweenNodes;
                    node.CumulativeDistance = cumulativeDistance;
                    distanceOfWay += distanceBetweenNodes;
                }

                way.Distance = distanceOfWay;
            }
        }

        public List<Node> CreateTrailmarkNodes(
          List<Way> wayList,
          int distanceBetweenTrailmarks,
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

                int wayDistanceBetweenTrailmarks;

                if (way.Distance < distanceBetweenTrailmarks)
                {
                    wayDistanceBetweenTrailmarks = (int)Math.Round(way.Distance / 2, 0);
                }
                else
                {
                    wayDistanceBetweenTrailmarks = distanceBetweenTrailmarks;
                }

                if (wayDistanceBetweenTrailmarks == 0)
                {
                    continue;
                }

                foreach (var tag in way.Tags)
                {
                    var nextTrailmarkDistane = wayDistanceBetweenTrailmarks + offset;

                    for (int nodeIndex = 1; nodeIndex < way.Nodes.Count; nodeIndex++)
                    {
                        var node = way.Nodes[nodeIndex];
                        var previousNode = way.Nodes[nodeIndex - 1];

                        while (node.CumulativeDistance > nextTrailmarkDistane)
                        {
                            var diff = nextTrailmarkDistane - previousNode.CumulativeDistance;
                            var fraction = diff / node.DistanceFromPreviousNode;
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

                            nextTrailmarkDistane += wayDistanceBetweenTrailmarks;
                        }
                    }

                    offset += distanceBetweenMultileTags;
                }
            }

            return nodeList;
        }

        public void WriteToFile(
           string sourceFilename,
           string targetFilename,
           List<Node> nodeList,
           List<Way> wayList)
        {
            var nodeIdCounter = 100000000000;
            var wayIdCounter = 200000000000;

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

            var allNodes = new List<Node>();

            using (var sw = new StreamWriter(targetFilename, false))
            {
                sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
                sw.WriteLine("<osm version=\"0.6\" generator=\"\">");
                sw.WriteLine($"  {boundsNode}");

                var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ssZ");

                foreach (var node in nodeList)
                {
                    node.Id = (++nodeIdCounter).ToString();
                    allNodes.Add(node);
                }

                foreach (var way in wayList)
                {
                    way.Id = (++wayIdCounter).ToString();

                    foreach (var node in way.Nodes)
                    {
                        node.Id = (++nodeIdCounter).ToString();
                        allNodes.Add(new Node
                        {
                            Id = node.Id,
                            Lat = node.Lat,
                            Lon = node.Lon
                        });
                    }
                }

                foreach (var node in allNodes)
                {
                    var nodeText = $"\t<node id=\"{node.Id}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{node.Lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{node.Lon.ToString(CultureInfo.InvariantCulture)}\">";

                    sw.WriteLine(nodeText);

                    foreach (var tag in node.Tags)
                    {
                        sw.WriteLine($"\t\t<tag k=\"jel\" v=\"{tag}\" />");
                    }

                    sw.WriteLine("\t</node>");
                }

                foreach (var way in wayList)
                {
                    var nodeText = $"\t<way id=\"{way.Id}\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" visible=\"true\" version=\"1\" changeset=\"0\">";

                    sw.WriteLine(nodeText);

                    foreach (var node in way.Nodes)
                    {
                        sw.WriteLine($"\t\t<nd ref=\"{node.Id}\" />");
                    }

                    foreach (var tag in way.Tags)
                    {
                        sw.WriteLine($"\t\t<tag k=\"jel\" v=\"{tag}\" />");
                    }

                    sw.WriteLine("\t</way>");
                }

                sw.WriteLine("</osm>");
            }
        }

        public void GenerateConfig()
        {
            using (var sw = new StreamWriter("config.txt"))
            {
                sw.WriteLine("Tag-mapping:");

                sw.WriteLine("\t<!-- Hiking trails -->");
                sw.WriteLine("\t<ways>");

                foreach (var trailmark in _config.Trailmarks)
                {
                    sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"{trailmark.Jel}\" zoom-appear=\"13\" />");
                }

                sw.WriteLine("\t</ways>");

                sw.WriteLine("\t<!-- Hiking trail symbols -->");
                for (int i = 1; i <= 4; i++)
                {
                    sw.WriteLine("\t<pois>");
                    foreach (var trailmark in _config.Trailmarks)
                    {
                        switch (i)
                        {
                            case 1:
                                sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"l{i}_{trailmark.Jel}\" zoom-appear=\"14\" />");
                                break;
                            case 2:
                                sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"l{i}_{trailmark.Jel}\" zoom-appear=\"15\" />");
                                break;
                            case 3:
                                sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"l{i}_{trailmark.Jel}\" zoom-appear=\"16\" />");
                                break;
                            case 4:
                                sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"l{i}_{trailmark.Jel}\" zoom-appear=\"17\" />");
                                break;
                            default:
                                break;
                        }
                    }
                    sw.WriteLine("\t</pois>");
                }

                sw.WriteLine();
                sw.WriteLine("Theme:");

                var trailmarksOrderedByPriority = _config.Trailmarks
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.Jel.Length)
                    .ToArray();

                foreach (var trailmark in trailmarksOrderedByPriority)
                {
                    var color = _config.Colors.FirstOrDefault(i => i.Name == trailmark.ColorName)?.Name;
                    if (color == null)
                    {
                        Console.WriteLine($"Color was not found: {trailmark.ColorName}");
                        continue;
                    }

                    sw.WriteLine($"\t<rule e=\"way\" k=\"jel\" v =\"{trailmark.Jel}\">");
                    sw.WriteLine($"\t\t<line stroke=\"{color}\" stroke-width=\"5\"/>");
                    sw.WriteLine("\t</rule>");
                }

                for (int i = 1; i <= 4; i++)
                {
                    foreach (var trailmark in _config.Trailmarks)
                    {

                        switch (i)
                        {
                            case 1:
                                sw.WriteLine($"\t<rule e=\"node\" k=\"jel\" v=\"l{i}_{trailmark.Jel}\" zoom-min=\"14\" zoom-max=\"14\" >");
                                break;
                            case 2:
                                sw.WriteLine($"\t<rule e=\"node\" k=\"jel\" v=\"l{i}_{trailmark.Jel}\" zoom-min=\"15\" zoom-max=\"15\" >");
                                break;
                            case 3:
                                sw.WriteLine($"\t<rule e=\"node\" k=\"jel\" v=\"l{i}_{trailmark.Jel}\" zoom-min=\"16\" zoom-max=\"16\" >");
                                break;
                            case 4:
                                sw.WriteLine($"\t<rule e=\"node\" k=\"jel\" v=\"l{i}_{trailmark.Jel}\" zoom-min=\"17\" >");
                                break;
                            default:
                                break;
                        }
                        sw.WriteLine($"\t\t<symbol src=\"file:/symbol/jel_{trailmark.Jel}.png\"/>");
                        sw.WriteLine("\t</rule>");
                    }
                }
            }
        }
    }
}
