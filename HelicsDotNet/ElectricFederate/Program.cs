using System;
using gmlc;
using h = gmlc.helics;
using s = SAInt_API.SAInt;
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

            // Set core type from string
            Console.WriteLine("Electric: Setting Federate Info Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Electric Federate Core");

            //Set core type from string #
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            //Federate init string
            Console.WriteLine("Electric: Setting Federate Info Init String");
            string fedinitstring = "--broker=mainbroker --federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            //Create value federate
            Console.WriteLine("Electric: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo);
            Console.WriteLine("Electric: Value federate created");

            //Register the publication 
            var pubElectricOutPut = h.helicsFederateRegisterGlobalTypePublication(vfed, "ElectricOutPut", "double", "");
            Console.WriteLine("Electric: Publication registered");

            //Subscribe to Gas Federate publication
            var sub = h.helicsFederateRegisterSubscription(vfed, "GasOutPut", "");
            Console.WriteLine("Electric: Subscription registered");

            //Set one second message interval
            double period = 0.5;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");
   
            // start simulation at t = 1 s, run to t = 5 s
            double total_time = 5 ; 
            double granted_time = 0 ;
            double requested_time; 

            // start execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Electric: Entering execution mode");

            // run initial power model at t=0, publish starting value
            APIExport.runESIM();
            string StrNoLP = $"BUS.{CoupledElectricNode}.PG.[MW]";
            float p = APIExport.evalFloat(StrNoLP);
            h.helicsPublicationPublishDouble(pubElectricOutPut, p);

            // iterate over intervals
            for (int n = 1; n <= total_time; n++)
            {
                requested_time = n;

                // keep requesting time until you reach the next relevant point to simulate
                while (granted_time < requested_time)
                {
                    Console.WriteLine($"Requested time: {requested_time}");
                    granted_time = h.helicsFederateRequestTime(vfed, requested_time);
                    Console.WriteLine($"Granted time: {granted_time}");
                }

                // run the electric simulation for the current granted time
                APIExport.runESIM();
                StrNoLP = $"BUS.{CoupledElectricNode}.PG.[MW]";
                p = APIExport.evalFloat(StrNoLP);
                h.helicsPublicationPublishDouble(pubElectricOutPut, p);

                Console.WriteLine($"Electric: Sending value for active power in [MW] = {p} at time {granted_time} to Gas federate");
                double value = h.helicsInputGetDouble(sub);

                Console.WriteLine($"Electric: Received value = {value} at time {granted_time} from Gas federate for pressue in [bar-g]");

                // curtail gas generator dispatch if pressure is below delivery pressure
                if (value<= 30.0)
                {
                    foreach (var gen in s.ENET.Gen)
                    {
                        if (gen.Name.ToUpper() == CoupledElectricNode.ToUpper())
                        {
                            gen.PGMAX *= .95; 
                        }
                    }

                    //foreach (var evt in s.ENET.SCE.SceList)
                    //{
                    //    if (evt.ObjName.ToUpper() == CoupledElectricNode.ToUpper() && evt.ObjPar == SAInt_API.Network.CtrlType.PGSET)
                    //    {
                    //        evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                    //        evt.ShowVal = string.Format("{0}", p * 0.95);
                    //    }
                    //}
                }

                Thread.Sleep(3);
            }

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            Console.WriteLine($"Requested time: {requested_time}");
            h.helicsFederateRequestTime(vfed, requested_time);

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Electric: Federate finalized");
            h.helicsFederateFree(vfed);
            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);

            // disconnect broker
            h.helicsCloseLibrary();
            Console.WriteLine("GasElectric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}
