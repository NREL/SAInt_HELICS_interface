using System;
using gmlc;
using h = gmlc.helics;
using SAInt_API;
using SAInt_API.Library;
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
            APIExport.openESCE(netfolder + "CASE1.esce");
            APIExport.openECON(netfolder + "CMBSTEOPF.econ");
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
                m.sw.WriteLine("tstep \t iter \t PG[MW] \t ThPow [MW] \t PGMAX [MW]");
            }

            //Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)helics_properties.helics_property_time_period, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)helics_properties.helics_property_time_period);
            Console.WriteLine($"Time period: {period_set}");


            // start simulation at t = 1 s, run to t = 2 s
            double total_time = 96;
            double granted_time = 0;
            double requested_time;

            // set max iteration
            h.helicsFederateSetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)helics_properties.helics_property_int_max_iterations);
            Console.WriteLine($"Max iterations: {iter_max}");

            // start execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Electric: Entering execution mode");

            Int16 step=0 ;
            bool IsRepeating = false;
            bool HasViolations = false;

            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {
                if (e.TimeStep >= 0)
                {

                    if (e.SolverState == SolverState.BeforeTimeStep)
                    {
                        // non-iterative time request here to block until both federates are done iterating
                        Console.WriteLine($"Requested time {e.TimeStep}");
                        granted_time = h.helicsFederateRequestTime(vfed, e.TimeStep);
                        step = 0;
                        Console.WriteLine($"Granted time: {granted_time}, SolverState: {e.SolverState}");
                        IsRepeating = !IsRepeating;
                        HasViolations = true;
                        // Reset Name plate capacity
                        foreach (Mapping m in MappingList)
                        {
                            m.ElectricGen.PGMAX = m.NCAP;
                            m.ElectricGen.PGMIN = 0;
                        }
                    }

                    if ( e.SolverState == SolverState.AfterTimeStep && IsRepeating)
                    {
                        IsRepeating =  (step < iter_max); //step==0 || !MappingFactory.StepSolved(.0001,e.TimeStep,MappingList) ||

                        if (IsRepeating)
                        {
                            step += 1;
                            int helics_iter_status;

                            Console.WriteLine($"Requested time: {e.TimeStep}, iteration: {step}");
                            granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, helics_iteration_request.helics_iteration_request_force_iteration, out helics_iter_status);

                            Console.WriteLine($"Granted time: {granted_time},  Iteration status: {helics_iter_status}");
                            MappingFactory.PublishRequiredThermalPower(granted_time-1, step, MappingList);

                            if (!(e.TimeStep == 0 && step == 1))
                            {
                                HasViolations = MappingFactory.SubscribeToAvailableThermalPower(granted_time-1, step, MappingList);
                            }

                            if (step > 1)
                            {
                                e.RepeatTimeIntegration = HasViolations;
                                IsRepeating = HasViolations;
                            }
                            else
                            {
                                e.RepeatTimeIntegration = true;
                                IsRepeating = true;
                            }
                           
                        }
                    }
                }
            };

            // run power model
            APIExport.runESIM();

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
            Console.WriteLine("Electric: Broker disconnected");
            var k = Console.ReadKey();
        }
    }
}