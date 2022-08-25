﻿using System;
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
            // Load Electric Model - 2 node case
            //string netfolder = @"C:\Getnet Files\HELICS Projects\Gas Fired Generator\";
            //string outputfolder = @"C:\Getnet Files\HELICS Projects\Gas Fired Generator\outputs\2Node\";
            //APIExport.openENET(netfolder + "GasFiredGenerator.enet");
            //APIExport.openHUBS(netfolder + "GasFiredGenerator.hubs");
            //APIExport.openESCE(netfolder + "QDYN_ACOPF_PMAX_PMAXPRC.esce");
            //APIExport.openECON(netfolder + "QDYN_ACOPF_PMAX_PMAXPRC.econ");
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
            //string netfolder = @"..\..\..\..\Networks\DemoAlt_disruption\";
            //string outputfolder = @"..\..\..\..\outputs\DemoAlt_disruption\";
            //APIExport.openENET(netfolder + "ENET30.enet");
            //APIExport.openESCE(netfolder + "CASE1.esce");
            //APIExport.openECON(netfolder + "CMBSTEOPF.econ");

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
            API.showSIMLOG(true);
#else
            APIExport.showSIMLOG(false);
#endif
            
            // Create Federate Info object that describes the federate properties
            Console.WriteLine("Electric: Creating Federate Info");
            var fedinfo = helics.helicsCreateFederateInfo();

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
            string fedinitstring = "--federates=1";
            h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring);

            // Create value federate
            Console.WriteLine("Electric: Creating Value Federate");
            var vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo);
            Console.WriteLine("Electric: Value federate created");

            // Load the mapping between the gas demands and the gas fiered power plants 
            List<ElectricGasMapping> MappingList = MappingFactory.GetMappingFromHubs(HUB.GasFiredGenerators);

            // Register Publication and Subscription for coupling points
            foreach (ElectricGasMapping m in MappingList)
            {
                m.ElectricPub = h.helicsFederateRegisterGlobalTypePublication(vfed, "PUB_" + m.GFG.FGENName, "double", "");
                m.GasSubPth = h.helicsFederateRegisterSubscription(vfed, "PUB_Pth_" + m.GFG.GDEMName, "");
                m.GasSubPbar = h.helicsFederateRegisterSubscription(vfed, "PUB_Pbar_" + m.GFG.GDEMName, "");
                m.GasSubQ_sm3s = h.helicsFederateRegisterSubscription(vfed, "PUB_Qmax_" + m.GFG.GDEMName, "");

                //Streamwriter for writing iteration results into file
                m.sw = new StreamWriter(new FileStream(outputfolder + m.GFG.FGENName + ".txt", FileMode.Create));
                m.sw.WriteLine("tstep \t iter \t PG[MW] \t ThPow [MW] \t PGMAX [MW]");
            }

            // Register Publication and Subscription for iteration synchronisation
            SWIGTYPE_p_void ElecPubIter = h.helicsFederateRegisterGlobalTypePublication(vfed, "ElectricIter", "double", "");
            SWIGTYPE_p_void ElecSub_GasIter = h.helicsFederateRegisterSubscription(vfed, "GasIter", "");

            // Set one second message interval
            double period = 1;
            Console.WriteLine("Electric: Setting Federate Timing");
            h.helicsFederateSetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD, period);

            // check to make sure setting the time property worked
            double period_set = h.helicsFederateGetTimeProperty(vfed, (int)HelicsProperties.HELICS_PROPERTY_TIME_PERIOD);
            Console.WriteLine($"Time period: {period_set}");

            // set number of HELICS timesteps based on scenario
            double total_time = ENET.SCE.NN;
            Console.WriteLine($"Number of timesteps in scenario: {total_time}");

            double granted_time = 0;
            //double granted_iteration = 0;
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
            DateTime SCEStartTime = ENET.SCE.StartTime;
            DateTime Trequested;
            DateTime Tgranted;
            TimeStepInfo currenttimestep =new TimeStepInfo() {timestep = 0, itersteps = 0,time= SCEStartTime};
            NotConverged CurrentDiverged = new NotConverged();
            //double GasIter=-1;

            // this function is called each time the SAInt solver state changes
            Solver.SolverStateChanged += (object sender, SolverStateChangedEventArgs e) =>
            {                     
#if DEBUG
                if (e.SolverState == SolverState.AfterTimeStep)
                {
                    foreach (var i in ENET.Generators)
                    {
                        Console.WriteLine($"{i.Name} \t {i.get_P(e.TimeStep)}");
                    }
                }
#endif
                if (e.SolverState == SolverState.BeforeTimeStep)
                {
                    // non-iterative time request here to block until both federates are done iterating the last time step
                    Trequested = SCEStartTime + new TimeSpan(0,0,e.TimeStep*(int)ENET.SCE.dt);
                    Console.WriteLine($"Requested time {Trequested}");
                    //Console.WriteLine($"Requested time {e.TimeStep}");
                    
                    step = 0;

                    // HELICS time granted 
                    granted_time = h.helicsFederateRequestTime(vfed, e.TimeStep);
                    Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time-1)*(int)ENET.SCE.dt);
                    Console.WriteLine($"Granted time: {Tgranted}, SolverState: {e.SolverState}");
                    //Console.WriteLine($"Granted time: {granted_time}, SolverState: {e.SolverState}");
                                        
                    IsRepeating = !IsRepeating;
                    HasViolations = true;

                    // Reset nameplate capacity
                    foreach (ElectricGasMapping m in MappingList)
                    {
                       
                        foreach (var evt in m.GFG.FGEN.SceList)
                        {

                            if (evt.ObjPar == CtrlType.PMIN)
                            {
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                                evt.ShowVal = string.Format("{0}", m.NCAP);
                                evt.Processed = false;
                                
                            }
                            if (evt.ObjPar == CtrlType.PMAX)
                            {
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                                evt.ShowVal = string.Format("{0}", 0);
                                evt.Processed = false;

                            }
                        }

                        m.lastVal.Clear();
                    }
                    // Set time step info
                    currenttimestep = new TimeStepInfo() { timestep = e.TimeStep, itersteps = 0,time= SCEStartTime + new TimeSpan(0,0,e.TimeStep*(int)ENET.SCE.dt)};
                    timestepinfo.Add(currenttimestep);
                }

                if ( e.SolverState == SolverState.AfterTimeStep && IsRepeating)
                {
                    // stop iterating if max iterations have been reached
                    IsRepeating =  (step < iter_max); 

                    if (IsRepeating)
                    {                      
                        step += 1;

                        currenttimestep.itersteps += 1;

                        int helics_iter_status;

                        // Wait for the gas federate to publish the outputs for the current time step
                        //while (step > 1 && GasIter != step)
                        //{
                        //    // HELICS time granted 
                        //    granted_time = h.helicsFederateRequestTimeIterative(vfed, step, HelicsIterationRequest.HELICS_ITERATION_REQUEST_FORCE_ITERATION, out helics_iter_status);
                        //    GasIter = h.helicsInputGetDouble(ElecSub_GasIter);
                        //}

                        //// iterative HELICS time request
                        Trequested = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)ENET.SCE.dt);
                        Console.WriteLine($"Requested time: {Trequested}, iteration: {step}");

                        granted_time = h.helicsFederateRequestTimeIterative(vfed, e.TimeStep, HelicsIterationRequest.HELICS_ITERATION_REQUEST_FORCE_ITERATION, out helics_iter_status);
                        Tgranted = SCEStartTime + new TimeSpan(0, 0, (int)(granted_time - 1) * (int)ENET.SCE.dt);
                        Console.WriteLine($"Granted Time: {Tgranted},  Iteration status: {helics_iter_status}");

                        // Using an offset of 1 on the granted_time here because HELICS starts at t=1 and SAInt starts at t=0
                        MappingFactory.PublishRequiredThermalPower(granted_time-1, step, MappingList);

                        //Iteration synchronization: Electric federate publishes its current iteration
                        h.helicsPublicationPublishDouble(ElecPubIter, step);

                        // get available thermal power at nodes, determine if there are violations
                        if (step > 1)
                        {
                            HasViolations = MappingFactory.SubscribeToAvailableThermalPower(granted_time - 1, step, MappingList);
                        }

                        if (step == iter_max && HasViolations)
                        {
                            CurrentDiverged = new NotConverged() { timestep = e.TimeStep, itersteps = step, time = SCEStartTime + new TimeSpan(0, 0, e.TimeStep * (int)ENET.SCE.dt) };
                            notconverged.Add(CurrentDiverged);
                        }

                        e.RepeatTimeIntegration = HasViolations;
                        IsRepeating = HasViolations;                      
                    }
                    
                }
                
            };

            // run power model
            API.runESIM();

            // request time for end of time + 1: serves as a blocking call until all federates are complete
            requested_time = total_time + 1;            
            //Console.WriteLine($"Requested time: {requested_time}");
            DateTime Drequested_time = ENET.SCE.EndTime + new TimeSpan(0, 0, (int)ENET.SCE.dt);
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
            h.helicsCloseLibrary();

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
            API.writeESOL(netfolder + "esolin.txt", outputfolder + "esolout_HELICS.txt");

           

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
                Console.WriteLine("Electric: There is no diverging time step");
            else
            {
                Console.WriteLine("Electric: the solution diverged at the following time steps:");
                foreach (NotConverged x in notconverged)
                {
                    Console.WriteLine($"Time \t {x.time} time-step {x.timestep}");
                }
                Console.WriteLine($"Electric: The total number of diverging time steps = { notconverged.Count }");
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