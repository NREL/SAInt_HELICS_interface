using System;
using h = helics;
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
            // Load Electric Model - Demo - Normal Operation
            //string netfolder = @"..\..\..\..\Networks\Demo\";
            //string outputfolder = @"..\..\..\..\outputs\Demo\";
            //APIExport.openENET(netfolder + "ENET30.enet");
            //APIExport.openESCE(netfolder + "CASE1.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            // Load Electric Model - Demo_disruption - Compressor Outage
            //string netfolder = @"..\..\..\..\Networks\Demo_disruption\";
            //string outputfolder = @"..\..\..\..\outputs\Demo_disruption\";
            //APIExport.openENET(netfolder + "ENET30.enet");
            //APIExport.openESCE(netfolder + "CASE1.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            // Load Electric Model - DemoAlt - Normal Operation 
            //string netfolder = @"..\..\..\..\Networks\DemoAlt\";
            //string outputfolder = @"..\..\..\..\outputs\DemoAlt\";
            //APIExport.openENET(netfolder + "ENET30.enet");
            //APIExport.openESCE(netfolder + "CASE0.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            // Load Electric Model - DemoAlt_disruption - Compressor Outage
            string netfolder = @"..\..\..\..\Networks\DemoAlt_disruption\";
            string outputfolder = @"..\..\..\..\outputs\DemoAlt_disruption\";
            APIExport.openENET(netfolder + "ENET30.enet");
            APIExport.openESCE(netfolder + "CASE1.esce");
            APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            // Load Electric Model - Belgian model - Normal Operation
            //string netfolder = @"..\..\..\..\Networks\Belgium_Case0\";
            //string outputfolder = @"..\..\..\..\outputs\Belgium_Case0\";
            //APIExport.openENET(netfolder + "EnetBelgiumtest.enet");
            //APIExport.openESCE(netfolder + "QDYNOPF.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            // Load Electric Model - Belgian model - Compressor Outage
            //string netfolder = @"..\..\..\..\Networks\Belgium_Case1\";
            //string outputfolder = @"..\..\..\..\outputs\Belgium_Case1\";
            //APIExport.openENET(netfolder + "EnetBelgiumtest.enet");
            //APIExport.openESCE(netfolder + "QDYNOPF.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

            Directory.CreateDirectory(outputfolder);
#if DEBUG
            APIExport.showSIMLOG(true);
#else
            APIExport.showSIMLOG(false);
