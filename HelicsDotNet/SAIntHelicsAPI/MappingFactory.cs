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

        public static void PublishRequiredThermalPower(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList)
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

                    double GCV = m.GFG.get_GCV(etime) / 1e6; // in MJ/m3
                                                             //double pval0 = API.evalFloat(String.Format("FGEN.{0}.P.({1}).[MW]", m.GFG.FGENName, etime));                
                    double pval = m.GFG.FGEN.get_P(etime);

                    double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                    // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                    double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]

                    h.helicsPublicationPublishDouble(m.RequieredThermalPower[i], ThermalPower);

                    Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]\t P = {4:0.0000} [MW]\t  PGMAX = {5:0.0000} [MW]",
                        Etime, Iter, m.GFG.FGEN, ThermalPower, pval, m.GFG.FGEN.get_PMAX(etime)));
                    m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                        etime, Iter, pval, ThermalPower, m.GFG.FGEN.get_PMAX(etime), Etime));
                }
            }
        }
        public static void PublishAvailableThermalPower(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList)
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

                    double pval = API.evalFloat(String.Format("GDEM.{0}.P.({1}).[bar-g]", m.GFG.GDEM.Name, gtime));
                    double PMIN = API.evalFloat(String.Format("{0}.PMIN.({1}).[bar-g]", m.GFG.GDEM.NetNode, gtime));

                    //double qval = API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime));
                    //double qval01 = API.evalFloat(String.Format("GDEM.{0}.Q.({1}).[sm3/s]", m.GFG.GDEM.Name, gtime));
                    double qval = m.GFG.GDEM.get_Q(gtime);

                    //double GCV =  API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));                
                    double GCV = m.GFG.get_GCV(gtime) / 1e6; // in MJ/m3
                    double qvalN15 = m.GFG.GDEM.get_Q(gtime);

                    double ThermalPower = qval * GCV; //Thermal power in [MW]
                    h.helicsPublicationPublishDouble(m.AvailableThermalPower[i], ThermalPower);
                    //h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.PMIN(gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);
                    h.helicsPublicationPublishDouble(m.PressureRelativeToPmin[i], pval - PMIN);

                    Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t P {4:0.0000} [bar-g]\t Q {5:0.0000} [sm3/s]",
                        Gtime, Iter, m.GFG.GDEM, ThermalPower, pval, qval));
                    m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                        gtime, Iter, pval, qval, ThermalPower, Gtime));
                }
            }
        }

        public static bool SubscribeToAvailableThermalPower(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            
            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            int etime;

            foreach (ElectricGasMapping m in MappingList)
            {                
                for (int i = 0; i < HorizonTimeSteps; i++)
                {
                    etime = HorizonTimeStepStart + i;
                    DateTime Etime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);
                    // subscribe to available thermal power from gas node
                    double AvailableThermalPower = h.helicsInputGetDouble(m.AvailableThermalPower[i]);

                    // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                    double valPbar = h.helicsInputGetDouble(m.PressureRelativeToPmin[i]);

                    if (Init == "Initialization")
                    {
                        if ((AvailableThermalPower < 0) | (valPbar < 0))
                        {
                            HasViolations = true;
                        }
                        if (HasViolations) break;
                        else
                        {
                            Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));

                    //get currently required thermal power 
                    //double pval = API.evalFloat(String.Format("{0}.P.({1}).[MW]", m.GFG.FGEN, etime));
                    double PGval = m.GFG.FGEN.get_P(etime);
                    double HR(double x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
                    double FGENThermalPower = HR(PGval) / 3.6 * PGval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                    m.LastVal[i].Add(AvailableThermalPower);

                    if (Math.Abs(FGENThermalPower - AvailableThermalPower) > eps)
                    {
                        if ((valPbar < eps || Iter > 6) && (!m.IsPmaxChanged[etime]))
                        {
                            double PG = GetActivePowerFromAvailableThermalPower(m, AvailableThermalPower, PGval);
                            double ThermalPower02 = HR(PG) / 3.6 * PG;
                            double PGMAX_old = m.GFG.FGEN.get_PMAX(etime);
                            double PGMAX = Math.Max(m.GFG.FGEN.get_PMIN(etime), PG);
                            SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                            ScenarioEvent PmaxEvent = new ScenarioEvent(m.GFG.FGEN, CtrlType.PMAX, PGMAX, Unit)
                            {
                                Processed = false,
                                StartTime = Etime,
                                Active = true,
                                Info = "HELICS"
                            };
                            double NewPmaxEventVal = PmaxEvent.ObjVal;
                            m.GFG.FGEN.SceList.Add(PmaxEvent);
                            m.GFG.ENET.SCE.SceList.Add(PmaxEvent);

                            m.IsPmaxChanged[etime] = false;
                            Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PMAX = {3:0.0000} [MW]",
                                Etime, Iter, m.GFG.FGEN, NewPmaxEventVal));
                            
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

        public static bool SubscribeToRequiredThermalPower(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            GNET = (GasNet)GetObject("get_GNET");

            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            int gtime;

            bool HasViolations = false;
            foreach (ElectricGasMapping m in MappingList)
            {
                for (int i=0; i < HorizonTimeSteps; i++)
                {
                    gtime = HorizonTimeStepStart + i;
                    DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);

                    // get publication from electric federate
                    double RequieredThermalPower = h.helicsInputGetDouble(m.RequieredThermalPower[i]);

                    if (Init == "Initialization")
                    {
                        if (RequieredThermalPower < 0)
                        {
                            HasViolations = true;
                        }
                        if (HasViolations) break;
                        else
                        {
                            Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, RequieredThermalPower));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, RequieredThermalPower));

                    m.LastVal[i].Add(RequieredThermalPower);

                    //get currently available thermal power 
                    double GCV = m.GFG.get_GasNQ(gtime).GCV / 1e6;

                    //double pval = GCV*API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime));                   
                    double pval = GCV * m.GFG.GDEM.get_Q(gtime);

                    if (Math.Abs(pval - RequieredThermalPower) > eps)
                    {
                        int EventQset = 0;
                        foreach (ScenarioEvent evt in m.GFG.GDEM.SceList)
                        {
                            EventQset += 1;
                            if (evt.ObjPar == CtrlType.QSET)
                            {
                                EventQset += 1;
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                                evt.ShowVal = string.Format("{0}", RequieredThermalPower / GCV);
                                evt.StartTime = Gtime;
                                evt.Processed = false;
                                evt.Active = true;

                                Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET({1}) = {3:0.0000} [sm3/s]\t QSET({5}) = {4:0.0000} [sm3/s]",
                                    Gtime, Iter, m.GFG.GDEM, evt.ObjVal, EvtVal, Iter - 1));
                            }

                        }
                        if (EventQset == 0)
                        {

                            double EvtVal = double.NaN;
                            SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                            ScenarioEvent evt = new ScenarioEvent(m.GFG.GDEM, CtrlType.QSET, RequieredThermalPower / GCV, Unit)
                            {
                                Processed = false,
                                StartTime = Gtime,
                                Active = true
                            };
                            m.GFG.GDEM.SceList.Add(evt);
                            Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET({1}) = {3:0.0000} [sm3/s]\t QSET({5}) = {4:0.0000} [sm3/s]",
                                    Gtime, Iter, m.GFG.GDEM, evt.ObjVal, EvtVal, Iter - 1));
                        }
                        HasViolations = true;
                    }
                    else
                    {
                        int Count = m.LastVal[i].Count;
                        if (Count > 2)
                        {
                            if (Math.Abs(m.LastVal[i][Count - 2] - m.LastVal[i][Count - 1]) > eps)
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

        public static double GetActivePowerFromAvailableThermalPower(ElectricGasMapping m,double Pth, double initVal)
        {
            double GetHR (double x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
            double GetF (double x) => 3.6 * Pth - x * GetHR(x);
            double GetdFdx (double x)  => -(m.GFG.FGEN.HR0 + 2*m.GFG.FGEN.HR1 * x + 3*m.GFG.FGEN.HR2 * x * x);

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
                    mapitem.RequieredThermalPower.Add(i, mapitem.EmptyPubSub);
                    mapitem.AvailableThermalPower.Add(i, mapitem.EmptyPubSub);
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

        public Dictionary <int, SWIGTYPE_p_void> AvailableThermalPower = new Dictionary<int, SWIGTYPE_p_void> ();
        public Dictionary<int, SWIGTYPE_p_void> PressureRelativeToPmin = new Dictionary<int, SWIGTYPE_p_void>();
        public Dictionary<int, SWIGTYPE_p_void> RequieredThermalPower = new Dictionary<int, SWIGTYPE_p_void>();

        public SWIGTYPE_p_void EmptyPubSub;

        public StreamWriter sw;
    }

    public class TimeStepInfo
    {
        public int timestep, itersteps;
        public DateTime time;
    }
}
