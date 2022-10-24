using System;
using h = helics;
using SAInt_API;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using SAIntHelicsLib;
using SAInt_API.Library;

using SAInt_API.Model.Network.Fluid.Gas;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model;

namespace HelicsDotNetReceiver
{
    class Program
    {
        public static GasNet GNET { get; set; }
        public static HubSystem HUB { get; set; }

         static object GetObject(string funcName)
        { 
            var func= typeof(API).GetMethod(funcName,System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return func.Invoke(null, new object[] {});
        }

        static void Main(string[] args)
        {
            Thread.Sleep(100);

            // Load Gas Model - 2 node case
            //string netfolder = @"C:\Getnet Files\HELICS Projects\Gas Fired Generator\";
            //string outputfolder = @"C:\Getnet Files\HELICS Projects\Gas Fired Generator\outputs\2Node\";
            //APIExport.openGNET(netfolder + "GasFiredGenerator.gnet");
            //APIExport.openHUBS(netfolder + "GasFiredGenerator.hubs");
            //APIExport.openGSCE(netfolder + "DYN_GAS.gsce");
            //APIExport.openGCON(netfolder + "DYN_GAS.gcon");

            string netfolder = @"..\..\..\..\Networks\GasFiredGenerator\";
            string outputfolder = @"..\..\..\..\outputs\GasFiredGenerator\";
            API.openGNET(netfolder + "GasFiredGenerator.gnet");

            MappingFactory.AccessFile(netfolder + "GasFiredGenerator.hubs");
            //API.openHUBS(netfolder + "Demo2.hubs");

            API.openGSCE(netfolder + "DYN_GAS.gsce");
            API.openGCON(netfolder + "STEADY_GAS.gcon");
            //API.openGCON(netfolder + "DYN000.gcon");

            MappingFactory.SendAcknowledge();
            MappingFactory.WaitForAcknowledge();
            GNET = (GasNet)GetObject("get_GNET");
            HUB = (HubSystem)GetObject("get_HUBS");

            // Load Gas Model - Demo - Normal Operation
            //string netfolder = @"..\..\..\..\Networks\Demo\";
            //string outputfolder = @"..\..\..\..\outputs\Demo\";
            //APIExport.openGNET(netfolder + "GNET25.net");
            //APIExport.openGSCE(netfolder + "CASE1.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            //Load Gas Model - Demo_disruption - Compressor Outage
            //string netfolder = @"..\..\..\..\Networks\Demo_disruption\";
            //string outputfolder = @"..\..\..\..\outputs\Demo_disruption\";
            //APIExport.openGNET(netfolder + "GNET25.net");
            //APIExport.openGSCE(netfolder + "CASE1.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            // Load Gas Model - DemoAlt - Normal Operation
            //string netfolder = @"..\..\..\..\Networks\DemoAlt\";
            //string outputfolder = @"..\..\..\..\outputs\DemoAlt\";
            //APIExport.openGNET(netfolder + "GNET25.net");
            //APIExport.openGSCE(netfolder + "CASE0.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            // Load Gas Model - DemoAlt_disruption - Compressor Outage
            //string netfolder = @"..\..\..\..\Networks\DemoAlt_disruption\";
            //string outputfolder = @"..\..\..\..\outputs\DemoAlt_disruption\";
            //APIExport.openGNET(netfolder + "GNET25.net");
            //APIExport.openGSCE(netfolder + "CASE1.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            //Load Gas Model - Belgian model - Normal Operation
            //string netfolder = @"..\..\..\..\Networks\Belgium_Case0\";
            //string outputfolder = @"..\..\..\..\outputs\Belgium_Case0\";
            //APIExport.openGNET(netfolder + "GNETBENEWtest.net");
            //APIExport.openGSCE(netfolder + "DYN.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            //Load Gas Model - Belgian model - Compressor Outage
            //string netfolder = @"..\..\..\..\Networks\Belgium_Case1\";
            //string outputfolder = @"..\..\..\..\outputs\Belgium_Case1\";
            //APIExport.openGNET(netfolder + "GNETBENEWtest.net");
            //APIExport.openGSCE(netfolder + "DYN.sce");
            //APIExport.openGCON(netfolder + "CMBSTEOPF.con");

            Directory.CreateDirectory(outputfolder);
#if !DEBUG
            API.showSIMLOG(true);
#else
            API.showSIMLOG(false);
#endif

            // Get HELICS version
            Console.WriteLine($"Gas: Helics version ={helics.helicsGetVersion()}");

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Gas: Creating Federate Info");
            var fedinfo = helics.helicsCreateFederateInfo();

            // Set core type from string
            Console.WriteLine("Gas: Setting Federate Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Gas Federate Core");
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            //If set to true, a federate will not be granted the requested time until all other federates have completed at least 1 iteration
            //of the current time or have moved past it.If it is known that 1 federate depends on others in a non-cyclic fashion, this
            //can be used to optimize the order of execution without iterating.
            //h.helicsFederateInfoSetFlagOption(fedinfo, (int)HelicsFederateFlags.HELICS_FLAG_WAIT_FOR_CURRENT_TIME_UPDATE, 1);
            //h.helicsFederateInfoSetFlagOption(fedinfo, (int)HelicsFederateFlags.HELICS_FLAG_RESTRICTIVE_TIME_POLICY, 1);

            // Federate init string
            Console.WriteLine("Gas: Setting Federate Info Init String");
            string fedinitstring = "--federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            // Create value federate
            Console.WriteLine("Gas: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Gas Federate", fedinfo);
            Console.WriteLine("Gas: Value federate created");

            // Load the mapping between the gas demands and the gas fiered power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);

            // Register Publication and Subscription for coupling points
            foreach (ElectricGasMapping m in MappingList)
            {
                m.GasPubPth = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pth_" + m.GFG.GDEMName, "double", "");
                m.GasPubPbar = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pbar_" + m.GFG.GDEMName, "double", "");
                m.GasPubQ_sm3s = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Qmax_" + m.GFG.GDEMName, "double", "");

                m.GasSubPthe = h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.GFG.FGENName, "");

                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GFG.GDEMName + ".txt", FileMode.Create));
                m.sw.WriteLine("tstep \t iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
            }

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS time steps based on scenario
            double total_time = GNET.SCE.NN;
            Console.WriteLine($"Number of time steps in scenario: {total_time}");

