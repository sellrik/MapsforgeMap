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
        public void TestReader()
        {
            var filename = "test.osm";

            using (var reader = XmlReader.Create(filename))
            {
                reader.MoveToContent();

                var isRelation = false;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "relation")
                        {
                            var element = XNode.ReadFrom(reader) as XElement;
                            if (element != null)
                            {

                            }

                            isRelation = true;
                            Console.WriteLine($"Relation. Id: {reader.GetAttribute("id")} ");
                            continue;

                        }

                        if (isRelation)
                        {
                            Console.WriteLine(reader.Name);
                        }
                    }

                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "relation")
                        {
                            isRelation = false;
                        }
                    }
                }
            }
        }
        public void TestReader2()
        {
            var filename = "test.osm";
            filename = "budaihegyseg.osm";

            var relations = GetRelations()
                .Where(i => i.Elements().Any(i => ((XElement)i).Attribute("k")?.Value == "jel"));

            var wayIds = relations
                .SelectMany(i =>
                    i.Elements()
                    .Where(i => i.Name == "member")
                    .Where(i => i.Attribute("type").Value == "way"))
                .Select(i => i.Attribute("ref").Value)
                .ToArray();

            var ways = GetWays(wayIds);

            var nodeIds = ways
                .SelectMany(i =>
                    i.Elements()
                    .Where(i => i.Name == "nd"))
                .Select(i => i.Attribute("ref").Value)
                .ToArray();

            var nodes = GetNodes(nodeIds).ToArray();
            //var ways = GetWays()
            //        .Where(i => wayIds.Contains(i.Attribute("id").Value))
            //        .ToList();

            IEnumerable<XElement> GetRelations()
            {
                using (var reader = XmlReader.Create(filename))
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
                                    yield return element;
                                }
                            }
                        }
                    }
                }
            }

            IEnumerable<XElement> GetWays(string[] ids)
            {
                using (var reader = XmlReader.Create(filename))
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
                                    if (ids.Contains(element.Attribute("id").Value))
                                    {
                                        yield return element;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            IEnumerable<XElement> GetNodes(string[] ids)
            {
                using (var reader = XmlReader.Create(filename))
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
                                    if (ids.Contains(element.Attribute("id").Value))
                                    {
                                        yield return element;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TestReader3()
        {
            var filename = "test.osm";
            filename = "budaihegyseg.osm";

            var outFilename = "out.osm";

            var relationJelList = new List<KeyValuePair<string, string>>();
            var relationWayList = new List<KeyValuePair<string, string>>();
            var wayNodeList = new List<KeyValuePair<string, string>>();

            using (var sr = new StreamReader(filename))
            {
                var isRelation = false;
                var relationId = string.Empty;

                var relationWayIds = new List<KeyValuePair<string, string>>();
                var relationHasJelTag = false;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (line.Trim().StartsWith("<relation"))
                    {
                        isRelation = true;
                        var attributes = GetAttributes(line);
                        relationId = attributes.FirstOrDefault(i => i.Key == "id").Value;
                    }
                    else if (line.Trim() == "</relation>")
                    {
                        if (relationHasJelTag)
                        {
                            relationWayList.AddRange(relationWayIds);
                        }

                        isRelation = false;
                        relationId = string.Empty;
                        relationWayIds = new List<KeyValuePair<string, string>>();
                        relationHasJelTag = false;
                    }

                    if (isRelation)
                    {
                        if (line.Trim().StartsWith("<tag") && line.Contains("k=\"jel\""))
                        {
                            var attributes = GetAttributes(line);
                            var jel = attributes.FirstOrDefault(i => i.Key == "v").Value;
                            relationJelList.Add(new KeyValuePair<string, string>(relationId, jel));

                            relationHasJelTag = true;
                        }
                        else if (line.Trim().StartsWith("<member"))
                        {
                            var attributes = GetAttributes(line);
                            var type = attributes.FirstOrDefault(i => i.Key == "type").Value;
                            if (type == "way")
                            {
                                var refAttributeValue = attributes.FirstOrDefault(i => i.Key == "ref").Value;
                                relationWayIds.Add(new KeyValuePair<string, string>(relationId, refAttributeValue));
                            }
                        }
                    }
                }
            }

            using (var sr = new StreamReader(filename))
            {
                var isWay = false;
                var wayId = string.Empty;
                var wayNodeIds = new List<KeyValuePair<string, string>>();
                var isWayPartOfRelation = false;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (line.Trim().StartsWith("<way"))
                    {
                        isWay = true;
                        var attributes = GetAttributes(line);
                        wayId = attributes.FirstOrDefault(i => i.Key == "id").Value;

                        if (relationWayList.Any(i => i.Value == wayId))
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
                            wayNodeList.AddRange(wayNodeIds);
                        }

                        isWay = false;
                        wayId = string.Empty;
                        wayNodeIds = new List<KeyValuePair<string, string>>();
                        isWayPartOfRelation = false;
                    }

                    if (isWay && isWayPartOfRelation)
                    {
                        if (line.Trim().StartsWith("<nd"))
                        {
                            var attributes = GetAttributes(line);
                            var refAttributeValue = attributes.FirstOrDefault(i => i.Key == "ref").Value;

                            wayNodeIds.Add(new KeyValuePair<string, string>(wayId, refAttributeValue));
                            // TODO: way-en van a tag?
                        }
                    }
                }
            }

            List<KeyValuePair<string, string>> GetAttributes(string line)
            {
                var result = new List<KeyValuePair<string, string>>();

                var attributes = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < attributes.Length; i++)
                {
                    var split = attributes[i].Replace("\"", "").Replace("/>", "").Split('=', StringSplitOptions.RemoveEmptyEntries);
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

        public void TestReader4()
        {
            var filename = "test.osm";
            filename = "budaihegyseg.osm";
            var outFilename = "out.osm";

            var relationJelList = new Dictionary<string, string>();
            var relationWayList = new HashSet<KeyValuePair<string, string>>();
            var wayNodeList = new HashSet<KeyValuePair<string, string>>();

            using (var reader = XmlReader.Create(filename))
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
                                    var relationId = element.Attribute("id").Value;

                                    relationJelList.Add(relationId, jelElement.Attribute("v").Value);

                                    var memberWays = element
                                        .Elements()
                                        .Where(i => i.Name == "member")
                                        .Where(i => i.Attribute("type").Value == "way");

                                    foreach (var item in memberWays)
                                    {
                                        var refAttribute = item.Attribute("ref").Value;
                                        relationWayList.Add(new KeyValuePair<string, string>(relationId, refAttribute));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var reader = XmlReader.Create(filename))
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
                                var id = element.Attribute("id").Value;
                                var relationWays = relationWayList.Where(i => i.Value == id);
                                if (relationWays.Any())
                                {
                                    var nodes = element
                                        .Elements()
                                        .Where(i => i.Name == "nd")
                                        .Select(i => new KeyValuePair<string, string>(id, i.Attribute("ref").Value));

                                    //wayNodeList.add(nodes);

                                    foreach (var item in nodes)
                                    {
                                        //var refAttribute = item.Attribute("ref").Value;
                                        //wayNodeList.Add(new KeyValuePair<string, string>(id, refAttribute));
                                        wayNodeList.Add(item);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //using (var sw = new StreamWriter(outFilename, false))
            //using (var reader = XmlReader.Create(filename))
            //{
            //    sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
            //    sw.WriteLine("<osm version=\"0.6\" generator=\"\">");

            //    reader.MoveToContent();
            //    while (reader.Read())
            //    {
            //        if (reader.NodeType == XmlNodeType.Element)
            //        {
            //            if (reader.Name == "way")
            //            {
            //            }
            //        }
            //    }

            //    sw.WriteLine("</osm>");
            //}
        }

        public void TestReader5()
        {
            var filename = "test.osm";
            filename = "budaihegyseg_srtm.osm";
            var outFilename = "budaihegyseg_srtm_hiking.osm";

            var relationTagList = new List<KeyValuePair<string, string>>();
            var relationWayList = new List<KeyValuePair<string, string>>();

            using (var reader = XmlReader.Create(filename))
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
                                    var relationId = element.Attribute("id").Value;

                                    relationTagList.Add(new KeyValuePair<string, string>(relationId, jelElement.ToString()));

                                    var memberWays = element
                                        .Elements()
                                        .Where(i => i.Name == "member")
                                        .Where(i => i.Attribute("type").Value == "way");

                                    foreach (var item in memberWays)
                                    {
                                        var refAttribute = item.Attribute("ref").Value;
                                        relationWayList.Add(new KeyValuePair<string, string>(relationId, refAttribute));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(outFilename, false))
            using (var sr = new StreamReader(filename))
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

                        if (relationWayList.Any(i => i.Value == wayId))
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
                            var relations = relationWayList.Where(i => i.Value == wayId);
                            foreach (var relation in relations)
                            {
                                var tags = relationTagList.Where(i => i.Key == relation.Key);
                                foreach (var tag in tags)
                                {
                                    sw.WriteLine($"\t\t{tag.Value}");
                                }
                            }
                        }

                        wayId = string.Empty;
                        isWayPartOfRelation = false;
                    }

                    sw.WriteLine(line);
                }
            }
        }


        List<KeyValuePair<string, string>> GetAttributes(string line)
        {
            var result = new List<KeyValuePair<string, string>>();

            var attributes = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < attributes.Length; i++)
            {
                var split = attributes[i].Replace("\"", "").Replace("/>", "").Split('=', StringSplitOptions.RemoveEmptyEntries);
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

