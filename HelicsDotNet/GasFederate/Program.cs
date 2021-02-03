using System;
using gmlc;
using h = gmlc.helics;
using SAInt_API;

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

            string fedinitstring = "--federates=1";
            double deltat = 0.01;

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
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            /* Set the message interval (timedelta) for federate. Note that
             HELICS minimum message time interval is 1 ns and by default
             it uses a time delta of 1 second. What is provided to the
             setTimedelta routine is a multiplier for the default timedelta.*/

            //Set one second message interval
            Console.WriteLine("Gas: Setting Federate Info Time Delta");
            h.helicsFederateInfoSetTimeProperty(fedinfo, (int)helics_properties.helics_property_time_delta, deltat);

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

            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            double value = 0.0;
            double currenttime = -1;

            while (currenttime <= 100)
            {
                currenttime = h.helicsFederateRequestTime(vfed, 100);
                value = h.helicsInputGetDouble(sub);
                APIExport.eval(string.Format("GSYS.SCE.SceList[6].ShowVal='{0}'", value / 20));
                APIExport.runGSIM();
                Console.WriteLine("Gas: Received value = {0} at time {1} from Electric federate for active power in [MW]", value, currenttime);
                string StrNoLP = String.Format("NO.{0}.P.[bar-g]", CoupledGasNode);
                double p = APIExport.evalFloat(StrNoLP);
                h.helicsPublicationPublishDouble(pubGasOutPut, p);
                Console.WriteLine(String.Format("Gas: Sending value for pressure in [bar-g] = {0} at time {1} to Electric federate", p, currenttime));
            }

            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();      
            var k = Console.ReadKey();
        }
    }
}
