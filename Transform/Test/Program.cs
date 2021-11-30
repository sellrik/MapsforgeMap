using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new Test();

            double lat1, lat2, lon1, lon2;

            lat1 = 47.54462302796;
            lon1 = 18.98349786426;

            lat2 = 47.54465076718;
            lon2 = 18.96775879302;

            var distance = test.CalculateDistance(lat1, lon1, lat2, lon2);
            test.CalculateIntermediatePoint(lat1, lon1, lat2, lon2);
        }
    }
}
