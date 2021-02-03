using System;
using gmlc;
using h = gmlc.helics;
using System.Threading;

namespace GasElectricBroker
{
    class Program
    {
        static void Main(string[] args)
        {
            string initBrokerString = "-f 2 --name=mainbroker";
            Console.WriteLine($"GasElectricBroker: Helics version ={helics.helicsGetVersion()}");

            //Create broker #
            Console.WriteLine("Creating Broker");
            var broker = h.helicsCreateBroker("tcp", "", initBrokerString);
            Console.WriteLine("Created Broker");

            Console.WriteLine("Checking if Broker is connected");
            int isconnected = h.helicsBrokerIsConnected(broker);
            Console.WriteLine("Checked if Broker is connected");

            if (isconnected == 1) Console.WriteLine("Broker created and connected");

            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);
            h.helicsCloseLibrary();
            Console.WriteLine("GasElectric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}
