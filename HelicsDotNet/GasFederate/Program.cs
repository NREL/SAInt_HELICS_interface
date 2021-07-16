using System;
using gmlc;
using h = gmlc.helics;
using SAInt_API;
using System.IO;
using System.Collections.Generic;
using SAIntHelicsLib;
using SAInt_API.Library;

namespace HelicsDotNetReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            string netfolder = @"..\..\..\..\Demo\";
            // Load Gas Model
            APIExport.openGNET(netfolder + "GNET25.net");
            APIExport.openGSCE(netfolder + "CASE1.sce");
            APIExport.openGCON(netfolder + "CMBSTEOPF.con");
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
                m.sw.WriteLine("tstep \t iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
            }

            //Set one second message interval
            double period = 1;
            Console.WriteLine("Gas: Setting Federate Timing");
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

            // enter execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            Int16 step=0;
            bool IsRepeating = false;
            bool HasViolations = false;

            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {
                if (e.TimeStep > 0)
                {
                    if (e.SolverState == SolverState.AfterTimeStep && !IsRepeating)
                    {
                        // non-iterative time request here to block until both federates are done iterating
                        Console.WriteLine($"Requested time {e.TimeStep}");
                        h.helicsFederateRequestTime(vfed, e.TimeStep);
                        step = 0;
                        Console.WriteLine($"TimeStep: {e.TimeStep} SolverState: {e.SolverState}");
                        IsRepeating = !IsRepeating;
                        HasViolations = true;
                    }

                    if (e.SolverState == SolverState.AfterTimeStep && IsRepeating)
                    {
                        IsRepeating = (step < iter_max);

                        if (IsRepeating)
                        {
                            step += 1;
                            int helics_iter_status;

                            Console.WriteLine($"Requested time: {e.TimeStep}, iteration: {step}");
                            granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, helics_iteration_request.helics_iteration_request_force_iteration, out helics_iter_status);

                            Console.WriteLine($"Granted time: {granted_time},  Iteration status: {helics_iter_status}");
                            MappingFactory.PublishAvailableThermalPower(granted_time, step, MappingList);

                            if (!(e.TimeStep == 1 && step == 1))
                            {
                                HasViolations = MappingFactory.SubscribeToRequiredThermalPower(granted_time,step, MappingList);
                            }

                            if (step > 1)
                            {
                                e.RepeatTimeIntegration = HasViolations;
                                IsRepeating = HasViolations;

                                //if (!IsRepeating)
                                //{
                                //    h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, helics_iteration_request.helics_iteration_request_no_iteration, out helics_iter_status);
                                //}
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

            // run gas model
            APIExport.runGSIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            Console.WriteLine($"Requested time: {requested_time}");
            h.helicsFederateRequestTime(vfed, requested_time);

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);

            foreach (Mapping m in MappingList)
            {
                if (m.sw != null)
                {
                    m.sw.Flush();
                    m.sw.Close();
                }
            }
      
            var k = Console.ReadKey();
        }        
    }
}