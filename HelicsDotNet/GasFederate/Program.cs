using System;
using h = helics;
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
            string netfolder = @"..\..\..\..\Networks\DemoAlt_disruption\";
            string outputfolder = @"..\..\..\..\outputs\DemoAlt_disruption\";
            APIExport.openGNET(netfolder + "GNET25.net");
            APIExport.openGSCE(netfolder + "CASE1.sce");
            APIExport.openGCON(netfolder + "CMBSTEOPF.con");

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
            APIExport.showSIMLOG(true);
#else
            APIExport.showSIMLOG(false);
#endif

            // Load mapping between gas nodes and power plants 
            List<Mapping> MappingList = MappingFactory.GetMappingFromFile(netfolder + "Mapping.txt");
            
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
            foreach (Mapping m in MappingList) {
                m.GasPubPth= h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pth_" + m.GasNodeID, "double", "");
                m.GasPubPbar = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_Pbar_" + m.GasNodeID, "double", "");

                m.ElectricSub= h.helicsFederateRegisterSubscription(vfed, "PUB_" + m.ElectricGenID, "");
                
                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GasNode.Name + ".txt", FileMode.Create));
                m.sw.WriteLine("tstep \t iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW] ");
            }

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS timesteps based on scenario
            double total_time = SAInt.GNET.SCE.NN;
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

            Int16 step=0;
            List<TimeStepInfo> timestepinfo = new List<TimeStepInfo>();
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
            DateTime SCEStartTime = SAInt.GNET.SCE.StartTime;
            DateTime Trequested;
            DateTime Tgranted;
            TimeStepInfo currenttimestep = new TimeStepInfo() { timestep = 0, itersteps = 0, time = SCEStartTime };

            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {
                
                if (e.SolverState == SolverState.BeforeTimeStep) {

                    
                    // non-iterative time request here to block until both federates are done iterating
                    Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * SAInt.GNET.SCE.dT);
                    Console.WriteLine($"Requested time {Trequested}");
                    //Console.WriteLine($"Requested time {e.TimeStep}");

                    step = 0; // Iteration number

                    // HELICS time granted 
                    granted_time = h.helicsFederateRequestTime(vfed, e.TimeStep);
                    Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time-1) * SAInt.GNET.SCE.dT);
                    Console.WriteLine($"Granted time: {Tgranted}, SolverState: {e.SolverState}");
                    //Console.WriteLine($"Granted time: {granted_time}, SolverState: {e.SolverState}");

                    IsRepeating = !IsRepeating;
                    HasViolations = true;

                    foreach (Mapping m in MappingList)
                    {
                        m.lastVal.Clear();
                    }
                    // Publishing initial available thermal power of zero MW and zero peressure difference
                    MappingFactory.PublishAvailableThermalPower(granted_time - 1, step, MappingList);

                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * SAInt.GNET.SCE.dT) };
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
                        Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * SAInt.GNET.SCE.dT); ;
                        Console.WriteLine($"Requested time: {Trequested}, iteration: {step}");

                        // HELICS time granted 
                        granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, HelicsIterationRequest.HELICS_ITERATION_REQUEST_FORCE_ITERATION, out helics_iter_status);
                        Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time-1) * SAInt.GNET.SCE.dT);
                        Console.WriteLine($"Granted time: {Tgranted},  Iteration status: {helics_iter_status}");

                        // using an offset of 1 on the granted_time here because HELICS starts at t=1 and SAInt starts at t=0 
                        MappingFactory.PublishAvailableThermalPower(granted_time-1, step, MappingList);

                        // get requested thermal power from connected gas plants, determine if there are violations
                         HasViolations = MappingFactory.SubscribeToRequiredThermalPower(granted_time - 1, step, MappingList);
                        
                        e.RepeatTimeIntegration = HasViolations;
                        IsRepeating = HasViolations;                                                         
                    }

                }
                
            };

            // run gas model
            APIExport.runGSIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime Drequested_time = SAInt.GNET.SCE.EndTime + new TimeSpan(0, 0, SAInt.GNET.SCE.dT);
            Console.WriteLine($"Requested time step: {requested_time} at Time: {Drequested_time}");
            h.helicsFederateRequestTime(vfed, requested_time);

#if !DEBUG
            // close out log file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
#endif

            // save SAInt output
            APIExport.writeGSOL(netfolder + "gsolin.txt", outputfolder + "gsolout_HELICS.txt");

            // finalize federate
            h.helicsFederateFinalize(vfed);
            Console.WriteLine("Gas: Federate finalized");
            h.helicsFederateFree(vfed);

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
