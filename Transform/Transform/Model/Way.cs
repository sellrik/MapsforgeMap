using System;
using System.Collections.Generic;
using System.Text;

namespace Transform.Model
{
    public class Way
    {
        public string Id { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        public List<WayNode> Nodes { get; set; } = new List<WayNode>();

        public double Distance { get; set; }
    }
}
