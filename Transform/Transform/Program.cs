using System;
using System.Diagnostics;

namespace Transform
{
    class Program
    {
        static void Main(string[] args)
        {
            var timer = new Stopwatch();
            timer.Start();

            var service = new Service();
            //service.TestReader();
            //service.TestReader2();
            //service.TestReader3();
            //service.TestReader4();
            service.TestReader5();

            timer.Stop();
            Console.WriteLine($"Ellapsed: {timer.Elapsed.ToString("hh\\:mm\\:ss")}");
        }
    }
}
