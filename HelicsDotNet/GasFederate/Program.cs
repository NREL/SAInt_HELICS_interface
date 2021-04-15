using System;
using gmlc;
using h = gmlc.helics;
using s = SAInt_API.SAInt;
using SAInt_API;
using SAInt_API.Network.Electric;
using SAInt_API.Network.Gas;
using System.Threading;
using System.IO;
using System.Collections.Generic;

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

            //string CoupledGasNode = "N15";
            //string CoupledElectricNode = "BUS001";

            SetMappingFile(netfolder + "Mapping.txt");


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

            // Register Publication and Subscription for coupling points
            foreach (Mapping m in MappingList) {
                m.GasPub= h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.GasNodeID, "double", "");
                m.ElectricSub= h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.ElectricGenID, "");
            }

            //Set one second message interval
            double period = 1;
            Console.WriteLine("Gas: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");

            // start simulation at t = 1 s, run to t = 5 s
            double total_time = 3;
            double granted_time = 0;
            double requested_time;

            // set max iteration
            h.helicsFederateSetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations, 4);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations);
            Console.WriteLine($"Max iterations: {iter_max}");

            // enter execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            // run initial gas model at t=0, publish starting value
            APIExport.runGSIM();

            Action<double> publish = (double gtime) =>
            {
                foreach (Mapping m in MappingList)
                {
                    double pval = APIExport.evalFloat(String.Format("{0}.P.[bar-g]", m.GasNodeID));
                    h.helicsPublicationPublishDouble(m.GasPub, pval);
                    Console.WriteLine(String.Format("Gas: Sending value for pressure at node {2} in [bar-g] = {0} at time {1} to Electric federate", pval, gtime, m.GasNode));
                }
            };

            publish.Invoke(granted_time);

            // iterate over intervals
            for (int n = 1; n <= total_time; n++)
            {
                requested_time = n;

                // non-iterative time request here to block until both federates are done iterating
                Console.WriteLine($"Requested time {requested_time}");
                h.helicsFederateRequestTime(vfed, requested_time);

                // iteration setttings
                int current_iter = 0;
                int helics_iter_status;
                bool iter_state = true;

                // keep requesting time while iterating
                while (iter_state)
                {
                    Console.WriteLine($"Requested time: {requested_time}, iteration: {current_iter}");
                    granted_time = h.helicsFederateRequestTimeIterative(vfed, requested_time, helics_iteration_request.helics_iteration_request_force_iteration, out helics_iter_status);
                    Console.WriteLine($"Granted time: {granted_time},  Iteration status: {helics_iter_status}");

                    // if past iteration 0, get the results from the last electric federate iteration before running
                    // need to check what the default values will be for iteration zero 
                    if (current_iter > 0)
                    {
                        foreach (Mapping m in MappingList)
                        {
                            // get publication from electric federate
                            double val = h.helicsInputGetDouble(m.ElectricSub);
                            Console.WriteLine("Gas: Received value = {0} at time {1} from Electric federate for active power at Generator {2} in [MW]", val, granted_time, m.ElectricGenID);

                            // calculate offtakes at corresponding using heat rates
                            foreach (var s in m.GasNode.EventList)
                            {
                                if (s.ObjPar == SAInt_API.Network.CtrlType.QSET)
                                {
                                    s.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.ksm3_h);
                                    s.ShowVal = string.Format("{0}", val); // here we could enter the conversion from electric power to gas offtake using heatrate and calrofic value
                                }
                            }
                        }
                    }
 
                    // run the gas simulation for the current granted time
                    APIExport.runGSIM();

                    // publish new gas offtake values
                    publish.Invoke(granted_time);

                    // check convergence criteria (to add function here)
                    bool converged = false; 
                    
                    // determine if iteration should stop
                    if (current_iter > iter_max ^ converged)
                    {
                        Console.WriteLine("Finished iterating");
                        iter_state = false;
                        // one last call to HELICS to end iteration at this time step
                        h.helicsFederateRequestTimeIterative(vfed, requested_time, helics_iteration_request.helics_iteration_request_no_iteration, out helics_iter_status);
                    }
                    else
                    {
                        // otherwise advance to next iteration
                        current_iter++;
                    }

                    Thread.Sleep(3);
                }
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

        public static List<Mapping> MappingList = new List<Mapping>();

        public partial class Mapping
        {
            public string ElectricGenID;
            public string GasNodeID;
            public GasNode  GasNode;
            public SWIGTYPE_p_void GasPub;
            public SWIGTYPE_p_void ElectricSub;
        }

        static void SetMappingFile(string filename)
        {
            if (File.Exists(filename) )
            {
                MappingList.Clear();
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        var zeile = new string[0];
                        while (sr.Peek() != -1)
                        {
                            zeile = sr.ReadLine().Split(new[] {(char) 9}, StringSplitOptions.RemoveEmptyEntries);
                            
                            if (zeile.Length > 1)
                            {
                                if (!zeile[0].Contains("%"))
                                {
                                    var mapitem = new Mapping();
                                    mapitem.ElectricGenID = zeile[0];
                                    mapitem.GasNodeID = zeile[1];
                                    //mapitem.ElectricGen = SAInt.ENET[mapitem.ElectricGenID] as eGen;
                                    mapitem.GasNode = SAInt.GNET[mapitem.GasNodeID] as GasNode;
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
