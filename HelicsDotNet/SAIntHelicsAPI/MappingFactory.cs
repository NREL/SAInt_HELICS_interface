using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using h = helics;
using SAInt_API;
using SAInt_API.Library.Units;
using SAInt_API.Model;
using SAInt_API.Model.Scenarios;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model.Network.Fluid.Gas;

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
 
        // Convergence criteria
        public static double eps = 0.001;

        // Iteration to start to curtail FGEN
        static int CurtailmentIterStart = 4;
        static int CurtailmentIter;

        public static void PublishRequiredThermalPower(int kstep, int Iter, List<ElectricGasMapping> MappingList)
        {
            foreach (ElectricGasMapping m in MappingList)
            {
                double GCV = m.GFG.get_GCV(kstep) / 1e6; // in MJ/m3                
                double PGval = m.GFG.FGEN.get_P(kstep);     

                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * PGval + m.GFG.FGEN.HR2 * PGval * PGval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR / 3.6 * PGval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.RequieredThermalPower, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]\t P = {4:0.0000} [MW]\t  PGMAX = {5:0.0000} [MW]",
                    m.GFG.ENET.SCE.dTime[kstep], Iter, m.GFG.FGEN, ThermalPower, PGval, m.GFG.FGEN.get_PMAX(kstep)));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                    kstep, Iter, PGval, ThermalPower, m.GFG.FGEN.get_PMAX(kstep), m.GFG.ENET.SCE.dTime[kstep]));
            }
        }
        public static void PublishAvailableThermalPower(int kstep, int Iter, List<ElectricGasMapping> MappingList)
        {
            foreach (ElectricGasMapping m in MappingList)
            {                
                GasNode GNODE = (GasNode)m.GFG.GDEM.NetNode;
                double Pressure = (GNODE.get_P(kstep) ) / 1e5; // in bar
                double MinPressure = GNODE.get_PMIN(kstep) / 1e5; // in bar
                double GCV = m.GFG.get_GCV(kstep) / 1e6; // in MJ/m3    
                
                double GasOfftake = m.GFG.GDEM.get_Q(kstep);
                double ThermalPower = GasOfftake * GCV;            

                //double ThermalPower  = GasOfftake * GCV; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.AvailableThermalPower, ThermalPower);
                h.helicsPublicationPublishDouble(m.PressureRelativeToPmin, Pressure - MinPressure);

                Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t P {4:0.0000} [bar]\t Q {5:0.0000} [sm3/s]",
                    m.GFG.GNET.SCE.dTime[kstep], Iter, m.GFG.GDEM, ThermalPower, Pressure, GasOfftake));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}", 
                    kstep, Iter, Pressure, GasOfftake, ThermalPower, m.GFG.GNET.SCE.dTime[kstep]));
            }
        }

        public static bool SubscribeToAvailableThermalPower(int kstep, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {             
            bool HasViolations = false;
            bool AnyCurtailment = false;
            if (Iter == 0) CurtailmentIter = CurtailmentIterStart;

            foreach (ElectricGasMapping m in MappingList)
            {
                DateTime DateTimeStep = m.GFG.ENET.SCE.dTime[kstep];

                // subscribe to available thermal power from gas node
                double AvailableThermalPower = h.helicsInputGetDouble(m.AvailableThermalPower);

                // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                double valPbar = h.helicsInputGetDouble(m.PressureRelativeToPmin);

                if (Init == "Initialization")
                {
                    if ((AvailableThermalPower < 0) | (valPbar < 0))
                    { 
                        HasViolations = true;
                    }
                    if (HasViolations) break;
                    else
                    {
                        Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar]", DateTimeStep, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar]", DateTimeStep, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));

                //get currently required thermal power 
                double pval = m.GFG.FGEN.get_P(kstep);
                double HR (double x)=> m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
                double ThermalPower = HR(pval) / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                m.lastVal.Add(AvailableThermalPower);

                if (Math.Abs(ThermalPower-AvailableThermalPower) > eps)
                {
                    if ((valPbar < eps || Iter > CurtailmentIter) && (!m.IsPmaxChanged))
                    {
                        AnyCurtailment = true;
                        
                        double PG = GetActivePowerFromAvailableThermalPower(m, AvailableThermalPower, pval);
                        double ThermalPower02 = HR(PG) / 3.6 * PG;
                        double PGMAX = Math.Max(m.GFG.FGEN.get_PMIN(kstep), PG);                        

                        Units Unit = new Units(UnitTypeList.PPOW, UnitList.MW);

                        foreach (var evt in m.GFG.FGEN.SceList.Where(xx => xx.ObjPar == CtrlType.PMAX && xx.StartTime == DateTimeStep))
                        {
                            evt.ObjVal = PGMAX;
                            evt.Unit = Unit;
                            evt.Processed = false;
                            evt.Active = true;
                            evt.Info = "HELICS";
                        }

                        m.IsPmaxChanged = true; // To avoid oscillation
                        Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PMAX = {3:0.0000} [MW]",
                                    DateTimeStep, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX(kstep)));
                    }
                    HasViolations = true;
                }
                else
                {
                    int Count = m.lastVal.Count;
                    if (Count > 2)
                    {
                        if ((Math.Abs(m.lastVal[Count - 2] - m.lastVal[Count - 1]) > eps) || (Math.Abs(m.lastVal[Count - 3] - m.lastVal[Count - 2]) > eps))
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
            // Make sure the effect of the current curtailment is communicated before the next round
            if (AnyCurtailment)
            {
                CurtailmentIter += 3;
            }

            Console.WriteLine($"Electric HasViolations?: {HasViolations}");
            return HasViolations;
        }

        public static bool SubscribeToRequiredThermalPower(int kstep, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            bool HasViolations = false;
            
            foreach (ElectricGasMapping m in MappingList)
            {
                DateTime DateTimeStep = m.GFG.GNET.SCE.dTime[kstep];
                // get publication from electric federate
                double RequieredThermalPower = h.helicsInputGetDouble(m.RequieredThermalPower);
                //Gtime = m.GFG.GNET.SCE.StartTime;

                if (Init == "Initialization")
                {
                    if (RequieredThermalPower < 0)
                    {
                        HasViolations = true;
                    }
                    if (HasViolations) break;
                    else
                    {
                        Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", DateTimeStep, Iter, m.GFG.GDEM, RequieredThermalPower));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", DateTimeStep, Iter, m.GFG.GDEM, RequieredThermalPower));

                m.lastVal.Add(RequieredThermalPower);

                //get currently available thermal power 
                double GCV = m.GFG.get_GasNQ(kstep).GCV / 1e6;

                double pval = API.evalFloat(String.Format("{0}.Q.({1}).[MW]", m.GFG.GDEM, kstep));                   
                double AvailableThermalPower = GCV * m.GFG.GDEM.get_Q(kstep);

                if (Math.Abs(AvailableThermalPower - RequieredThermalPower) > eps )
                {                    
                    
                    Units Unit = new Units(UnitTypeList.Q, UnitList.sm3_s);

                    foreach (var evt in m.GFG.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET)) // && xx.StartTime == Gtime
                    {
                        evt.ObjVal = RequieredThermalPower / GCV;
                        evt.Unit = Unit;
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
                    int Count = m.lastVal.Count;
                    if (Count > 2)
                    {
                        if ((Math.Abs(m.lastVal[Count - 2] - m.lastVal[Count - 1]) > eps) || (Math.Abs(m.lastVal[Count - 3] - m.lastVal[Count - 2])>eps))
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
            Console.WriteLine($"Gas HasViolations?: {HasViolations}");
            return HasViolations;
        }

        public static double GetActivePowerFromAvailableThermalPower(ElectricGasMapping m, double Pth, double InitVal)
        {
            double GetHR (double x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
            double Get_dF (double x) => 3.6 * Pth - x * GetHR(x);
            double GetdF_by_dx (double x) => -(m.GFG.FGEN.HR0 + 2*m.GFG.FGEN.HR1 * x + 3*m.GFG.FGEN.HR2 * x * x);

            int maxiter = 30;
            int i=0;
            double ActivePower = InitVal;
            double Residual;

            while (i<maxiter)
            {
                Residual = Math.Abs(Get_dF(ActivePower));
                if (Residual < 1e-6)
                {
                    return ActivePower;
                }
                else if (GetdF_by_dx(ActivePower) != 0)
                {
                    ActivePower -= Get_dF(ActivePower) / GetdF_by_dx(ActivePower);
                }
                else
                {
                    ActivePower -= 0.0001;
                }  
                i+=1;
            }

            return ActivePower;
        }

        public static List<ElectricGasMapping> GetMappingFromHubs(IList<GasFiredGenerator> GFGs)
        {
            List<ElectricGasMapping> MappingList = new List<ElectricGasMapping>();

            foreach (GasFiredGenerator GFG in GFGs)
            {                
                var mapitem = new ElectricGasMapping();

                mapitem.GFG = GFG;

                if (GFG.GDEM != null)
                {
                    Units Unit = new Units(UnitTypeList.Q, UnitList.sm3_s);

                    // Initial QSET event values
                    for (int kstep = 0; kstep <= GFG.GDEM.GNET.SCE.NN; kstep++)
                    {
                        DateTime DateTimeStep = GFG.GNET.SCE.dTime[kstep];
                        bool QsetEventExist = GFG.GDEM.SceList.Any(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == DateTimeStep);
                        if (QsetEventExist)
                        {
                            foreach (var evt in GFG.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == DateTimeStep))
                            {
                                evt.Unit = Unit;
                                evt.ShowVal = string.Format("{0}", GFG.GDEM.get_QSET(kstep));
                                evt.Processed = false;
                                evt.Active = true;
                            }
                        }
                        else
                        {
                            ScenarioEvent QsetEvent = new ScenarioEvent(GFG.GDEM, CtrlType.QSET, GFG.GDEM.get_QSET(kstep), Unit)
                            {
                                Processed = false,
                                StartTime = DateTimeStep,
                                Active = true
                            };
                            GFG.GNET.SCE.AddEvent(QsetEvent);
                        }
                    }
                }

                if (GFG.FGEN != null)
                {
                    Units Unit = new Units(UnitTypeList.PPOW, UnitList.MW);

                    for (int kstep = 0; kstep <= GFG.FGEN.ENET.SCE.NN; kstep++)
                    {
                        DateTime DateTimeStep = GFG.ENET.SCE.dTime[kstep];
                        // Initial PMAX event values
                        bool IsTherePmaxEvent = GFG.FGEN.SceList.Any(xx => xx.ObjPar == CtrlType.PMAX && xx.StartTime == DateTimeStep);
                        
                        if (IsTherePmaxEvent)
                        {
                            foreach (var evt in GFG.FGEN.SceList.Where(xx => xx.ObjPar == CtrlType.PMAX && xx.StartTime == DateTimeStep))
                            {
                                evt.Unit = Unit;
                                evt.ShowVal = string.Format("{0}", GFG.FGEN.get_PMAX(kstep));
                                evt.Processed = false;
                                evt.Active = true;
                            }
                        }
                        else
                        {
                            ScenarioEvent PmaxEvent = new ScenarioEvent(GFG.FGEN, CtrlType.PMAX, GFG.FGEN.get_PMAX(kstep), Unit)
                            {
                                Processed = false,
                                StartTime = DateTimeStep,
                                Active = true
                            };
                            GFG.ENET.SCE.AddEvent(PmaxEvent);
                        }
                    }                    
                }

                MappingList.Add(mapitem);
            }
            return MappingList;
        }

        public static void AccessFile(string FilePath)
        {
            API.openHUBS(FilePath);
            System.Threading.Thread.Sleep(100);
        }
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }

  
    public class ElectricGasMapping
    {
        public GasFiredGenerator GFG;
        public bool IsPmaxChanged = false;

        public List<double> lastVal = new List<double>();

        public SWIGTYPE_p_void AvailableThermalPower;
        public SWIGTYPE_p_void PressureRelativeToPmin;
        public SWIGTYPE_p_void RequieredThermalPower;

        public StreamWriter sw;
    }

    public class TimeStepInfo
    {
        public int timestep, itersteps;
        public DateTime time;
    }
}
