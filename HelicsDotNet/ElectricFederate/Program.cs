using System;
using h = helics;
using SAInt_API;
//using SAInt_API.NetList
using SAInt_API.Library;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using SAIntHelicsLib;

//using SAInt_API.Model.Network.NetSystem;
//using SAInt_API.Model.Scenarios;
using SAInt_API.Model.Network.Electric;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model;

namespace HelicsDotNetSender
{
    class Program
    {

        public static ElectricNet ENET { get; set; }
        public static HubSystem HUB { get; set; }

        static object GetObject(string funcName)
        {
            var func = typeof(API).GetMethod(funcName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return func.Invoke(null, new object[] { });
        }
        static void Main(string[] args)
        {

            MappingFactory.WaitForAcknowledge();
            string netfolder = @"..\..\..\..\Networks\GasFiredGenerator\";
            string outputfolder = @"..\..\..\..\outputs\GasFiredGenerator\";
            API.openENET(netfolder + "GasFiredGenerator.enet");

            MappingFactory.AccessFile(netfolder + "GasFiredGenerator.hubs");
            //API.openHUBS(netfolder + "Demo.hubs");

            API.openESCE(netfolder + "QDYNACOPF.esce");
            API.openECON(netfolder + "QDYN_ACPF_OFF_ON.econ");

            MappingFactory.SendAcknowledge();
            ENET = (ElectricNet)GetObject("get_ENET");
            HUB = (HubSystem)GetObject("get_HUBS");

            Directory.CreateDirectory(outputfolder);
#if !DEBUG
            API.showSIMLOG(true);
#else
            API.showSIMLOG(false);
#endif
            // Get HELICS version
            Console.WriteLine($"Electric: HELICS version ={h.helicsGetVersion()}");

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Electric: Creating Federate Info");
            var fedinfo = h.helicsCreateFederateInfo();

            // Set core type from string
            Console.WriteLine("Electric: Setting Federate Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Electric Federate Core");
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            // Federate init string
            Console.WriteLine("Electric: Setting Federate Info Init String");
            string fedinitstring = "--federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            // Create value federate
            Console.WriteLine("Electric: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo);
            Console.WriteLine("Electric: Value federate created");

            // Load the mapping between the gas demands and the gas fired power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);

            // Register Publication and Subscription for coupling points
            foreach (ElectricGasMapping m in MappingList)
            {
                m.RequieredThermalPower = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.GFG.FGENName, "double", "");
                m.AvailableThermalPower = h.helicsFederateRegisterSubscription(vfed, "PUB_Pth_" + m.GFG.GDEMName, "");
                m.PressureRelativeToPmin = h.helicsFederateRegisterSubscription(vfed, "PUB_Pbar_" + m.GFG.GDEMName, "");

                for (int i = 1; i <= m.HorizonTimeSteps; i++)
                {
                    m.RequieredThermalPower02[i] = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.GFG.FGENName + i.ToString(), "double", ""); ;
                    m.AvailableThermalPower02[i] = h.helicsFederateRegisterSubscription(vfed, "PUB_Pth_" + m.GFG.GDEMName + i.ToString(), ""); ;
                    m.PressureRelativeToPmin02[i] = h.helicsFederateRegisterSubscription(vfed, "PUB_Pbar_" + m.GFG.GDEMName + i.ToString(), ""); ;
                }

                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GFG.FGENName + ".txt", FileMode.Create));
                m.sw.WriteLine("Date\t\t\t\t TimeStep\t Iteration \t PG[MW] \t ThPow [MW] \t PGMAX [MW]");
            }
            
            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS time steps based on scenario
            double total_time = ENET.SCE.NN;
            Console.WriteLine($"Number of time steps in scenario: {total_time}");

            // set max iteration at 20
            h.helicsFederateSetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Max iterations per time step: {iter_max}");

