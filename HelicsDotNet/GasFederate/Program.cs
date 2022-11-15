using System;
using h = helics;
using SAInt_API;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using SAIntHelicsLib;
using SAInt_API.Library;
using SAInt_API.Library.Units;
using SAInt_API.Model.Scenarios;

using SAInt_API.Model.Network.Fluid.Gas;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model;
using System.Linq;

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
            //Thread.Sleep(100);

            //string netfolder = @"..\..\..\..\Networks\GasFiredGenerator\";
            //string outputfolder = @"..\..\..\..\outputs\GasFiredGenerator\";
            //API.openGNET(netfolder + "GasFiredGenerator.gnet");
            //MappingFactory.AccessFile(netfolder + "GasFiredGenerator.hubs");
            //API.openGSCE(netfolder + "DYN_GAS.gsce");
            //API.openGCON(netfolder + "STEADY_GAS.gcon");

            string netfolder = @"..\..\..\..\Networks\DemoCase\WI_4746\";
            string outputfolder = @"..\..\..\..\outputs\DemoCase\WI_4746\";
            API.openGNET(netfolder + "GNET25.gnet");
            MappingFactory.AccessFile(netfolder + "Demo.hubs");
            API.openGSCE(netfolder + "CASE1.gsce");
            API.openGCON(netfolder + "CMBSTEOPF.gcon");

            MappingFactory.SendAcknowledge();
            MappingFactory.WaitForAcknowledge();

            GNET = (GasNet)GetObject("get_GNET");
            HUB = (HubSystem)GetObject("get_HUBS");

            Directory.CreateDirectory(outputfolder);
#if !DEBUG
            API.showSIMLOG(true);
#else
            API.showSIMLOG(false);
#endif

            // Get HELICS version
            Console.WriteLine($"Gas: HELICS version ={h.helicsGetVersion()}");

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Gas: Creating Federate Info");
            var fedinfo = h.helicsCreateFederateInfo();

            // Set core type from string
            Console.WriteLine("Gas: Setting Federate Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Gas Federate Core");
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");
            
            // Federate init string
            Console.WriteLine("Gas: Setting Federate Info Init String");
            string fedinitstring = "--federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            // Create value federate
            Console.WriteLine("Gas: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Gas Federate", fedinfo);
            Console.WriteLine("Gas: Value federate created");

            // Load the mapping between the gas demands and the gas fired power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);

            // Register Publication and Subscription for coupling points
            foreach (ElectricGasMapping m in MappingList)
            {
                m.AvailableThermalPower = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pth_" + m.GFG.GDEMName, "double", "");
                m.PressureRelativeToPmin = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pbar_" + m.GFG.GDEMName, "double", "");
                m.RequieredThermalPower = h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.GFG.FGENName, "");

                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GFG.GDEMName + ".txt", FileMode.Create));
                m.sw.WriteLine("Date\t\t\t\t TimeStep\t Iteration \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
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
            int Iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Max iterations per time step: {Iter_max}");

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
            bool HasViolations = true;
            int helics_iter_status = 3;

            double granted_time = 0;    

            TimeStepInfo currenttimestep = new TimeStepInfo() { timestep = 0, itersteps = 0, time = GNET.SCE.dTime(0)};
            TimeStepInfo CurrentDiverged = new TimeStepInfo();

            List<TimeStepInfo> IterationInfo = new List<TimeStepInfo>();
            List<TimeStepInfo> AllDiverged = new List<TimeStepInfo>();

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
                    MappingFactory.PublishAvailableThermalPower(0, Iter, MappingList);
                    Iter += 1;
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
                    foreach  (ElectricGasMapping m in MappingList)
                    { 
                        m.lastVal.Clear(); // Clear the list before iteration starts                
                    }

                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0, time = GNET.SCE.dTime[e.TimeStep]};
                    IterationInfo.Add(currenttimestep);
                }

                if (e.SolverState == SolverState.AfterTimeStep && e.TimeStep > 0)
                {
                    // Publish if it is repeating and has violations so that the iteration continues
                    if (HasViolations)
                    {
                        if (Iter < Iter_max)
                        {
                            MappingFactory.PublishAvailableThermalPower(e.TimeStep, Iter, MappingList);
                            e.RepeatTimeStep = 1;
                        }
                        else if (Iter == Iter_max)
                        {
                            granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, iter_flag, out helics_iter_status);
                            CurrentDiverged = new TimeStepInfo() { timestep = e.TimeStep, itersteps = Iter, time = GNET.SCE.dTime[e.TimeStep] };
                            AllDiverged.Add(CurrentDiverged);
                            Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Not Converged!");
                        }
                    }
                    else
                    {
                        granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, iter_flag, out helics_iter_status);
                        Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Converged!");
                    }

                    //Iterative HELICS time request
                    Console.WriteLine($"\nGas Requested Time: {GNET.SCE.dTime[e.TimeStep]}, iteration: {Iter}");

                    granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, iter_flag, out helics_iter_status);

                    Console.WriteLine($"Gas Granted Co-simulation Time Step: {granted_time},  Iteration status: {helics_iter_status}, SolverState: {e.SolverState}");

                    if (helics_iter_status == (int)HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                    {
                        Console.WriteLine($"Gas: Time Step {e.TimeStep} Iteration Stopped!\n");
                            e.RepeatTimeStep = 0; 
                    }
                    else
                    {  
                        // get requested thermal power from connected gas plants, determine if there are violations
                        HasViolations = MappingFactory.SubscribeToRequiredThermalPower(e.TimeStep, Iter, MappingList);
                    }

                    // Counting iterations
                    Iter += 1;
                    currenttimestep.itersteps += 1;
                }

                // ACOPF starts at time step 1, while dynamic gas starts at time step = 0
                else if (e.SolverState == SolverState.AfterTimeStep && e.TimeStep == 0)
                {
                    e.RepeatTimeStep = 0;
                }

            };

            Console.WriteLine("======================================================\n");
            Console.WriteLine("\nGas: Starting the Gas Simulation");
            Console.WriteLine("======================================================\n");
            // run the gas network model
            API.runGSIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            DateTime DateTimeRequested = GNET.SCE.EndTime.AddSeconds(GNET.SCE.dt);
            Console.WriteLine($"\nGas Requested Time Step: {total_time + 1} at Time: {DateTimeRequested}");
            h.helicsFederateRequestTime(vfed, total_time + 1);

#if !DEBUG
            // close out log file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
#endif

            // save SAInt output
            API.writeGSOL(netfolder + "gsolin.txt", outputfolder + "gsolout_HELICS.xlsx");
            API.exportGSCE(outputfolder + "GSCE.xlsx");

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();

            using (FileStream fs = new FileStream(outputfolder + "TimeStepIterationInfo_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t\t\t\t\t TimeStep \t\t IterStep");
                    foreach (TimeStepInfo x in IterationInfo)
                    {
                        sw.WriteLine(String.Format("{0} \t\t {1}\t\t\t\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }
            using (FileStream fs = new FileStream(outputfolder + "NotConverged_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t\t\t\t TimeStep \t IterStep");
                    foreach (TimeStepInfo x in AllDiverged)
                    {
                        sw.WriteLine(String.Format("{0} \t{1}\t\t\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }

            // Diverging time steps
            if (AllDiverged.Count == 0)
                Console.WriteLine("Gas: There is no diverging time step");
            else
            {
                Console.WriteLine("Gas: the solution diverged at the following time steps:");
                foreach (TimeStepInfo x in AllDiverged)
                { 
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}"); 
                }
                Console.WriteLine($"Gas: The total number of diverging time steps = { AllDiverged.Count }");
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
