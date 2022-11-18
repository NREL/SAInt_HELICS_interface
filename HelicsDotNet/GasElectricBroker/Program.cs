﻿using System;
using h = helics;
using System.Threading;
using System.Diagnostics;

namespace GasElectricBroker
{
    class Program
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

            //Run Electric Federate
            //Process.Start(@"..\..\..\..\ElectricFederate\bin\x64\Debug\ElectricFederate.exe");

            //Run Electric Federate
            //Process.Start(@"..\..\..\..\GasFederate\bin\x64\Debug\GasFederate.exe");

            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);            
            Console.WriteLine("Broker: Broker is disconnected");

            _ = Console.ReadKey();
        }
    }
}
