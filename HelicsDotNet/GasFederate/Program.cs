using System;
using gmlc;
using h = gmlc.helics;
using s = SAInt_API.SAInt;
using SAInt_API;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using SAIntHelicsLib;

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

            List<Mapping> MappingList = MappingFactory.GetMappingFromFile(netfolder + "Mapping.txt");

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
                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(netfolder + m.GasNode.Name + ".txt", FileMode.Create));
            }

            //Set one second message interval
            double period = 1;
            Console.WriteLine("Gas: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");

            // start simulation at t = 1 s, run to t = 5 s
            double total_time = 1;
            double granted_time = 0;
            double requested_time;

            // set max iteration
            h.helicsFederateSetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations);
            Console.WriteLine($"Max iterations: {iter_max}");

            // enter execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            // run initial gas model at t=0, publish starting value
            APIExport.runGSIM();

            //MappingFactory.PublishAvailableThermalPower(granted_time, MappingList);

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
                            Console.WriteLine("Gas: Received value = {0} at time {1} from Electric federate for required thermal power at Generator {2} in [MW]", val, granted_time, m.ElectricGenID);

                            // calculate offtakes at corresponding using heat rates
                            foreach (var evt in m.GasNode.EventList)
                            {
                                if (evt.ObjPar == SAInt_API.Network.CtrlType.QSET)
                                {
                                    evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                                    evt.ShowVal = string.Format("{0}", 1E6 * val/s.GNET.CV); // converting thermal power to flow rate using calorific value
                                }
                            }
                        }
                    }
 
                    // run the gas simulation for the current granted time
                    APIExport.runGSIM();

                    // publish new gas offtake values
                    MappingFactory.PublishAvailableThermalPower(current_iter, MappingList);

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

            foreach (Mapping m in MappingList)
            {
                if (m.sw != null)
                {
                    m.sw.Flush();
                    m.sw.Close();
                }
            }

            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();      
            var k = Console.ReadKey();
        }        
    }
}
