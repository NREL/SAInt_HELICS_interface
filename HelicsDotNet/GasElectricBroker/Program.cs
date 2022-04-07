using System;
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
            Console.WriteLine($"GasElectricBroker: Helics version ={helics.helicsGetVersion()}");

            //Create broker #
            Console.WriteLine("Creating Broker");
            var broker = h.helicsCreateBroker("tcp", "", initBrokerString);
            Console.WriteLine("Created Broker");

            Console.WriteLine("Checking if Broker is connected");
            int isconnected = h.helicsBrokerIsConnected(broker);
            Console.WriteLine("Checked if Broker is connected");

            if (isconnected == 1) Console.WriteLine("Broker created and connected");

            // Call Federates in separate processes
            Process.Start(@"C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\ElectricFederate\bin\x64\Debug\ElectricFederate.exe");
            Process.Start(@"C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\GasFederate\bin\x64\Debug\GasFederate.exe");

            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);
            h.helicsCloseLibrary();
            Console.WriteLine("GasElectric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}
