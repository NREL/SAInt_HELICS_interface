using System;
using h = helics;
using SAInt_API;
using SAInt_API.Library;
using System.Collections.Generic;
using System.IO;
using SAIntHelicsLib;
using SAInt_API.Model.Network.Electric;
using SAInt_API.Model.Network.Hub;

namespace SAIntElectricFederate
{
    class ElectricFederate
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
            Console.WriteLine("\nMake sure that all the model files are in the same folder." +
                "\nEnter the electric network folder path:");
            string NetworkSourceFolder = Console.ReadLine() + @"\"; // @"..\..\..\..\Networks\Demo"

            Console.WriteLine("\nEnter the electric network file name:");
            string NetFileName = Console.ReadLine(); // "ENET30.enet"

            Console.WriteLine("\nEnter the electric scenario file name:");
            string SceFileName = Console.ReadLine(); // "PCM001.esce"           

            Console.WriteLine("\nIf there is an initial state file, enter Y:");
            string InitialStateExist = Console.ReadLine();
            string StateFileName = "Null";
            if (InitialStateExist == "Y" || InitialStateExist == "y")
            {
                Console.WriteLine("\nEnter the electric state file name:");
                StateFileName = Console.ReadLine(); // "CMBSTEOPF.econ"
            }

            Console.WriteLine("\nEnter the hub file name:");
            string HubFileName = Console.ReadLine(); // "Demo.hubs"

            Console.WriteLine("\nIf there is a solution description file, enter Y:");
            string SolDescExist = Console.ReadLine();
            string SolDescFileName = "Null";
            if (SolDescExist == "Y" || SolDescExist == "y")
            {
                Console.WriteLine("\nEnter the electric solution description file name:");
                SolDescFileName = Console.ReadLine(); // "esolin.txt"
            }

            string OutputFolder = NetworkSourceFolder + @"\Outputs\DCUCOPF_DynGas\" + SceFileName + @"\";
            Directory.CreateDirectory(OutputFolder);

            // Wait until the hub file is accessible
            MappingFactory.WaitForAcknowledge();

            API.openENET(NetworkSourceFolder + NetFileName);
            MappingFactory.AccessFile(NetworkSourceFolder + HubFileName);
            API.openESCE(NetworkSourceFolder + SceFileName);
            if (InitialStateExist == "Y" || InitialStateExist == "y")
            {                
                API.openECON(NetworkSourceFolder + StateFileName);
            }
            MappingFactory.SendAcknowledge();

            ENET = (ElectricNet)GetObject("get_ENET");
            HUB = (HubSystem)GetObject("get_HUBS");

            //ENET.SCE.SolverType = SolverType.Gurobi;
            //ENET.SCE.SolverModel = SolverModel.MIP;
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

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Electric: Time period: {period_set}");

            // set number of HELICS time steps based on scenario
            double total_time = ENET.SCE.NN;
            Console.WriteLine($"Electric: Number of time steps in scenario: {total_time}");

            // set max iteration at 20
            h.helicsFederateSetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Electric: Max iterations per time step: {iter_max}");

            var iter_flag = HelicsIterationRequest.HELICS_ITERATION_REQUEST_ITERATE_IF_NEEDED;

            // Register publication for the time horizon
            SWIGTYPE_p_void HorizonPub = h.helicsFederateRegisterGlobalTypePublication(vfed, "Horizon", "int", "");
            SWIGTYPE_p_void HorizonSub = h.helicsFederateRegisterSubscription(vfed, "HorizonReceive", "");
            int Horizon = ENET.SCE.NNHRZ;
            int HorizonReceived = -1;