            // set max iteration at 20
            h.helicsFederateSetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Max iterations: {iter_max}");

            // Switch to release mode to enable console output to file
#if !DEBUG
            // redirect console output to log file
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            ostrm = new FileStream(outputfolder + "Log_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(ostrm);
            Console.SetOut(writer);
#endif
            // variables and lists to manage iterations
            int Iter = 0;
            bool HasViolations = false;
            int helics_iter_status = 3;

            double granted_time = 0;
            double requested_time;

            DateTime SCEStartTime = GNET.SCE.StartTime;
            DateTime Trequested;

            TimeStepInfo currenttimestep = new TimeStepInfo() { timestep = 0, itersteps = 0, time = SCEStartTime };
            NotConverged CurrentDiverged = new NotConverged();

            List<TimeStepInfo> timestepinfo = new List<TimeStepInfo>();
            List<NotConverged> NotConverged = new List<NotConverged>();

            var iter_flag = HelicsIterationRequest.HELICS_ITERATION_REQUEST_ITERATE_IF_NEEDED;

            // start initialization mode
            h.helicsFederateEnterInitializingMode(vfed);
            Console.WriteLine("\nGas: Entering Initialization Mode");
            Console.WriteLine("======================================================\n");
            MappingFactory.PublishAvailableThermalPower(0, Iter, MappingList);

            while (true)
            {
                Console.WriteLine("\nGas: Entering Iterative Execution Mode\n");
                HelicsIterationResult itr_status = h.helicsFederateEnterExecutingModeIterative(vfed, iter_flag);

                if (itr_status == HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                {
                    Console.WriteLine($"Gas: Time Step {0} Initialization Completed!");

                    //granted_time = h.helicsFederateRequestTimeIterative(vfed, 0, iter_flag, out helics_iter_status);

                    break;
                }

                // subscribe to available thermal power from gas node
                HasViolations = MappingFactory.SubscribeToRequiredThermalPower(0, Iter, MappingList, "Initialization");

                if (!HasViolations)
                {
                    continue;
                }
                else
                {
                    Iter += 1;
                    MappingFactory.PublishAvailableThermalPower(0, Iter, MappingList);
                }
            }

            int FirstTimeStep = 0;
            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {

                if (e.SolverState == SolverState.BeforeTimeStep && e.TimeStep>0) 
                {
                    Iter = 0; // Iteration number

                    HasViolations = true;

                    if (FirstTimeStep == 0)
                    {
                        Console.WriteLine("======================================================\n");
                        Console.WriteLine("\nGas: Entering Main Co-simulation Loop");
                        Console.WriteLine("======================================================\n");
                        FirstTimeStep += 1;
                    }                    
                    
                    foreach (ElectricGasMapping m in MappingList)
                    {
                        m.lastVal.Clear(); // Clear the list before iteration starts
                    }

                    MappingFactory.PublishAvailableThermalPower(e.TimeStep, Iter, MappingList);

                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt) };
                    timestepinfo.Add(currenttimestep);
                }

                if (e.SolverState == SolverState.AfterTimeStep && e.TimeStep > 0)
                {
                    // Counting iterations
                    Iter += 1;
                    currenttimestep.itersteps += 1;

                    //Iterative HELICS time request
                    Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt);
                    Console.WriteLine($"\nGas Requested Time: {Trequested}, iteration: {Iter}");

                    granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, iter_flag, out helics_iter_status);