            // Switch to release mode to enable console output to file 
#if !DEBUG
            // redirect console output to log file
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            ostrm = new FileStream(outputfolder + "Log_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(ostrm);
            Console.SetOut(writer);
#endif
            // variables and lists to manage iterations
            int Iter = 0;   
            bool HasViolations = true;
            int helics_iter_status = 3;

            double granted_time = 0;
            double requested_time;

            DateTime SCEStartTime = ENET.SCE.StartTime;
            DateTime Trequested;
           
            TimeStepInfo currenttimestep = new TimeStepInfo() {timestep = 0, itersteps = 0,time= SCEStartTime};
            TimeStepInfo CurrentDiverged = new TimeStepInfo();

            List<TimeStepInfo> IterationInfo = new List<TimeStepInfo>();
            List<TimeStepInfo> NotConverged = new List<TimeStepInfo>();

            var iter_flag = HelicsIterationRequest.HELICS_ITERATION_REQUEST_ITERATE_IF_NEEDED;

            // start initialization mode
            h.helicsFederateEnterInitializingMode(vfed);
            Console.WriteLine("\nElectric: Entering Initialization Mode");
            Console.WriteLine("======================================================\n");
            MappingFactory.PublishRequiredThermalPower(0, Iter, MappingList);

            while (true)
            {
                Console.WriteLine("\nElectric: Entering Iterative Execution Mode\n");
                HelicsIterationResult itr_status = h.helicsFederateEnterExecutingModeIterative(vfed, iter_flag);

                if (itr_status == HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                {
                    Console.WriteLine($"Electric: Time Step {0} Initialization Completed!");
                    break;
                }

                // subscribe to available thermal power from gas node
                HasViolations = MappingFactory.SubscribeToAvailableThermalPower(0, Iter, MappingList, "Initialization");                

                if (!HasViolations)
                {
                    continue;
                }
                else
                {                    
                    MappingFactory.PublishRequiredThermalPower(0, Iter, MappingList);
                    Iter += 1;
                }
            }

            int FirstTimeStep = 0;
            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {                     

                if (e.SolverState == SolverState.BeforeTimeStep && e.TimeStep > 0)
                {
                    Iter = 0;

                    HasViolations = true;

                    if (FirstTimeStep == 0)
                    {
                        Console.WriteLine("======================================================\n");
                        Console.WriteLine("\nElectric: Entering Main Co-simulation Loop");
                        Console.WriteLine("======================================================\n");
                        FirstTimeStep += 1;
                    }                                      

                    // Reset nameplate capacity
                    foreach (ElectricGasMapping m in MappingList)
                    {
                        //foreach (var evt in m.GFG.FGEN.SceList)
                        //{
                        //    if (evt.ObjPar == CtrlType.PSET)
                        //    {
                        //        //m.GFG.FGEN.SceList.Remove(evt);
                        //        evt.Processed = true;
                        //        evt.ObjVal = double.NaN;
                        //        //evt.ObjPar = CtrlType.NONE;
                        //    }
                        //}
                        m.lastVal.Clear(); // Clear the list before iteration starts
                    }

                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0,time= SCEStartTime + new TimeSpan(0,0,e.TimeStep*(int)ENET.SCE.dt)};
                    IterationInfo.Add(currenttimestep);
                }

                if ( e.SolverState == SolverState.AfterTimeStep && e.TimeStep > 0)
                {
#if !DEBUG 
                    foreach (var i in ENET.Generators)
                    {
                        Console.WriteLine($"{i.Name} \t {i.get_P(e.TimeStep)}");
                    }
#endif
                    // Publish if it is repeating and has violations so that the iteration continues
                    if (HasViolations)
                    {
                        if (Iter < iter_max)
                        {
                            MappingFactory.PublishRequiredThermalPower(e.TimeStep, Iter, MappingList);
                            e.RepeatTimeIntegration = true;
                        }
                        else if (Iter == iter_max)
                        {
                            CurrentDiverged = new TimeStepInfo() { timestep = e.TimeStep, itersteps = Iter, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)ENET.SCE.dt) };
                            NotConverged.Add(CurrentDiverged);
                            Console.WriteLine($"Electric: Time Step {e.TimeStep} Iteration Not Converged!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Electric: Time Step {e.TimeStep} Iteration Converged!");
                    }

                    // Iterative HELICS time request
                    Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)ENET.SCE.dt);
                    Console.WriteLine($"\nElectric Requested Time: {Trequested}, iteration: {Iter}");

                    granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, iter_flag, out helics_iter_status);
                    
                    Console.WriteLine($"Electric Granted Co-simulation Time Step: {granted_time}, Iteration Status: {helics_iter_status}, SolverState: {e.SolverState}");

                    if (helics_iter_status == (int)HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                    {
                        Console.WriteLine($"Electric: Time Step {e.TimeStep} Iteration Stopped!\n");
                        e.RepeatTimeIntegration = false;
                    }

                    else
                    {
                        // get available thermal power at nodes, determine if there are violations
                        HasViolations = MappingFactory.SubscribeToAvailableThermalPower(e.TimeStep, Iter, MappingList);
                    }

                    // Counting iterations
                    Iter += 1;
                    currenttimestep.itersteps += 1;
                }
                
            };

            Console.WriteLine("======================================================\n");
            Console.WriteLine("\nElectric: Starting the Electric Simulation");
            Console.WriteLine("======================================================\n");
            // run the electric network model
            API.runESIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;            
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime DateTimeRequested = ENET.SCE.EndTime + new TimeSpan(0, 0, (int)ENET.SCE.dt);
            Console.WriteLine($"\nElectric Requested Time Step: {requested_time} at Time: {DateTimeRequested}");
            h.helicsFederateRequestTime(vfed, requested_time);


#if !DEBUG
            // close out log file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
#endif

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Electric: Federate finalized");
            h.helicsFederateFree(vfed);
            h.helicsCloseLibrary();

            // save SAInt output
            API.writeESOL(netfolder + "esolin.txt", outputfolder + "esolout_HELICS.txt");

            using (FileStream fs=new FileStream(outputfolder + "TimeStepIterationInfo_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write)) 
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
            
            using (FileStream fs = new FileStream(outputfolder + "NotConverged_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t\t\t\t TimeStep \t IterStep");
                    foreach (TimeStepInfo x in NotConverged)
                    {
                        sw.WriteLine(String.Format("{0}\t\t\t\t{1}\t\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }

            // Diverging time steps
            if (NotConverged.Count == 0)
                Console.WriteLine("Electric: There is no diverging time step");
            else
            {
                Console.WriteLine("Electric: the solution diverged at the following time steps:");
                foreach (TimeStepInfo x in NotConverged)
                {
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}");
                }
                Console.WriteLine($"Electric: The total number of diverging time steps = { NotConverged.Count }");
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