            //start initialization mode to publish time horizon
            h.helicsFederateEnterInitializingMode(vfed);
            h.helicsPublicationPublishInteger(HorizonPub, Horizon);
            while (true)
            {
                HorizonReceived = (int)h.helicsInputGetInteger(HorizonSub);

                HelicsIterationResult itr_status = h.helicsFederateEnterExecutingModeIterative(vfed, iter_flag);
                if (itr_status == HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                {
                    break;
                }               

                h.helicsPublicationPublishInteger(HorizonPub, Horizon);
                if (HorizonReceived == Horizon) break;
            }
            Console.WriteLine($"\nElectric: Number of time steps in a single horizon: {Horizon} ");

            // Load the mapping between the gas demands and the gas fired power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);            

            // Register Publication and Subscription for coupling points
            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < Horizon; i++)
                {
                    m.RequieredFuelRate[i] = h.helicsFederateRegisterGlobalTypePublication(vfed, "FuelReq_" + m.GFG.FGENName + i.ToString(), "double", "");
                    m.AvailableFuelRate[i] = h.helicsFederateRegisterSubscription(vfed, "FuelAvail_" + m.GFG.GDEMName + i.ToString(), "");
                    m.PressureRelativeToPmin[i] = h.helicsFederateRegisterSubscription(vfed, "DeltaP_" + m.GFG.GDEMName + i.ToString(), "");

                    m.LastVal.Add(i, new List<double>());
                }
                m.Horizon = Horizon;
                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(OutputFolder + m.GFG.FGENName + ".txt", FileMode.Create));
                m.sw.WriteLine("Date\t\t\t\t TimeStep\t Iteration \t PG[MW] \t FuelRate[m3/s]\t PGMAX[MW]\t FMAX[m3/s]");
            }            

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
            
            int HorizonStartingTimeStep = 1;
            int CountHorizons = 0;

            double granted_time = 0;
           
            TimeStepInfo CurrentHorizon = new TimeStepInfo() {HorizonStep = 0, IterationCount = 0};
            TimeStepInfo CurrentDiverged = new TimeStepInfo();

            List<TimeStepInfo> IterationInfo = new List<TimeStepInfo>();
            List<TimeStepInfo> AllDiverged = new List<TimeStepInfo>();

            // start initialization mode
            h.helicsFederateEnterInitializingMode(vfed);
            Console.WriteLine("\nElectric: Entering Initialization Mode");
            Console.WriteLine("======================================================\n");
            MappingFactory.PublishRequiredFuelRate(1, Iter, MappingList);

            while (true)
            {
                Console.WriteLine("\nElectric: Initialize Iterative Execution Mode\n");
                HelicsIterationResult itr_status = h.helicsFederateEnterExecutingModeIterative(vfed, iter_flag);

                if (itr_status == HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                {
                    Console.WriteLine($"Electric: Horizon {1} Initialization Completed!");
                    break;
                }

                // subscribe to available thermal power from gas node
                HasViolations = MappingFactory.SubscribeToAvailableFuelRate(1, Iter, MappingList, "Initialization");                

                if (!HasViolations)
                {
                    continue;
                }
                else
                {                    
                    MappingFactory.PublishRequiredFuelRate(1, Iter, MappingList);
                    Iter += 1;
                }
            }

            int FirstTimeStep = 0;
            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {                     

                //if (e.SolverState == SolverState.BeforeTimeStep && e.TimeStep > 0)
                if (e.SolverState == SolverState.BeforeConsecutiveRun)
                {
                    Iter = 0; // Iteration number
                    CountHorizons += 1;
                    HorizonStartingTimeStep = (CountHorizons - 1) * Horizon +1;
                    HasViolations = true;
                    if (FirstTimeStep == 0)
                    {
                        Console.WriteLine("======================================================\n");
                        Console.WriteLine("\nElectric: Entering Main Co-simulation Loop");
                        Console.WriteLine("======================================================\n");
                        FirstTimeStep += 1;
                    }

                    if (CountHorizons > 1)
                    {
                        foreach (ElectricGasMapping m in MappingList)
                        {
                            for (int i = 0; i < m.Horizon; i++)
                            {
                                // Clear the list before iteration starts
                                m.LastVal[i].Clear();
                            }
                        }
                    }
                    // Set time step info
                    CurrentHorizon = new TimeStepInfo() { HorizonStep = CountHorizons, IterationCount = 0};
                    IterationInfo.Add(CurrentHorizon);
                }
                if ( e.SolverState == SolverState.AfterConsecutiveRun)
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
                            MappingFactory.PublishRequiredFuelRate(HorizonStartingTimeStep, Iter, MappingList);
                            e.RepeatConsecutiveRun = 1;
                        }
                        else if (Iter == iter_max)
                        {
                            granted_time = h.helicsFederateRequestTimeIterative(vfed, CountHorizons, iter_flag, out helics_iter_status);

                            CurrentDiverged = new TimeStepInfo() { HorizonStep = CountHorizons, IterationCount = Iter};
                            AllDiverged.Add(CurrentDiverged);
                            Console.WriteLine($"Electric: Horizon {CountHorizons} Iteration Not Converged!");
                        }
                    }
                    else
                    {
                        granted_time = h.helicsFederateRequestTimeIterative(vfed, CountHorizons, iter_flag, out helics_iter_status);
                        Console.WriteLine($"Electric: Horizon {CountHorizons} Iteration Converged!");
                    }

                    // Iterative HELICS time request
                    Console.WriteLine($"\nElectric: Requested Horizon: {CountHorizons}, iteration: {Iter}");

                    granted_time = h.helicsFederateRequestTimeIterative(vfed, CountHorizons, iter_flag, out helics_iter_status);
                    
                    Console.WriteLine($"Electric: Granted Co-simulation Horizon: {granted_time}, Iteration Status: {helics_iter_status}, SolverState: {e.SolverState}");

                    if (helics_iter_status == (int)HelicsIterationResult.HELICS_ITERATION_RESULT_NEXT_STEP)
                    {
                        // Check if the the simulation is also converged while the other federate has converged
                        if (HasViolations) { HasViolations = MappingFactory.SubscribeToAvailableFuelRate(HorizonStartingTimeStep, Iter, MappingList); }
                        Console.WriteLine($"Electric: Horizon {CountHorizons} Iteration Stopped!\n");
                            e.RepeatConsecutiveRun = 0;                        
                    }
                    else
                    {
                        // get available thermal power at nodes, determine if there are violations
                        HasViolations = MappingFactory.SubscribeToAvailableFuelRate(HorizonStartingTimeStep, Iter, MappingList);
                    }

                    // Counting iterations
                    Iter += 1;
                    CurrentHorizon.IterationCount += 1;
                }
                
            };

            Console.WriteLine("======================================================\n");
            Console.WriteLine("\nElectric: Starting the Electric Simulation");
            Console.WriteLine("======================================================\n");
            // run the electric network model
            API.runESIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            int requested_time = (int)total_time / Horizon + 1; ;            
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime DateTimeRequested = ENET.SCE.EndTime.AddSeconds(ENET.SCE.dt); 
            Console.WriteLine($"\nElectric: Requested Horizon: {requested_time} at Time: {DateTimeRequested}");
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

            // Export SAInt output and scenario events
            string result = SceFileName.Split('.')[0];
            File.Copy(NetworkSourceFolder + result + ".esol", OutputFolder + result + ".esol", true);
            File.Copy(NetworkSourceFolder + result + ".econ", OutputFolder + result + ".econ", true);
            if (SolDescExist == "Y" || SolDescExist == "y")
            {
                API.writeESOL(NetworkSourceFolder + SolDescFileName, OutputFolder + "esolout_HELICS.xlsx");
            }
            API.exportESCE(OutputFolder + "ElectricScenarioEventsESCE.xlsx");

            using (FileStream fs=new FileStream(OutputFolder + "HorizonIterationInfo_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write)) 
            {   
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Horizon \t\t IterStep");
                    foreach (TimeStepInfo x in IterationInfo)
                    {
                        sw.WriteLine(String.Format("{0}\t\t\t\t{1}", x.HorizonStep, x.IterationCount));
                    }
                }

            }           

            using (FileStream fs = new FileStream(OutputFolder + "NotConverged_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("HorizonStep \t IterStep");
                    foreach (TimeStepInfo x in AllDiverged)
                    {
                        sw.WriteLine(String.Format("{0}\t\t\t{1}", x.HorizonStep, x.IterationCount));
                    }
                }
            }

            // Diverging time steps
            if (AllDiverged.Count == 0)
                Console.WriteLine("Electric: There is no diverging Horizons");
            else
            {
                Console.WriteLine("Electric: the solution diverged at the following Horizons:");
                foreach (TimeStepInfo x in AllDiverged)
                {
                    Console.WriteLine($"Horizon {x.HorizonStep}");
                }
                Console.WriteLine($"Electric: The total number of diverging Horizons = { AllDiverged.Count }");
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