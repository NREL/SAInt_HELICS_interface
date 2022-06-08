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

            string netfolder = @"C:\Getnet Files\SAInt 3.0 combind simulation Exr\NewNetworkFiles\DemoSAInt3.0\";
            string outputfolder = @"C:\Getnet Files\SAInt 3.0 combind simulation Exr\NewNetworkFiles\outputs\Demo\";
            API.openGNET(netfolder + "GNET25.gnet");

            MappingFactory.AccessFile(netfolder + "Demo.hubs");

            //API.openHUBS(netfolder + "Demo.hubs");

            API.openGSCE(netfolder + "CASE1.gsce");
            API.openGCON(netfolder + "CMBSTEOPF.gcon");

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
#if DEBUG
            API.showSIMLOG(true);
#else
            APIExport.showSIMLOG(false);
#endif

            // Load mapping between gas nodes and power plants 
            //List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);

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

            // Register Publication and Subscription for coupling points
            // Load mapping between gas nodes and power plants 
            //List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);
            //foreach (ElectricGasMapping m in MappingList)
            //{
            //    m.GasPubPth = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pth_" + m.GasNodeID, "double", "");
            //    m.GasPubPbar = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pbar_" + m.GasNodeID, "double", "");

            //    m.ElectricSub = h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.ElectricGenID, "");

            //    //Streamwriter for writing iteration results into file
            //    m.sw = new StreamWriter(new FileStream(outputfolder + m.GasNode.Name + ".txt", FileMode.Create));
            //    m.sw.WriteLine("tstep \t iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
            //}

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS timesteps based on scenario
            double total_time = GNET.SCE.NN;
            Console.WriteLine($"Number of timesteps in scenario: {total_time}");

            double granted_time = 0;
            double requested_time;

            // set max iteration at 20
            h.helicsFederateSetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Max iterations: {iter_max}");

            // start initialization mode
            //h.helicsFederateEnterInitializingMode(vfed);
            //Console.WriteLine("Gas: Entering initialization mode");

            // enter execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Gas: Entering execution mode");

            // Register Publication and Subscription for coupling points
            // Load mapping between gas nodes and power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);
            foreach (ElectricGasMapping m in MappingList)
            {
                m.GasPubPth = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pth_" + m.GFG.GDEMName, "double", "");
                m.GasPubPbar = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pbar_" + m.GFG.GDEMName, "double", "");

                m.ElectricSub = h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.GFG.FGENName, "");

                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GFG.GDEMName + ".txt", FileMode.Create));
                m.sw.WriteLine("tstep \t iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
            }

            Int16 step = 0;
            List<TimeStepInfo> timestepinfo = new List<TimeStepInfo>();
            List<NotConverged> notconverged = new List<NotConverged>();
            bool IsRepeating = false;
            bool HasViolations = false;

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
            DateTime SCEStartTime = GNET.SCE.StartTime;
            DateTime Trequested;
            DateTime Tgranted;
            TimeStepInfo currenttimestep = new TimeStepInfo() { timestep = 0, itersteps = 0, time = SCEStartTime };
            NotConverged CurrentDiverged = new NotConverged();

            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {

                if (e.SolverState == SolverState.BeforeTimeStep) {


                    // non-iterative time request here to block until both federates are done iterating
                    Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt);
                    Console.WriteLine($"Requested time {Trequested}");
                    //Console.WriteLine($"Requested time {e.TimeStep}");

                    step = 0; // Iteration number

                    // HELICS time granted 
                    granted_time = h.helicsFederateRequestTime(vfed, e.TimeStep);
                    Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time - 1) * (int)GNET.SCE.dt);
                    Console.WriteLine($"Granted time: {Tgranted}, SolverState: {e.SolverState}");
                    //Console.WriteLine($"Granted time: {granted_time}, SolverState: {e.SolverState}");

                    IsRepeating = !IsRepeating;
                    HasViolations = true;

                    foreach (ElectricGasMapping m in MappingList)
                    {
                        m.lastVal.Clear();
                    }
                    // Publishing initial available thermal power of zero MW and zero peressure difference
                    if (e.TimeStep == 0)
                    {
                        MappingFactory.PublishAvailableThermalPower(granted_time - 1, step, MappingList);
                    }
                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt) };
                    timestepinfo.Add(currenttimestep);
                }

                if (e.SolverState == SolverState.AfterTimeStep && IsRepeating)
                {
                    // stop iterating if max iterations have been reached
                    IsRepeating = (step < iter_max);

                    if (IsRepeating)
                    {
                        step += 1;
                        currenttimestep.itersteps += 1;

                        int helics_iter_status;

                        // iterative HELICS time request
                        Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt);
                        Console.WriteLine($"Requested time: {Trequested}, iteration: {step}");

                        // HELICS time granted 
                        granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, HelicsIterationRequest.HELICS_ITERATION_REQUEST_FORCE_ITERATION, out helics_iter_status);
                        Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time - 1) * (int)GNET.SCE.dt);
                        Console.WriteLine($"Granted time: {Tgranted},  Iteration status: {helics_iter_status}");

                        // using an offset of 1 on the granted_time here because HELICS starts at t=1 and SAInt starts at t=0 
                        MappingFactory.PublishAvailableThermalPower(granted_time - 1, step, MappingList);

                        // get requested thermal power from connected gas plants, determine if there are violations
                        HasViolations = MappingFactory.SubscribeToRequiredThermalPower(granted_time - 1, step, MappingList);

                        if (step >= iter_max && HasViolations)
                        {
                            CurrentDiverged = new NotConverged() { timestep = e.TimeStep, itersteps = step, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)GNET.SCE.dt) };
                            notconverged.Add(CurrentDiverged);
                        }

                        e.RepeatTimeIntegration = HasViolations;
                        IsRepeating = HasViolations;

                    }

                }

            };

            // run gas model
            API.runGSIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime Drequested_time = GNET.SCE.EndTime + new TimeSpan(0, 0, (int)GNET.SCE.dt);
            Console.WriteLine($"Requested time step: {requested_time} at Time: {Drequested_time}");
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
                    sw.WriteLine("Date \t TimeStep \t IterStep");
                    foreach (TimeStepInfo x in timestepinfo)
                    {
                        sw.WriteLine(String.Format("{0}\t{1}\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }
            using (FileStream fs = new FileStream(outputfolder + "NotConverged_gas_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Date \t TimeStep \t IterStep");
                    foreach (NotConverged x in notconverged)
                    {
                        sw.WriteLine(String.Format("{0}\t{1}\t{2}", x.time, x.timestep, x.itersteps));
                    }
                }

            }

            // Diverging time steps
            if (notconverged.Count == 0)
                Console.WriteLine("Gas: There is no diverging time step");
            else
            {
                Console.WriteLine("Gas: the solution diverged at the following time steps:");
                foreach (NotConverged x in notconverged)
                { 
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}"); 
                }
                Console.WriteLine($"Gas: The total number of diverging time steps = { notconverged.Count }");
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
