using System;
using System.Collections.Generic;
using System.Text;

namespace Transform.Model
{
    public class WayNode
    {
        public string Id { get; set; }

        public int Index { get; set; } // Order of nodes of the way

        public double Lat { get; set; }

        public double Lon { get; set; }

        public double DistanceFromPreviousNode { get; set; }

        public double CumulativeDistance { get; set; }
    }
}