#endif
            // Load mapping between gas nodes and power plants 
            List<Mapping> MappingList = MappingFactory.GetMappingFromFile(netfolder + "Mapping.txt");

            // Get HELICS version
            Console.WriteLine($"Electric: Helics version ={h.helicsGetVersion()}");

            // Create broker 
            //int SeparateBroker = 0;
            //int SeparateBroker = 1;

            //if (SeparateBroker == 0)
            //{
            //    string initBrokerString = "-f 2 --name=mainbroker";
            //    Console.WriteLine("Creating Broker");
            //    var broker = h.helicsCreateBroker("tcp", "", initBrokerString);
            //    Console.WriteLine("Created Broker");

            //    Console.WriteLine("Checking if Broker is connected");
            //    int isconnected = h.helicsBrokerIsConnected(broker);
            //    Console.WriteLine("Checked if Broker is connected");
            //    if (isconnected == 1) Console.WriteLine("Broker created and connected");
            //}

            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Electric: Creating Federate Info");
            var fedinfo = h.helicsCreateFederateInfo();

            // Set core type from string
            Console.WriteLine("Electric: Setting Federate Core Type");
            h.helicsFederateInfoSetCoreName(fedinfo, "Electric Federate Core");
            h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "tcp");

            //If set to true, a federate will not be granted the requested time until all other federates have completed at least 1 iteration
            //of the current time or have moved past it.If it is known that 1 federate depends on others in a non-cyclic fashion, this
            //can be used to optimize the order of execution without iterating.
            //h.helicsFederateInfoSetFlagOption(fedinfo, (int)HelicsFederateFlags.HELICS_FLAG_WAIT_FOR_CURRENT_TIME_UPDATE, 1);

            // Federate init string
            Console.WriteLine("Electric: Setting Federate Info Init String");
            //string fedinitstring = "--broker=mainbroker --federates=1";
            string fedinitstring = "--federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            // Create value federate
            Console.WriteLine("Electric: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo);
            Console.WriteLine("Electric: Value federate created");

            // Register Publication and Subscription for coupling points
            foreach (Mapping m in MappingList)
            {
                m.ElectricPub = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.ElectricGenID, "double", "");
                m.GasSubPth = h.helicsFederateRegisterSubscription(vfed, "PUB_Pth_" + m.GasNodeID, "");
                m.GasSubPbar = h.helicsFederateRegisterSubscription(vfed, "PUB_Pbar_" + m.GasNodeID, "");
                
                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.ElectricGen.Name+".txt", FileMode.Create));
                m.sw.WriteLine("tstep \t iter \t PG[MW] \t ThPow [MW] \t PGMAX [MW]");
            }

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS timesteps based on scenario
            double total_time = SAInt.ENET.SCE.NN;
            Console.WriteLine($"Number of timesteps in scenario: {total_time}");

            double granted_time = 0;
            double requested_time;

            // set max iteration at 20
            h.helicsFederateSetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS, 20);
            int iter_max = h.helicsFederateGetIntegerProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_INT_MAX_ITERATIONS);
            Console.WriteLine($"Max iterations: {iter_max}");

            // start initialization mode
            //h.helicsFederateEnterInitializingMode(vfed);
            //Console.WriteLine("Electric: Entering initialization mode");

            // start execution mode
            h.helicsFederateEnterExecutingMode(vfed);
            Console.WriteLine("Electric: Entering execution mode");

            // variables to control iterations
            Int16 step=0 ;
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
            ostrm = new FileStream(outputfolder + "Log_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(ostrm);
            Console.SetOut(writer);
#endif
            DateTime SCEStartTime = SAInt.ENET.SCE.StartTime;
            DateTime Trequested;
            DateTime Tgranted;
            TimeStepInfo currenttimestep =new TimeStepInfo() {timestep = 0, itersteps = 0,time= SCEStartTime};
            NotConverged CurrentDiverged = new NotConverged();

            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {                     
#if DEBUG
                if (e.SolverState == SolverState.AfterTimeStep)
                {
                    foreach (var i in SAInt.ENET.Gen)
                    {
                        Console.WriteLine($"{i.Name} \t {i.get_PG(e.TimeStep)}");
                    }
                }
#endif
                if (e.SolverState == SolverState.BeforeTimeStep)
                {
                    // non-iterative time request here to block until both federates are done iterating the last time step
                    Trequested = SCEStartTime + new TimeSpan(0,0,e.TimeStep*SAInt.ENET.SCE.dT);
                    Console.WriteLine($"Requested time {Trequested}");
                    //Console.WriteLine($"Requested time {e.TimeStep}");
                    
                    step = 0;

                    // HELICS time granted 
                    granted_time = h.helicsFederateRequestTime(vfed, e.TimeStep);
                    Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time-1)*SAInt.ENET.SCE.dT);
                    Console.WriteLine($"Granted time: {Tgranted}, SolverState: {e.SolverState}");
                    //Console.WriteLine($"Granted time: {granted_time}, SolverState: {e.SolverState}");

                    IsRepeating = !IsRepeating;
                    HasViolations = true;

                    // Reset nameplate capacity
                    foreach (Mapping m in MappingList)
                    {
                        m.ElectricGen.PGMAX = m.NCAP;
                        m.ElectricGen.PGMIN = 0;
                        m.lastVal.Clear();
                    }
                    // Initital publication of thermal power request equivalent to PGMAX for time = 0 and iter = 0;
                    if (e.TimeStep == 0)
                    {
                        MappingFactory.PublishRequiredThermalPower(granted_time - 1, step, MappingList);
                    }
                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0,time= SCEStartTime + new TimeSpan(0,0,e.TimeStep*SAInt.ENET.SCE.dT)};
                    timestepinfo.Add(currenttimestep);
                }

                if (e.SolverState == SolverState.AfterTimeStep && IsRepeating)
                {
                    // stop iterating if max iterations have been reached
                    IsRepeating =  (step < iter_max); 

                    if (IsRepeating)
                    {
                        step += 1;
                        currenttimestep.itersteps += 1;

                        int helics_iter_status;

                        // iterative HELICS time request
                        Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * SAInt.ENET.SCE.dT);
                        Console.WriteLine($"Requested time: {Trequested}, iteration: {step}");                        

                        // HELICS time granted  
                        granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, HelicsIterationRequest.HELICS_ITERATION_REQUEST_FORCE_ITERATION, out helics_iter_status);
                        Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time-1) * SAInt.ENET.SCE.dT);
                        Console.WriteLine($"Granted time: {Tgranted},  Iteration status: {helics_iter_status}");

                        // Using an offset of 1 on the granted_time here because HELICS starts at t=1 and SAInt starts at t=0
                        MappingFactory.PublishRequiredThermalPower(granted_time-1, step, MappingList);

                        // get available thermal power at nodes, determine if there are violations                 
                        HasViolations = MappingFactory.SubscribeToAvailableThermalPower(granted_time-1, step, MappingList);

                        if (step == iter_max && HasViolations)
                        {
                            CurrentDiverged = new NotConverged() { timestep = e.TimeStep, itersteps = step, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * SAInt.ENET.SCE.dT) };
                            notconverged.Add(CurrentDiverged);
                        }

                        e.RepeatTimeIntegration = HasViolations;
                        IsRepeating = HasViolations;                      
                    }
                    
                }

                
            };

            // run power model
            APIExport.runESIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;            
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime Drequested_time = SAInt.ENET.SCE.EndTime + new TimeSpan(0, 0, SAInt.ENET.SCE.dT);
            Console.WriteLine($"Requested time step: {requested_time} at Time: {Drequested_time}");
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

            using (FileStream fs=new FileStream(outputfolder + "TimeStepInfo_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write)) 
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

            // save SAInt output
            APIExport.writeESOL(netfolder + "esolin.txt", outputfolder + "esolout_HELICS.txt");

           

            using (FileStream fs = new FileStream(outputfolder + "NotConverged_electric_federate.txt", FileMode.OpenOrCreate, FileAccess.Write))
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
                Console.WriteLine("\n Electric: There is no diverging time step.");
            else
            {
                Console.WriteLine("Electric: the solution diverged at the following time steps:");
                foreach (NotConverged x in notconverged)
                {
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}");
                }
                Console.WriteLine($"\n Electric: The total number of diverging time steps = { notconverged.Count }");
            }

            //if (SeparateBroker == 0)
            //{
            //    while (h.helicsBrokerIsConnected(broker) > 0) Thread.Sleep(1);
            //}

            foreach (Mapping m in MappingList)
            {
                if (m.sw != null)
                {
                    m.sw.Flush();
                    m.sw.Close();
                }
            }

            // disconnect broker
            //if (SeparateBroker == 0)
            //{
            //    h.helicsCloseLibrary();
            //    Console.WriteLine("Electric: Broker disconnected");
            //}
            
            var k = Console.ReadKey();
        }
    }
}