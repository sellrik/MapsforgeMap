using System;
using System.Collections.Generic;
using System.Text;

namespace Test
{
    public class Test
    {
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // http://www.movable-type.co.uk/scripts/latlong.html

            var R = 6371000;

            var φ1 = ConvertToRadians(lat1);

            var φ2 = ConvertToRadians(lat2);
            var Δφ = ConvertToRadians(lat2 - lat1);
            var Δλ = ConvertToRadians(lon2 - lon1);

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var d = R * c; // in metres
            return d;
        }

        public void CalculateIntermediatePoint(double lat1, double lon1, double lat2, double lon2)
        {
            var fraction = 0.5;
            var φ1 = ConvertToRadians(lat1);
            var λ1 = ConvertToRadians(lon1);
            var φ2 = ConvertToRadians(lat2);
            var λ2 = ConvertToRadians(lon2);

            // distance between points
            //var Δφ = φ2 - φ1;
            //var Δλ = λ2 - λ1;
            //var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
            //    + Math.Cos(φ1) * Math.Cos(φ2) * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            //var δ = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var δ = CalculateDistance(lat1, lon1, lat2, lon2);

            var A = Math.Sin((1 - fraction) * δ) / Math.Sin(δ);
            var B = Math.Sin(fraction * δ) / Math.Sin(δ);

            var x = A * Math.Cos(φ1) * Math.Cos(λ1) + B * Math.Cos(φ2) * Math.Cos(λ2);
            var y = A * Math.Cos(φ1) * Math.Sin(λ1) + B * Math.Cos(φ2) * Math.Sin(λ2);
            var z = A * Math.Sin(φ1) + B * Math.Sin(φ2);

            var φ3 = Math.Atan2(z, Math.Sqrt(x * x + y * y));
            var λ3 = Math.Atan2(y, x);

            var lat = ConvertToDegrees(φ3);
            var lon = ConvertToDegrees(λ3);
        }

        double ConvertToRadians(double value)
        {
            return value * Math.PI / 180;
        }

        double ConvertToDegrees(double value)
        {
            return value / Math.PI * 180;
        }
    }
}
