using System;
using h = helics;
using System.Threading;

namespace GasElectricBroker
{
    class Broker
    {
        static void Main(string[] args)
        {
            string initBrokerString = "-f 2 --name=mainbroker";
            Console.WriteLine($"GasElectricBroker: Helics version ={h.helicsGetVersion()}");

            //Create broker #
            Console.WriteLine("Creating Broker");
            var broker = h.helicsCreateBroker("tcp", "", initBrokerString);
            Console.WriteLine("Created Broker");

            Console.WriteLine("Checking if Broker is connected");
            int isconnected = h.helicsBrokerIsConnected(broker);
            Console.WriteLine("Checked if Broker is connected");

            if (isconnected == 1) Console.WriteLine("Broker: Broker is created and connected");

            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);            
            Console.WriteLine("Broker: Broker is disconnected");

            _ = Console.ReadKey();
        }
    }
}