                    Console.WriteLine($"Gas Granted Co-simulation Time Step: {granted_time},  Iteration status: {helics_iter_status}, SolverState: {e.SolverState}");

                    if (helics_iter_status == (int)HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                    {
                        Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Completed!");

                        e.RepeatTimeIntegration = false;
                    }

                    // get requested thermal power from connected gas plants, determine if there are violations
                    HasViolations = MappingFactory.SubscribeToRequiredThermalPower(e.TimeStep, Iter, MappingList);

                    // Publish if it is repeating and has violations so that the iteration continues
                    if (HasViolations)
                    {
                        if (Iter < iter_max)
                        {
                            MappingFactory.PublishAvailableThermalPower(e.TimeStep, Iter, MappingList);
                            e.RepeatTimeIntegration = true;
                        }
                        else
                        {
                            CurrentDiverged = new NotConverged() { timestep = e.TimeStep, itersteps = Iter, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt) };
                            NotConverged.Add(CurrentDiverged);
                            Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Not Converged!");
                            e.RepeatTimeIntegration = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Completed!");
                    }

                }

                // ACOPF starts at time step 1, while dynamic gas starts at time step = 0
                else if (e.SolverState == SolverState.AfterTimeStep && e.TimeStep == 0)
                {
                    e.RepeatTimeIntegration = false;
                }

            };

            Console.WriteLine("======================================================\n");
            Console.WriteLine("\nGas: Starting the Gas Simulation");
            Console.WriteLine("======================================================\n");
            // run the gas network model
            API.runGSIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime DateTimeRequested = GNET.SCE.EndTime + new TimeSpan(0, 0, (int)GNET.SCE.dt);
            Console.WriteLine($"\nGas Requested Time Step: {requested_time} at Time: {DateTimeRequested}");
            h.helicsFederateRequestTime(vfed, requested_time);

#if !DEBUG
            // close out log file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
#endif

            // save SAInt output
            API.writeGSOL(netfolder + "gsolin.txt", outputfolder + "gsolout_HELICS.txt");

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();

            using (FileStream fs = new FileStream(outputfolder + "TimeStepInfo_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t\t\t\t TimeStep \t IterStep");
                    foreach (TimeStepInfo x in timestepinfo)
                    {
                        sw.WriteLine(String.Format("{0}\t{1}\t\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }
            using (FileStream fs = new FileStream(outputfolder + "NotConverged_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t\t\t\t TimeStep \t IterStep");
                    foreach (NotConverged x in NotConverged)
                    {
                        sw.WriteLine(String.Format("{0}\t{1}\t\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }

            // Diverging time steps
            if (NotConverged.Count == 0)
                Console.WriteLine("Gas: There is no diverging time step");
            else
            {
                Console.WriteLine("Gas: the solution diverged at the following time steps:");
                foreach (NotConverged x in NotConverged)
                { 
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}"); 
                }
                Console.WriteLine($"Gas: The total number of diverging time steps = { NotConverged.Count }");
            }

            foreach (ElectricGasMapping m in MappingList)
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
