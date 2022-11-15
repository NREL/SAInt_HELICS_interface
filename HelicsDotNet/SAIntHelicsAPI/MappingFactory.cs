using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SAInt_API;
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

        public static HubSystem HUB { get; set; }

        //public static CombinedSystem ENET { get; set; }
        public static ElectricNet ENET { get; set; }

        //public static CombinedSystem GNET { get; set; }
        public static GasNet GNET { get; set; }
        public static FuelGenerator FGEN { get; set; }
        static object GetObject(string funcName)
        {
            var func = typeof(API).GetMethod(funcName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return func.Invoke(null, new object[] { });
        }

        public static StreamWriter gasSw;
        public static StreamWriter elecSw;
        public static double eps = 0.1;

        public static void PublishRequiredFuelRate(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList)
        {
            ENET = (ElectricNet)GetObject("get_ENET");

            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            int etime;

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < HorizonTimeSteps; i++)
                {
                    etime = i + HorizonTimeStepStart;
                    DateTime Etime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);               
                    double Pval = m.GFG.FGEN.get_P(etime);

                    double RequieredFuelRate = (m.GFG.FGEN.FC0 + m.GFG.FGEN.FC1 * Pval + m.GFG.FGEN.FC2 * Pval * Pval)/3600; // in m3/s                  
                    double RequieredFuelRate02 = m.GFG.FGEN.get_F(etime) / 3600; //in m3/s

                    h.helicsPublicationPublishDouble(m.RequieredFuelRate[i], RequieredFuelRate);

                    Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\t FuelRate = {3:0.0000} [m3/s]\t P = {4:0.0000} [MW]\t  PGMAX = {5:0.0000} [MW]",
                        Etime, Iter, m.GFG.FGEN, RequieredFuelRate, Pval, m.GFG.FGEN.get_PMAX(etime)));
                    m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                        etime, Iter, Pval, RequieredFuelRate, m.GFG.FGEN.get_PMAX(etime), Etime));
                }
            }
        }
        public static void PublishAvailableFuelRate(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");

            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            int gtime;

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < HorizonTimeSteps; i++)
                {
                    gtime = i + HorizonTimeStepStart;
                    DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);

                    double Pval = API.evalFloat(String.Format("GDEM.{0}.P.({1}).[bar-g]", m.GFG.GDEM.Name, gtime));
                    double PMIN = API.evalFloat(String.Format("{0}.PMIN.({1}).[bar-g]", m.GFG.GDEM.NetNode, gtime));
                 
                    double AvailableFuelRate = m.GFG.GDEM.get_Q(gtime); // in sm3/s
                    
                    h.helicsPublicationPublishDouble(m.AvailableFuelRate[i], AvailableFuelRate);

                    //h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.PMIN(gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);
                    h.helicsPublicationPublishDouble(m.PressureRelativeToPmin[i], Pval - PMIN);

                    Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t Q = {3:0.0000} [sm3/s]\t P {4:0.0000} [bar-g]",
                        Gtime, Iter, m.GFG.GDEM, AvailableFuelRate, Pval));
                    m.sw.WriteLine(String.Format("{3}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {4:0.00000}",
                        gtime, Iter, Pval, Gtime, AvailableFuelRate));
                }
            }
        }

        public static bool SubscribeToAvailableFuelRate(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            
            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            int etime;

            SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);

            foreach (ElectricGasMapping m in MappingList)
            {                
                for (int i = 0; i < HorizonTimeSteps; i++)
                {
                    etime = HorizonTimeStepStart + i;
                    DateTime Etime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);
                    // subscribe to available thermal power from gas node
                    double AvailableFuelRate = h.helicsInputGetDouble(m.AvailableFuelRate[i]);

                    // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                    double valPbar = h.helicsInputGetDouble(m.PressureRelativeToPmin[i]);

                    if (Init == "Initialization")
                    {
                        if ((AvailableFuelRate < 0) | (valPbar < 0))
                        {
                            HasViolations = true;
                        }
                        if (HasViolations) break;
                        else
                        {
                            Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t FuelRate = {3:0.0000} [m3/s]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableFuelRate, valPbar));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t FuelRate = {3:0.0000} [m3/s]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableFuelRate, valPbar));

                    //get currently required thermal power 
                    //double pval = API.evalFloat(String.Format("{0}.P.({1}).[MW]", m.GFG.FGEN, etime));
                    double PGval = m.GFG.FGEN.get_P(etime);
                    double FuelRate(double x) => m.GFG.FGEN.FC0 + m.GFG.FGEN.FC1 * x + m.GFG.FGEN.FC2 * x * x;  // in m3/h                  
                    double RequieredFuelRate = m.GFG.FGEN.Fuel.get_F(etime)/3600; // in m3/s
                    double RequieredFuelRate02 = FuelRate(PGval)/3600; // in m3/s

                    m.LastVal[i].Add(AvailableFuelRate);

                    if (Math.Abs(RequieredFuelRate - AvailableFuelRate) > eps)
                    {
                        if ((valPbar < eps || Iter > 5) && (!m.IsPmaxChanged[etime]))
                        {
                            double PG = GetActivePowerFromAvailableFuelRate(m, AvailableFuelRate, PGval);
                            double PGMAX_old = m.GFG.FGEN.get_PMAX(etime);
                            double PGMAX = Math.Max(m.GFG.FGEN.get_PMIN(etime), PG);                            

                            foreach (var evt in m.GFG.FGEN.SceList.Where(xx => xx.ObjPar == CtrlType.PMAX && xx.StartTime == Etime))
                            {
                                evt.Unit = Unit;
                                evt.ShowVal = string.Format("{0}", PGMAX);
                                evt.Processed = false;
                                evt.StartTime = Etime;
                                evt.Active = true;
                                evt.Info = "HELICS";
                            };

                            m.IsPmaxChanged[etime] = false; // true if we want to set it only once.
                            Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PMAX = {3:0.0000} [MW]",
                                Etime, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX(etime)));                            
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

            Console.WriteLine($"Electric HasViolations?: {HasViolations}");
            return HasViolations;
        }

        public static bool SubscribeToRequiredFuelRate(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            GNET = (GasNet)GetObject("get_GNET");

            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;

            bool HasViolations = false;

            SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);

            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i = 0; i < HorizonTimeSteps; i++)
                {
                    int gtime = HorizonTimeStepStart + i;
                    DateTime Gtime = GNET.SCE.dTime[gtime];// + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);

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
                            Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t RequieredFuelRate = {3:0.0000} [m3/s]", Gtime, Iter, m.GFG.GDEM, RequieredFuelRate));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t RequieredFuelRate = {3:0.0000} [m3/s]", Gtime, Iter, m.GFG.GDEM, RequieredFuelRate));

                    m.LastVal[i].Add(RequieredFuelRate);
                                   
                    double AvailableFuelRate = m.GFG.GDEM.get_Q(gtime);

                    if (Math.Abs(AvailableFuelRate - RequieredFuelRate) > eps)
                    {
                        foreach (var evt in m.GFG.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET && xx.StartTime == Gtime))
                        {
                            evt.Unit = Unit;
                            evt.ShowVal = string.Format("{0}", RequieredFuelRate);
                            evt.StartTime = Gtime;
                            evt.Processed = false;
                            evt.Active = true;
                            evt.Info = "HELICS";
                        }

                        //foreach (var evt in m.GFG.GNET.SCE.SceList.Where(xx => xx.NetObject == m.GFG.GDEM && xx.ObjPar == CtrlType.QSET && xx.StartTime == Gtime))
                        //{
                        //    GNET.SCE.RemoveEvent(evt);
                        //}
                        //ScenarioEvent QsetEvent = new ScenarioEvent(m.GFG.GDEM, CtrlType.QSET, RequieredThermalPower, Unit)
                        //{
                        //    Processed = false,
                        //    StartTime = Gtime,
                        //    Active = true,
                        //    Info = "HELICS"
                        //};
                        //m.GFG.GNET.SCE.AddEvent(QsetEvent);

                        Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET = {3:0.0000} [sm3/s]",
                            Gtime, Iter, m.GFG.GDEM, m.GFG.GDEM.get_QSET(gtime)));

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

        public static double GetActivePowerFromAvailableFuelRate(ElectricGasMapping m,double AvailableFuelRate, double initVal)
        {
            double GetFuelRate (double x) => m.GFG.FGEN.FC0 + m.GFG.FGEN.FC1 * x + m.GFG.FGEN.FC2 * x * x;
            double GetF (double x) => 3600 * AvailableFuelRate - GetFuelRate(x);
            double GetdFdx (double x)  => -(m.GFG.FGEN.FC1 * x + 2*m.GFG.FGEN.FC2 * x);

            double Res = Math.Abs(GetF(initVal));
            int maxiter = 30;
            int i=0;
            double p=initVal;

            while (i<maxiter)
            { if (GetdFdx(p) != 0)
                {
                    p -= GetF(p) / GetdFdx(p);
                }
                else
                {
                    p -= 0.0001;
                }                

                Res =Math.Abs(GetF(p));

                if (Res < 1e-10)
                {
                    return p;
                }

                i+=1;
            }

            return p;
        }

        public static List<ElectricGasMapping> GetMappingFromHubs(IList<GasFiredGenerator> GFGs)
        {
            List<ElectricGasMapping> MappingList = new List<ElectricGasMapping>();

            foreach (GasFiredGenerator hub in GFGs)
            {
                
                var mapitem = new ElectricGasMapping() { GFG = hub };

                if (hub.GDEM != null)
                {
                    for (int gtime = 0; gtime <= hub.GDEM.GNET.SCE.NN; gtime++)
                    {
                        mapitem.GasQset.Add(hub.GDEM.get_QSET(gtime));
                    }
                }

                if (hub.FGEN != null)
                {
                    for (int etime = 0; etime <= hub.FGEN.ENET.SCE.NN; etime++)
                    {
                        mapitem.ElecPmax.Add(hub.FGEN.get_PMAX(etime));

                        mapitem.IsPmaxChanged.Add(false);
                    }
                }

                for (int i = 0; i< mapitem.HorizonTimeSteps; i++)
                {
                    mapitem.RequieredFuelRate.Add(i, mapitem.EmptyPubSub);
                    mapitem.AvailableFuelRate.Add(i, mapitem.EmptyPubSub);
                    mapitem.PressureRelativeToPmin.Add(i, mapitem.EmptyPubSub);
                    mapitem.LastVal.Add(i, new List<double>());
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

        public List<double> ElecPmax = new List<double>();
        public List<bool> IsPmaxChanged = new List<bool>();

        public List<double> GasQset = new List<double>();

        public int HorizonTimeSteps = 4;  // Used for federates having different time horizons 

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
        public int timestep, itersteps;
        public DateTime time;
    }
}
