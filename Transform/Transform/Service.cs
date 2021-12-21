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
        string[] supportedJelTags = new[]
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

        Dictionary<string, string> jelTagColors = new Dictionary<string, string>()
        {
            { "k", "#0000ff" },
            { "p", "#ff0000" },
            { "z", "#008000" },
            { "s", "#ffcc00" }
        };

        Dictionary<string, string> specialJelTagColors = new Dictionary<string, string>()
        {
            { "but", "#050505" },
            { "ll", "#ac31d8" },
            { "lm", "#6c207e" },
            { "ltmp", "#6c207e" },
            { "t", "#b0aaa0" }
        };

        List<KeyValuePair<string, int>> jelTagColorPriority = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("k", 1),
            new KeyValuePair<string, int>("p", 2),
            new KeyValuePair<string, int>("z", 3),
            new KeyValuePair<string, int>("s", 4),
        };

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

        public void CreateTagNodes(string sourceFilename, string targetFilename)
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


            // TODO: ez hogy lehet? A térkép területén kívülre esnek?

            //var x = wayList
            //    .SelectMany(i => i.Nodes)
            //    .Where(i => i.Lat == 0)
            //    .ToList();

            //var y = wayList
            //    .Where(i => i.Nodes.Any(j => j.Lat == 0))
            //    .ToList();

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
            var nodeIdCounter = 100000000000;

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
                                Id = (++nodeIdCounter).ToString(),
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

                var orderedNodeList = nodeList
                    .OrderBy(i => long.Parse(i.Id));

                foreach (var node in orderedNodeList)
                {
                    var nodeText = $"  <node id=\"{node.Id}\" version=\"1\" timestamp =\"{timestamp}\" uid=\"0\" user=\"\" lat =\"{node.Lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{node.Lon.ToString(CultureInfo.InvariantCulture)}\">";

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

        public void GenerateConfig()
        {
            using (var sw = new StreamWriter("config.txt"))
            {
                sw.WriteLine("Tag-mapping:");

                sw.WriteLine("\t<!-- Hiking trails -->");
                sw.WriteLine("\t<ways>");
                foreach (var item in supportedJelTags)
                {
                    sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"{item}\" zoom-appear=\"13\" />");
                }
                sw.WriteLine("\t</ways>");

                sw.WriteLine("\t<!-- Hiking trail symbols -->");
                for (int i = 1; i <= 3; i++)
                {
                    sw.WriteLine("\t<pois>");
                    foreach (var item in supportedJelTags)
                    {
                        sw.WriteLine($"\t\t<osm-tag key=\"jel\" value=\"l{i}_{item}\" zoom-appear=\"14\" />");
                    }
                    sw.WriteLine("\t</pois>");
                }

                sw.WriteLine();
                sw.WriteLine("Theme:");

                var supportedJelTagsOrderedByPriority = supportedJelTags
                    .Select(i => new
                    {
                        Value = i,
                        Priority = jelTagColorPriority.FirstOrDefault(j => j.Key == i.Substring(0, 1)).Value
                    })
                    .Select(i => new
                    {
                        Value = i.Value,
                        Priority = i.Priority == default(int) ? int.MaxValue : i.Priority,
                    })
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.Value.Length)
                    .Select(i => i.Value)
                    .ToArray();

                foreach (var item in supportedJelTagsOrderedByPriority)
                {
                    var color = GetColor(item);

                    sw.WriteLine($"\t<rule e=\"way\" k=\"jel\" v =\"{item}\">");
                    sw.WriteLine($"\t\t<line stroke=\"{color}\" stroke-width=\"5\"/>");
                    sw.WriteLine("\t</rule>");
                }

                for (int i = 1; i <= 3; i++)
                {
                    foreach (var item in supportedJelTags)
                    {
                        var color = GetColor(item);

                        sw.WriteLine($"\t<rule e=\"node\" k=\"jel\" v=\"l{i}_{item}\" zoom-min=\"14\" zoom-max=\"14\">");
                        sw.WriteLine($"\t\t<symbol src=\"file:/symbol/jel_{item}.png\"/>");
                        sw.WriteLine("\t</rule>");
                    }
                }
            }

            string GetColor(string jel)
            {
                if (specialJelTagColors.TryGetValue(jel, out var value))
                {
                    return value;
                }

                return jelTagColors[jel.Substring(0, 1)];
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

