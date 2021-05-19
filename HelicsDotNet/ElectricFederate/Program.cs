using System;
using gmlc;
using h = gmlc.helics;
using s = SAInt_API.SAInt;
using SAInt_API;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using SAIntHelicsLib;

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

            List<Mapping> MappingList = MappingFactory.GetMappingFromFile(netfolder + "Mapping.txt");
     
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
                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(netfolder + m.ElectricGen.Name+".txt", FileMode.Create));               
            }

            //Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");
   
            // start simulation at t = 1 s, run to t = 2 s
            double total_time = 1 ; 
            double granted_time = 0 ;
            double requested_time;

            // set max iteration
            h.helicsFederateSetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations);
            Console.WriteLine($"Max iterations: {iter_max}");

            // start execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Electric: Entering execution mode");

            // run initial power model at t=0, publish starting value
            APIExport.runESIM();

            //MappingFactory.PublishRequiredThermalPower(granted_time, MappingList);

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

                    // Get offtake limits from gas federate if past iteration zero
                    if (current_iter > 0)
                    {
                        foreach (Mapping m in MappingList)
                        {
                            double val = h.helicsInputGetDouble(m.GasSub);
                            Console.WriteLine($"Electric: Received value = {val} at time {granted_time} from node {m.GasNodeID} Gas federate for available thermal power in [MW]");

                            //get currently required thermal power 
                            double pval = APIExport.evalFloat(String.Format("{0}.PG.[MW]", m.ElectricGenID));
                            double HR = m.ElectricGen.K_0 + m.ElectricGen.K_1 * pval + m.ElectricGen.K_2 * pval * pval;
                            double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                            if ( ThermalPower>val)
                            {
                                m.ElectricGen.PGMAX *= .8;
                            }
                        }
                    }

                    // run the electric simulation for the current granted tim
                    APIExport.runESIM();

                    // publish new values
                    MappingFactory.PublishRequiredThermalPower(current_iter, MappingList);

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

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Electric: Federate finalized");
            h.helicsFederateFree(vfed);
            
            while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);

            foreach (Mapping m in MappingList)
            {
                if (m.sw != null)
                {
                    m.sw.Flush();
                    m.sw.Close();
                }
            }


            // disconnect broker
            h.helicsCloseLibrary();
            Console.WriteLine("GasElectric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}
