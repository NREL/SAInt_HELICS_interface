using System;
using gmlc;
using h = gmlc.helics;
using SAInt_API;
using System.Threading;

namespace HelicsDotNetSender
{
    class Program
    {
        static void Main(string[] args)
        {

            string netfolder = @"..\..\..\..\Demo\";
            //Load Electric Model
            APIExport.openENET(netfolder + "ENET30.enet");
            APIExport.openESCE(netfolder + "CMBSTEOPF.esce");
            APIExport.showSIMLOG(false);

            string CoupledGasNode = "N15";
            string CoupledElectricNode = "BUS001";
        
            string fedinitstring = "--broker=mainbroker --federates=1";
            double deltat = 0.01;
            Console.WriteLine($"Electric: Helics version ={helics.helicsGetVersion()}");

            //Create broker #
            string initBrokerString = "-f 2 --name=mainbroker";
            Console.WriteLine("Creating Broker");
            var broker = h.helicsCreateBroker("tcp", "", initBrokerString);
            Console.WriteLine("Created Broker");

            Console.WriteLine("Checking if Broker is connected");
            int isconnected = h.helicsBrokerIsConnected(broker);
            Console.WriteLine("Checked if Broker is connected");

            if (isconnected == 1) Console.WriteLine("Broker created and connected");

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Electric: Creating Federate Info");
            var fedinfo = helics.helicsCreateFederateInfo();

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Electric: Setting Federate Info Name");
            h.helicsFederateInfoSetCoreName(fedinfo, "Gas Federate Core");

            // Set core type from string
            Console.WriteLine("Electric: Setting Federate Info Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Electric Federate Core");

            //Set core type from string #
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            //Federate init string
            Console.WriteLine("Electric: Setting Federate Info Init String");
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            /* Set the message interval (timedelta) for federate. Note that
             HELICS minimum message time interval is 1 ns and by default
             it uses a time delta of 1 second. What is provided to the
             setTimedelta routine is a multiplier for the default timedelta.*/

            //Set one second message interval
            Console.WriteLine("Electric: Setting Federate Info Time Delta");
            h.helicsFederateInfoSetTimeProperty(fedinfo, (int)helics_properties.helics_property_time_delta, deltat);

            //Create value federate
            Console.WriteLine("Electric: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo);
            Console.WriteLine("Electric: Value federate created");

            //Register the publication #
            var pubElectricOutPut = h.helicsFederateRegisterGlobalTypePublication(vfed, "ElectricOutPut", "double", "");
            Console.WriteLine("Electric: Publication registered");

            //Subscribe to Electric publication
            var sub = h.helicsFederateRegisterSubscription(vfed, "GasOutPut", "");
            Console.WriteLine("Electric: Subscription registered");

            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Electric: Entering execution mode");

            for ( int c = 5; c <= 50; c++)
            {
                double currenttime = h.helicsFederateRequestTime(vfed, c);
                APIExport.runESIM();
                string StrNoLP = $"BUS.{CoupledElectricNode}.PG.[MW]";
                float p = APIExport.evalFloat(StrNoLP);
                h.helicsPublicationPublishDouble(pubElectricOutPut, p);
                Console.WriteLine($"Electric: Sending value for active power in [MW] = {p} at time {currenttime} to Gas federate");
                double value = h.helicsInputGetDouble(sub);
                Console.WriteLine($"Electric: Received value = {value} at time {currenttime} from Gas federate for pressue in [bar-g]");
                Thread.Sleep(1);
            }
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Electric: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();
            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);
            Console.WriteLine("GasElectric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}
