using System;
using System.Collections.Generic;
using System.Text;

namespace Transform.Model
{
    public class Node
    {
        public string Id { get; set; }

        public double Lat { get; set; }

        public double Lon { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }
}
