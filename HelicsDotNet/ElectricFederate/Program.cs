using System;
using gmlc;
using h = gmlc.helics;
using s = SAInt_API.SAInt;
using SAInt_API;
using SAInt_API.Network.Electric;
using SAInt_API.Network.Gas;
using System.Threading;
using System.Collections.Generic;
using System.IO;

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

            SetMappingFile(netfolder + "Mapping.txt");

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

            // Register Publication and Subscription for coupling points
            foreach (Mapping m in MappingList)
            {
                m.ElectricPub = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.ElectricGenID, "double", "");
                m.GasSub = h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.GasNodeID, "");
            }

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

            Action<double> publish = (double gtime) =>
            {
                foreach (Mapping m in MappingList)
                {
                    double pval = APIExport.evalFloat(String.Format("{0}.PG.[MW]", m.ElectricGenID));
                    h.helicsPublicationPublishDouble(m.ElectricPub, pval);
                    Console.WriteLine(String.Format("Electric: Sending value for active power at Generator {2} in [MW] = {0} at time {1} to Gas federate", pval, gtime, m.ElectricGen));
                }
            };

            publish.Invoke(granted_time);

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

                publish.Invoke(granted_time);

                foreach (Mapping m in MappingList)
                {
                    double val = h.helicsInputGetDouble(m.GasSub);
                    Console.WriteLine($"Electric: Received value = {val} at time {granted_time} from node {m.GasNodeID} Gas federate for pressue in [bar-g]");
                    // curtail gas generator dispatch if pressure is below delivery pressure
                    if (val <= m.PMIN)
                    {
                        m.ElectricGen.PGMAX *= .95;
                        Console.WriteLine($"Electric: Pressure at node {m.GasNodeID} = {val} [bar-g] is below minimum delivery pressure {m.PMIN} [bar-g], therefore, reducing max active power generation for Generator {m.ElectricGen} to {m.ElectricGen.PGMAX} [MW] at time {granted_time}");
                    }
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

        public static List<Mapping> MappingList = new List<Mapping>();

        public partial class Mapping
        {
            public string ElectricGenID;
            public string GasNodeID;
            public eGen ElectricGen;
            public SWIGTYPE_p_void ElectricPub;
            public SWIGTYPE_p_void GasSub;
            public double PMIN;
        }

        static void SetMappingFile(string filename)
        {
            if (File.Exists(filename))
            {
                MappingList.Clear();
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        var zeile = new string[0];
                        while (sr.Peek() != -1)
                        {
                            zeile = sr.ReadLine().Split(new[] { (char)9 }, StringSplitOptions.RemoveEmptyEntries);

                            if (zeile.Length > 1)
                            {
                                if (!zeile[0].Contains("%"))
                                {
                                    var mapitem = new Mapping();
                                    mapitem.ElectricGenID = zeile[0];
                                    mapitem.GasNodeID = zeile[1];
                                    mapitem.PMIN= Convert.ToDouble(zeile[2]);
                                    mapitem.ElectricGen = SAInt.ENET[mapitem.ElectricGenID] as eGen;
                                    MappingList.Add(mapitem);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception(string.Format("File {0} does not exist!", filename));
            }
        }
    }
}
