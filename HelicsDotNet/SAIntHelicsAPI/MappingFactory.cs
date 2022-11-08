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

        public static void PublishRequiredThermalPower(int etime, int Iter, List<ElectricGasMapping> MappingList)
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            DateTime Etime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                double GCV = m.GFG.get_GCV(etime) / 1e6; // in MJ/m3
                 //double pval0 = API.evalFloat(String.Format("FGEN.{0}.P.({1}).[MW]", m.GFG.FGENName, etime));                
                double pval = m.GFG.FGEN.get_P(etime);     

                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.RequieredThermalPower, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]\t P = {4:0.0000} [MW]\t  PGMAX = {5:0.0000} [MW]",
                    Etime, Iter, m.GFG.FGEN, ThermalPower, pval, m.GFG.FGEN.get_PMAX(etime)));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                    etime, Iter, pval, ThermalPower, m.GFG.FGEN.get_PMAX(etime), Etime));
            }
        }
        public static void PublishAvailableThermalPower(int gtime, int Iter, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");
            
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {                
                double pval = API.evalFloat(String.Format("GDEM.{0}.P.({1}).[bar-g]", m.GFG.GDEM.Name, gtime));
                double PMIN = API.evalFloat(String.Format("{0}.PMIN.({1}).[bar-g]", m.GFG.GDEM.NetNode, gtime));

                //double qval = API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime));
                //double qval01 = API.evalFloat(String.Format("GDEM.{0}.Q.({1}).[sm3/s]", m.GFG.GDEM.Name, gtime));
                double qval = m.GFG.GDEM.get_Q(gtime);                

                //double GCV =  API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));                
                double GCV = m.GFG.get_GCV(gtime) / 1e6; // in MJ/m3
                double qvalN15 = m.GFG.GDEM.get_Q(gtime);

                double ThermalPower  = qval * GCV; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.AvailableThermalPower, ThermalPower);
                //h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.PMIN(gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);
                h.helicsPublicationPublishDouble(m.PressureRelativeToPmin, pval - PMIN);

                Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t P {4:0.0000} [bar-g]\t Q {5:0.0000} [sm3/s]", 
                    Gtime, Iter, m.GFG.GDEM, ThermalPower, pval, qval));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}", 
                    gtime, Iter, pval, qval, ThermalPower, Gtime));
            }
        }

        public static bool SubscribeToAvailableThermalPower(int HorizonTimeStepStart, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            
            int HorizonTimeSteps = MappingList.First().HorizonTimeSteps;
            foreach (ElectricGasMapping m in MappingList)
            {
                int HorizonTimeStepEnd = HorizonTimeStepStart + HorizonTimeSteps - 1;
                for (int etime = HorizonTimeStepStart; etime <= HorizonTimeStepEnd; etime++)
                {
                    DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);
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
                            Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Gtime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));
                            continue;
                        }
                    }

                    Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Gtime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));


                    //get currently required thermal power 
                    //double pval = API.evalFloat(String.Format("{0}.P.({1}).[MW]", m.GFG.FGEN, etime));
                    double pval = m.GFG.FGEN.get_P(etime);
                    double HR(double x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
                    double FGENThermalPower = HR(pval) / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                    m.LastVal.Add(AvailableThermalPower);

                    if (Math.Abs(FGENThermalPower - AvailableThermalPower) > eps)
                    {
                        if ((valPbar < eps || Iter > 6) && (!m.IsFmaxChanged[etime]))
                        {
                            double PG = GetActivePowerFromAvailableThermalPower(m, AvailableThermalPower, pval);
                            double ThermalPower02 = HR(PG) / 3.6 * PG;
                            double FMAX_old = m.GFG.FGEN.Fuel.get_FMAX(etime);
                            double FMAX = m.GFG.FGEN.Fuel.get_FMAX(etime);                            
                            double GCV = m.GFG.get_GCV(etime) / 1e6;
                            int EventFuelMaxSet = 0;
                            foreach (ScenarioEvent evt in m.GFG.FGEN.Fuel.SceList)
                            {
                                EventFuelMaxSet += 1;
                                if (evt.ObjPar == CtrlType.FMAX)
                                {
                                    EventFuelMaxSet += 1;
                                    double EvtVal = evt.ObjVal;
                                    evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                                    evt.ShowVal = string.Format("{0}", AvailableThermalPower / GCV);
                                    evt.StartTime = Gtime;
                                    evt.Processed = false;
                                    evt.Active = true;

                                    Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t FMAX({1}) = {3:0.0000} [sm3/s]\t FMAX({5}) = {4:0.0000} [sm3/s]",
                                        Gtime, Iter, m.GFG.FGEN, evt.ObjVal, EvtVal, Iter - 1));
                                }

                            }
                            if (EventFuelMaxSet == 0)
                            {

                                double EvtVal = double.NaN;
                                SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                                ScenarioEvent evt = new ScenarioEvent(m.GFG.FGEN.Fuel, CtrlType.FuelMax, AvailableThermalPower / GCV, Unit)
                                {
                                    Processed = false,
                                    StartTime = Gtime,
                                    Active = true
                                };
                                m.GFG.GDEM.SceList.Add(evt);
                                Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET({1}) = {3:0.0000} [sm3/s]\t QSET({5}) = {4:0.0000} [sm3/s]",
                                        Gtime, Iter, m.GFG.GDEM, evt.ObjVal, EvtVal, Iter - 1));
                            }
                            m.IsFmaxChanged[etime] = true;        


                            Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PGMAX_new = {3:0.0000} [MW]\t  PGMAX_old = {4:0.0000} [MW]", Gtime, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX(), FMAX_old));
                        }
                        HasViolations = true;
                    }
                    else
                    {
                        if (m.LastVal.Count > 2)
                        {
                            //if ((Math.Abs(m.lastVal[m.lastVal.Count - 1] - m.lastVal[m.lastVal.Count - 2]) > eps) || (Math.Abs(m.lastVal[m.lastVal.Count - 2] - m.lastVal[m.lastVal.Count - 3]) > eps))
                            if (Math.Abs(m.LastVal[m.LastVal.Count - 1] - m.LastVal[m.LastVal.Count - 2]) > eps)
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

        public static bool SubscribeToRequiredThermalPower(int gtime, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            GNET = (GasNet)GetObject("get_GNET");
            bool HasViolations = false;
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, gtime * (int)GNET.SCE.dt);
            
            foreach (ElectricGasMapping m in MappingList)
            {
                // get publication from electric federate
                double val = h.helicsInputGetDouble(m.RequieredThermalPower);
                //Gtime = m.GFG.GNET.SCE.StartTime;

                if (Init == "Initialization")
                {
                    if (val < 0)
                    {
                        HasViolations = true;
                    }
                    if (HasViolations) break;
                    else
                    {
                        Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, val));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, val));

                m.LastVal.Add(val);

                //get currently available thermal power 
                double GCV = m.GFG.get_GasNQ(gtime).GCV / 1e6;

                //double pval = GCV*API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime));                   
                double pval = GCV * m.GFG.GDEM.get_Q(gtime);

                if (Math.Abs(pval - val) > eps )
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
                            evt.ShowVal = string.Format("{0}", val / GCV);
                            evt.StartTime = Gtime;
                            evt.Processed = false;
                            evt.Active = true;

                            Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET({1}) = {3:0.0000} [sm3/s]\t QSET({5}) = {4:0.0000} [sm3/s]",
                                Gtime, Iter, m.GFG.GDEM, evt.ObjVal, EvtVal, Iter-1));
                        }

                    }
                    if (EventQset == 0)
                    {

                        double EvtVal = double.NaN;
                        SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                        ScenarioEvent evt = new ScenarioEvent(m.GFG.GDEM, CtrlType.QSET, val / GCV, Unit)
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
                    if (m.LastVal.Count > 2)
                    {
                        if (Math.Abs(m.LastVal[m.LastVal.Count - 2] - m.LastVal[m.LastVal.Count - 1]) > eps)
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

        public static double GetActivePowerFromAvailableThermalPower(ElectricGasMapping m,double Pth, double initVal)
        {
            Func<double,double> GetHR = (x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
            Func<double, double> GetF = (x) => 3.6 * Pth - x * GetHR(x);
            Func<double, double> GetdFdx = (x) => -(m.GFG.FGEN.HR0 + 2*m.GFG.FGEN.HR1 * x + 3*m.GFG.FGEN.HR2 * x * x);

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
                
                var mapitem = new ElectricGasMapping();
                mapitem.GFG = hub;
                mapitem.LastVal = new List<double>();
                if (hub.GDEM != null)
                {
                    bool IsThereQMAXEvent = hub.GDEM.SceList.Any(evt => evt.ObjPar == CtrlType.QMAX);
                    for (int i = 0; i < hub.GDEM.GNET.SCE.NN; i++)
                    {
                        if (IsThereQMAXEvent)
                        {
                            mapitem.GasQmax[i] = hub.GDEM.get_QMAX(i);
                        }
                    }
                        
                }
                if (hub.FGEN != null)
                {
                    bool IsThereFMAXEvent = hub.FGEN.SceList.Any(evt => evt.ObjPar == CtrlType.FMAX);
                    for (int i = 0; i < hub.FGEN.ENET.SCE.NN; i++)
                    {
                        if (IsThereFMAXEvent)
                        {
                            mapitem.GenFuelMax[i] = hub.FGEN.Fuel.get_FMAX(i);
                        }
                    }
                   
                }

                for (int i = 1; i<= mapitem.HorizonTimeSteps; i++)
                {
                    mapitem.RequieredThermalPower02.Add(i, mapitem.EmptyPubSub);
                    mapitem.AvailableThermalPower02.Add(i, mapitem.EmptyPubSub);
                    mapitem.PressureRelativeToPmin02.Add(i, mapitem.EmptyPubSub);
                    
                    mapitem.LastVal02.Add(i, new List<double>());

                    mapitem.IsFmaxChanged[i] = false;                    
                }
                MappingList.Add(mapitem);
            }
            return MappingList;
        }

        public static void AccessFile(string FilePath)
        {
           API.openHUBS(FilePath);
            System.Threading.Thread.Sleep(1000);

        }
    }

  
    public class ElectricGasMapping
    {
        public GasFiredGenerator GFG;

        public double[] GenFuelMax = new double[] { };
       
        public double[] GasQmax = new double[] { };
        public bool[] IsFmaxChanged = new bool[] { };

        public int HorizonTimeSteps = 24;  // Used for federates having different time horizons 

        public List<double> LastVal = new List<double>();

        public Dictionary<int, List<double>> LastVal02 = new Dictionary<int, List<double>>();

        public Dictionary <int, SWIGTYPE_p_void> AvailableThermalPower02 = new Dictionary<int, SWIGTYPE_p_void> ();
        public Dictionary<int, SWIGTYPE_p_void> PressureRelativeToPmin02 = new Dictionary<int, SWIGTYPE_p_void>();
        public Dictionary<int, SWIGTYPE_p_void> RequieredThermalPower02 = new Dictionary<int, SWIGTYPE_p_void>();

        public SWIGTYPE_p_void EmptyPubSub;
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
