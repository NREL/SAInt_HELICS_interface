﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SAInt_API;
using SAInt_API.Library;
using SAInt_API.Library.Units;
using SAInt_API.Model.Network;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model.Network.Electric;
using SAInt_API.Model.Network.Fluid.Gas;

using SAInt_API.Model;
using SAInt_API.Model.Scenarios;

using h = helics;
using System.Net.Sockets;
using System.Net;

namespace SAIntHelicsLib
{

    public static class MappingFactory
    {
        #region Server client communication to open hub file without conflict
        private const string serverIP = "127.0.0.1";
        private const Int32 port = 13000;
        public static void WaitForAcknowledge()
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                IPAddress localAddr = IPAddress.Parse(serverIP);

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                        // Process the data sent by the client.
                        data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                        // Send back a response.
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);
                    }

                    // Shutdown and end connection
                    client.Close();
                    break;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }
        public static void SendAcknowledge()
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                
                TcpClient client = new TcpClient(serverIP, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes("go");

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", "go");

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

        }
#endregion
        public static double eps = 0.001;

        // Iteration to start to curtail FGEN
        static int CurtailmentIterStart = 2;
        static int CurtailmentIter;

        public static void PublishRequiredFuelRate(int HorizonStartingTimeStep, int Iter, List<ElectricGasMapping> MappingList)
        {
            int Horizon = MappingList.First().Horizon;
            int kstep;

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < Horizon; i++)
                {
                    kstep = i + HorizonStartingTimeStep;
                    DateTime DateTimeStep = m.GFG.ENET.SCE.dTime[kstep];               
                    double ActivePower = m.GFG.FGEN.get_P(kstep);
                    double RequieredFuelRate = m.GFG.FGEN.get_F(kstep) / 3600; //in m3/s

                    h.helicsPublicationPublishDouble(m.RequieredFuelRate[i], RequieredFuelRate);

                    Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\tFuelRateRequested = {3:0.000} [m3/s]\tP = {4:0.000} [MW]\t PGMAX = {5:0.000} [MW]",
                        DateTimeStep, Iter, m.GFG.FGEN, RequieredFuelRate, ActivePower, m.GFG.FGEN.get_PMAX(kstep)));
                    m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t\t{4:0.00000} \t{6:0.00000}",
                        kstep, Iter, ActivePower, RequieredFuelRate, m.GFG.FGEN.get_PMAX(kstep), DateTimeStep, m.GFG.FGEN.Fuel.get_FMAX(kstep)/3600));
                }
            }
        }
        public static void PublishAvailableFuelRate(int HorizonStartingTimeStep, int Iter, List<ElectricGasMapping> MappingList)
        {
            int Horizon = MappingList.First().Horizon;
            int kstep;

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < Horizon; i++)
                {
                    kstep = i + HorizonStartingTimeStep;
                    DateTime DateTimeStep = m.GFG.GNET.SCE.dTime[kstep];

                    GasNode GNODE = (GasNode)m.GFG.GDEM.NetNode;
                    double Pressure = (GNODE.get_P(kstep)) / 1e5; // in bar
                    double MinPressure = GNODE.get_PMIN(kstep) / 1e5; // in bar

                    double AvailableFuelRate = m.GFG.GDEM.get_Q(kstep); // in sm3/s
                    
                    h.helicsPublicationPublishDouble(m.AvailableFuelRate[i], AvailableFuelRate);

                    h.helicsPublicationPublishDouble(m.PressureRelativeToPmin[i], Pressure - MinPressure);

                    Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t FuelRateAvailable = {3:0.000} [sm3/s]\t QSET = {3:0.000} [sm3/s]\t P {5:0.000} [bar]",
                        DateTimeStep, Iter, m.GFG.GDEM, AvailableFuelRate, m.GFG.GDEM.get_QSET(kstep), Pressure));
                    m.sw.WriteLine(String.Format("{3}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {4:0.00000}",
                        kstep, Iter, Pressure, DateTimeStep, AvailableFuelRate));
                }
            }
        }

        public static bool SubscribeToAvailableFuelRate(int HorizonStartingTimeStep, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            bool HasViolations = false;            
            int Horizon = MappingList.First().Horizon;
            int kstep;

            bool AnyCurtailment = false;
            if (Iter == 0) CurtailmentIter = CurtailmentIterStart;

            Units Unit = new Units(UnitTypeList.FUELVOLDOT, UnitList.m3_h);

            foreach (ElectricGasMapping m in MappingList)
            {                
                for (int i = 0; i < Horizon; i++)
                {
                    kstep = HorizonStartingTimeStep + i;
                    DateTime DateTimeStep = m.GFG.ENET.SCE.dTime[kstep]; ;
                    // subscribe to available thermal power from gas node
                    double AvailableFuelRate = h.helicsInputGetDouble(m.AvailableFuelRate[i]);

                    // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                    double valPbar = h.helicsInputGetDouble(m.PressureRelativeToPmin[i]);

                    if (Init == "Initialization")
                    {
                        if ((AvailableFuelRate < 0))
                        {
                            HasViolations = true;
                        }
                        if (HasViolations) break;
                        else
                        {
                            Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t FuelRateAvailable = {3:0.0000} [m3/s]\t dPr = {4:0.0000} [bar]", DateTimeStep, Iter, m.GFG.FGEN, AvailableFuelRate, valPbar));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t FuelRateAvailable = {3:0.0000} [m3/s]\t dPr = {4:0.0000} [bar]", DateTimeStep, Iter, m.GFG.FGEN, AvailableFuelRate, valPbar));

                    //get currently required thermal power 
                    double ActivePower = m.GFG.FGEN.get_P(kstep);                                    
                    double RequieredFuelRate = m.GFG.FGEN.get_F(kstep)/3600; // in m3/s
                    //double PGMAX;

                    m.LastVal[i].Add(AvailableFuelRate);

                    if (Math.Abs(RequieredFuelRate - AvailableFuelRate) > eps)
                    {
                        if ((valPbar < eps || Iter > CurtailmentIter) && (!m.IsFmaxChanged[kstep]))
                        {
                            AnyCurtailment = true;

                            foreach (var evt in m.GFG.FGEN.Fuel.SceList.Where(xx => xx.ObjPar == CtrlType.FMAX && xx.StartTime == DateTimeStep))
                            {
                                evt.Unit = Unit;
                                evt.ObjVal = AvailableFuelRate*3600;
                                evt.Processed = false;
                                evt.StartTime = DateTimeStep;
                                evt.Active = true;
                                evt.Info = "HELICS";
                            };
                            m.IsFmaxChanged[kstep] = true; // true if we want to set it only once.

                            Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PMAXset = {3:0.0000} [MW]",
                                DateTimeStep, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX(kstep)));                            
                        }
                        HasViolations = true;
                    }
                    else
                    { int Count = m.LastVal[i].Count;
                        if (Count > 2)
                        {                            
                            if ((Math.Abs(m.LastVal[i][Count - 1] - m.LastVal[i][Count - 2]) > eps) || (Math.Abs(m.LastVal[i][Count - 2] - m.LastVal[i][Count - 3]) > eps))
                            {
                                HasViolations = true;
                            }
                        }
                        else
                        {
                            HasViolations = true;
                        }
                    }
                }
            }
            // Make sure the effect of the current curtailment is communicated before the next round
            if (AnyCurtailment)
            {
                CurtailmentIter += 2;
            }

            Console.WriteLine($"Electric HasViolations?: {HasViolations}");
            return HasViolations;
        }

        public static bool SubscribeToRequiredFuelRate(int HorizonStartingTimeStep, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            int Horizon = MappingList.First().Horizon;

            bool HasViolations = false;

            Units Unit = new Units(UnitTypeList.Q, UnitList.sm3_s);

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < Horizon; i++)
                {
                    int kstep = HorizonStartingTimeStep + i;
                    DateTime DateTimeStep = m.GFG.GNET.SCE.dTime[kstep];// + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);

                    // get publication from electric federate
                    double RequieredFuelRate = h.helicsInputGetDouble(m.RequieredFuelRate[i]);

                    if (Init == "Initialization")
                    {
                        if (RequieredFuelRate < 0)
                        {
                            HasViolations = true;
                        }
                        if (HasViolations) return HasViolations;
                        else
                        {
                            Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t RequieredFuelRate = {3:0.0000} [m3/s]", DateTimeStep, Iter, m.GFG.GDEM, RequieredFuelRate));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t RequieredFuelRate = {3:0.0000} [m3/s]", DateTimeStep, Iter, m.GFG.GDEM, RequieredFuelRate));

                    m.LastVal[i].Add(RequieredFuelRate);
                                   
                    double AvailableFuelRate = m.GFG.GDEM.get_Q(kstep);

                    if (Math.Abs(AvailableFuelRate - RequieredFuelRate) > eps)
                    {
                        foreach (var evt in m.GFG.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == DateTimeStep))
                        {
                            evt.Unit = Unit;
                            evt.ObjVal = RequieredFuelRate;
                            evt.StartTime = DateTimeStep;
                            evt.Processed = false;
                            evt.Active = true;
                            evt.Info = "HELICS";
                        }

                        Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET = {3:0.0000} [sm3/s]",
                            DateTimeStep, Iter, m.GFG.GDEM, m.GFG.GDEM.get_QSET(kstep)));

                        HasViolations = true;
                    }
                    else
                    {
                        int Count = m.LastVal[i].Count;
                        if (Count > 2)
                        {
                            if ((Math.Abs(m.LastVal[i][Count - 2] - m.LastVal[i][Count - 1]) > eps) || (Math.Abs(m.LastVal[i][Count - 3] - m.LastVal[i][Count - 2]) > eps))
                            {
                                HasViolations = true;
                            }
                        }
                        else
                        {
                            HasViolations = true;
                        }
                    }
                }
            }
            Console.WriteLine($"Gas HasViolations?: {HasViolations}");
            return HasViolations;
        }
    

        public static List<ElectricGasMapping> GetMappingFromHubs(IList<GasFiredGenerator> GFGs)
        {
            List<ElectricGasMapping> MappingList = new List<ElectricGasMapping>();

            foreach (GasFiredGenerator hub in GFGs)
            {
                var mapitem = new ElectricGasMapping() { GFG = hub };

                if (hub.GDEM != null)
                {
                    Units Unit = new Units(UnitTypeList.Q, UnitList.sm3_s);

                    // Initialize events for each time step before simulation
                    for (int kstep = 0; kstep <= hub.GNET.SCE.NN; kstep++)
                    {
                        DateTime DateTimeStep = hub.GNET.SCE.dTime[kstep];

                        bool IsThereQsetEvent = hub.GDEM.SceList.Any(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == DateTimeStep);
                        if (IsThereQsetEvent)
                        {
                            foreach (var evt in hub.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == DateTimeStep))
                            {
                                evt.Unit = Unit;
                                evt.ObjVal = hub.GDEM.get_QSET(kstep);
                                evt.Processed = false;
                                evt.Active = true;
                            }
                        }
                        else
                        {
                            ScenarioEvent QsetEvent = new ScenarioEvent(hub.GDEM, CtrlType.QSET, hub.GDEM.get_QSET(kstep), Unit)
                            {
                                Processed = false,
                                StartTime = DateTimeStep,
                                Active = true
                            };
                            hub.GNET.SCE.AddEvent(QsetEvent);
                        }
                    }
                }

                if (hub.FGEN != null)
                {
                    double FMAX;

                    Units Unit = new Units(UnitTypeList.FUELVOLDOT, UnitList.m3_h);

                    for (int kstep = 0; kstep <= hub.ENET.SCE.NN; kstep++)
                    {
                        DateTime DateTimeStep = hub.ENET.SCE.dTime[kstep];

                        if (hub.FGEN.Fuel.get_FMAX(kstep) == double.PositiveInfinity)
                        {
                            FMAX = 1e9;
                        }
                        else FMAX = hub.FGEN.Fuel.get_FMAX(kstep);

                        bool IsThereFMAXEvent = hub.FGEN.Fuel.SceList.Any(xx => xx.ObjPar == CtrlType.FMAX && xx.StartTime == DateTimeStep);

                        if (IsThereFMAXEvent)
                        {
                            foreach (var evt in hub.FGEN.Fuel.SceList.Where(xx => xx.ObjPar == CtrlType.FMAX && xx.StartTime == DateTimeStep))
                            {
                                evt.Unit = Unit;
                                evt.ObjVal = FMAX;
                                evt.Processed = false;
                                evt.Active = true;
                            }
                        }
                        else
                        {
                            ScenarioEvent evt = new ScenarioEvent
                            {
                                NetObject= hub.FGEN.Fuel,
                                ObjPar = CtrlType.FMAX,
                                ObjVal = FMAX,
                                Unit= Unit,
                                Processed = false,
                                StartTime = DateTimeStep,
                                Active = true
                            };
                            hub.ENET.SCE.AddEvent(evt);
                        }

                        mapitem.IsFmaxChanged.Add(false);                        
                    }
                }

                MappingList.Add(mapitem);
            }
            return MappingList;
        }

        public static void AccessFile(string FilePath)
        {
           API.openHUBS(FilePath);
            System.Threading.Thread.Sleep(10);
        }       
    }

  
    public class ElectricGasMapping
    {
        public GasFiredGenerator GFG;

        public List<bool> IsFmaxChanged = new List<bool>();

        public int Horizon;  // Used for federates having different time horizons 

        //public List<double> LastVal = new List<double>();

        public Dictionary<int, List<double>> LastVal = new Dictionary<int, List<double>>();

        public Dictionary <int, SWIGTYPE_p_void> AvailableFuelRate = new Dictionary<int, SWIGTYPE_p_void> ();
        public Dictionary<int, SWIGTYPE_p_void> PressureRelativeToPmin = new Dictionary<int, SWIGTYPE_p_void>();
        public Dictionary<int, SWIGTYPE_p_void> RequieredFuelRate = new Dictionary<int, SWIGTYPE_p_void>();

        public SWIGTYPE_p_void EmptyPubSub;

        public StreamWriter sw;
    }

    public class TimeStepInfo
    {
        public int HorizonStep, IterationCount;
        public DateTime time;
    }
}
