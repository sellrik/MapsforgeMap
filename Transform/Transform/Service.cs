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

