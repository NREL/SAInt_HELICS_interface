using System;
using gmlc;
using h = gmlc.helics;
using SAInt_API;
using System.Threading;

namespace HelicsDotNetReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            string netfolder = @"..\..\..\..\Demo\";
            // Load Gas Model
            APIExport.openGNET(netfolder + "GNET25.net");
            APIExport.openGSCE(netfolder + "CMBSTEOPF.sce");
            APIExport.showSIMLOG(false);

            string CoupledGasNode = "N15";
            string CoupledElectricNode = "BUS001";


            Console.WriteLine($"Gas: Helics version ={helics.helicsGetVersion()}");

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Gas: Creating Federate Info");
            var fedinfo = helics.helicsCreateFederateInfo();

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Gas: Setting Federate Info Name");
            h.helicsFederateInfoSetCoreName(fedinfo, "Gas Federate Core");

            // Set core type from string
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            //Federate init string
            Console.WriteLine("Gas: Setting Federate Info Init String");
            string fedinitstring = "--federates=1";

            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            //Create value federate
            Console.WriteLine("Gas: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Gas Federate", fedinfo);
            Console.WriteLine("Gas: Value federate created");

            //Register the publication #
            var pubGasOutPut = h.helicsFederateRegisterGlobalTypePublication(vfed, "GasOutPut", "double", "");
            Console.WriteLine("Gas: Publication registered");

            //Subscribe to Electric publication
            var sub = h.helicsFederateRegisterSubscription(vfed, "ElectricOutPut", "");
            Console.WriteLine("Gas: Subscription registered");

            //Set one second message interval
            double period = 0.5;
            Console.WriteLine("Gas: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");

            // has gas simulation on 0.5 s delay relative to power (t = 1.5, 2.5, etc. )
            double total_time = 5;
            double offset = 0.5;
            double granted_time = 0;
            double requested_time;

            // enter execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            // run initial gas model at t=0, publish starting value
            APIExport.runGSIM();
            string StrNoLP = String.Format("NO.{0}.P.[bar-g]", CoupledGasNode);
            double p = APIExport.evalFloat(StrNoLP);
            h.helicsPublicationPublishDouble(pubGasOutPut, p);

  
            // iterate over intervals
            for (int n = 1; n <= total_time; n++)
            {
                requested_time = n + offset;

                // keep requesting time until you reach the next relevant point to simulate
                while (granted_time < requested_time)
                {
                    Console.WriteLine($"Requested time: {requested_time}");
                    granted_time = h.helicsFederateRequestTime(vfed, requested_time);
                    Console.WriteLine($"Granted time: {granted_time}");
                }

                // get value from previous electric simulation
                double value = h.helicsInputGetDouble(sub);
                APIExport.eval(string.Format("GSYS.SCE.SceList[6].ShowVal='{0}'", value / 20));
                Console.WriteLine("Gas: Received value = {0} at time {1} from Electric federate for active power in [MW]", value, granted_time);

                // run the gas simulation for the current granted time
                APIExport.runGSIM();
                StrNoLP = String.Format("NO.{0}.P.[bar-g]", CoupledGasNode);
                p = APIExport.evalFloat(StrNoLP);
                h.helicsPublicationPublishDouble(pubGasOutPut, p);
                Console.WriteLine(String.Format("Gas: Sending value for pressure in [bar-g] = {0} at time {1} to Electric federate", p, granted_time));
                Thread.Sleep(3);
            }

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            Console.WriteLine($"Requested time: {requested_time}");
            h.helicsFederateRequestTime(vfed, requested_time);

            // finalize gas federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();      
            var k = Console.ReadKey();
        }
    }
}
