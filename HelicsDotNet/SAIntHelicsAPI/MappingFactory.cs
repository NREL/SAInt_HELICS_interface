using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
//using s = SAInt_API.SAInt;
using SAInt_API;
using SAInt_API.Library.Units;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model.Network.Electric;
using SAInt_API.Model.Network.Fluid.Gas;

using SAInt_API.Model;
using SAInt_API.Model.Scenarios;

using h = helics;
using System.Net.Sockets;
using System.Linq;

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
        public static double eps = 0.001;

        public static void PublishRequiredThermalPower(int etime, int Iter, List<ElectricGasMapping> MappingList)
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, etime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                double GCV = m.GFG.get_GCV(etime) / 1e6; // in MJ/m3
                 //double pval0 = API.evalFloat(String.Format("FGEN.{0}.P.({1}).[MW]", m.GFG.FGENName,egtime));                
                double PGval = m.GFG.FGEN.get_P(etime);     

                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * PGval + m.GFG.FGEN.HR2 * PGval * PGval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR / 3.6 * PGval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.RequieredThermalPower, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]\t P = {4:0.0000} [MW]\t  PGMAX = {5:0.0000} [MW]",
                    Gtime, Iter, m.GFG.FGEN, ThermalPower, PGval, m.GFG.FGEN.get_PMAX(etime)));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}",
                    etime, Iter, PGval, ThermalPower, m.GFG.FGEN.get_PMAX(etime), Gtime));
            }
        }
        public static void PublishAvailableThermalPower(int gtime, int Iter, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");
            
            DateTime Gtime = GNET.SCE.dTime[gtime];

            foreach (ElectricGasMapping m in MappingList)
            {                
                double Pressure = API.evalFloat(String.Format("GDEM.{0}.P.({1}).[bar-g]", m.GFG.GDEM.Name, gtime));
                double MinPressure = API.evalFloat(String.Format("{0}.PMIN.({1}).[bar-g]", m.GFG.GDEM.NetNode, gtime));
                double GCV = m.GFG.get_GCV(gtime) / 1e6; // in MJ/m3
                
                double ThermalPower = API.evalFloat(String.Format("{0}.Q.({1}).[MW]", m.GFG.GDEM, gtime));
                double qval02 = API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime));
                double GasOfftake = m.GFG.GDEM.get_Q(gtime);  

                //double ThermalPower  = GasOfftake * GCV; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.AvailableThermalPower, ThermalPower);
                //h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.PMIN(gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);
                h.helicsPublicationPublishDouble(m.PressureRelativeToPmin, Pressure - MinPressure);

                Console.WriteLine(String.Format("Gas-S: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t P {4:0.0000} [bar-g]\t Q {5:0.0000} [sm3/s]", 
                    Gtime, Iter, m.GFG.GDEM, ThermalPower, Pressure, GasOfftake));
                m.sw.WriteLine(String.Format("{5}\t\t{0}\t\t\t{1}\t\t {2:0.00000} \t {3:0.00000} \t {4:0.00000}", 
                    gtime, Iter, Pressure, GasOfftake, ThermalPower, Gtime));
            }
        }

        public static bool SubscribeToAvailableThermalPower(int etime, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            DateTime Etime = ENET.SCE.dTime[etime];

            foreach (ElectricGasMapping m in MappingList)
            {
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
                        Console.WriteLine(String.Format("Electric-R: Initialization Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Electric-R: Time {0}\t iter {1}\t {2}\t Pthg = {3:0.0000} [MW]\t dPr = {4:0.0000} [bar-g]", Etime, Iter, m.GFG.FGEN, AvailableThermalPower, valPbar));


                //get currently required thermal power 
                //double pval = API.evalFloat(String.Format("{0}.P.({1}).[MW]", m.GFG.FGEN, etime));
                double pval = m.GFG.FGEN.get_P(etime);
                double HR (double x)=> m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
                double ThermalPower = HR(pval) / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                Units FperTUnit = m.GFG.FGEN.Fuel.FuelPerTimeUnit;
                double FuelRate = m.GFG.FGEN.get_F(etime); //[(s)m3/h]

                m.lastVal.Add(AvailableThermalPower);

                if (Math.Abs(ThermalPower-AvailableThermalPower) > eps)
                {
                    if ((valPbar < eps || Iter > 4) && (!m.IsPmaxChanged))
                    {
                        double PG = GetActivePowerFromAvailableThermalPower(m, AvailableThermalPower, pval);
                        double ThermalPower02 = HR(PG) / 3.6 * PG;
                        double PGMAX = Math.Max(m.GFG.FGEN.get_PMIN(etime), PG);                        

                        SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);

                        foreach (var evt in m.GFG.FGEN.SceList.Where(xx => xx.ObjPar == CtrlType.PMAX && xx.StartTime == Etime))
                        {
                            if (evt.ObjPar == CtrlType.PMAX)
                            {
                                evt.ObjVal = PGMAX;
                                evt.Unit = Unit;
                                evt.Processed = false;
                                evt.Active = true;
                                evt.Info = "HELICS";
                            }
                        }

                        m.IsPmaxChanged = true; // To avoid oscillation
                        Console.WriteLine(String.Format("Electric-E: Time {0}\t iter {1}\t {2}\t PMAX = {3:0.0000} [MW]",
                                    Etime, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX(etime)));
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
                        Console.WriteLine(String.Format("Gas-R: Initialization Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, RequieredThermalPower));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Gas-R: Time {0}\t iter {1}\t {2}\t Pthe = {3:0.0000} [MW]", Gtime, Iter, m.GFG.GDEM, RequieredThermalPower));

                m.lastVal.Add(RequieredThermalPower);

                //get currently available thermal power 
                double GCV = m.GFG.get_GasNQ(gtime).GCV / 1e6;

                double pval = API.evalFloat(String.Format("{0}.Q.({1}).[MW]", m.GFG.GDEM, gtime));                   
                double AvailableThermalPower = GCV * m.GFG.GDEM.get_Q(gtime);

                if (Math.Abs(AvailableThermalPower - RequieredThermalPower) > eps )
                {                    
                    
                    SAInt_API.Library.Units.Units Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);

                    foreach (var evt in m.GFG.GDEM.SceList.Where(xx => xx.ObjPar == CtrlType.QSET)) // && xx.StartTime == Gtime
                    {
                        evt.ObjVal = RequieredThermalPower / GCV;
                        evt.Unit = Unit;
                        evt.Processed = false;
                        evt.Active = true;
                        evt.Info = "HELICS";
                    }
                    Console.WriteLine(String.Format("Gas-E: Time {0}\t iter {1}\t {2}\t QSET = {3:0.0000} [sm3/s]",
                        Gtime, Iter, m.GFG.GDEM, m.GFG.GDEM.get_QSET(gtime)));

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

        public static double GetActivePowerFromAvailableThermalPower(ElectricGasMapping m, double Pth, double initVal)
        {
            double GetHR (double x) => m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * x + m.GFG.FGEN.HR2 * x * x;
            double Get_dF (double x) => 3.6 * Pth - x * GetHR(x);
            double GetdF_by_dx (double x) => -(m.GFG.FGEN.HR0 + 2*m.GFG.FGEN.HR1 * x + 3*m.GFG.FGEN.HR2 * x * x);

            int maxiter = 30;
            int i=0;
            double p = initVal;
            double Residual;

            while (i<maxiter)
            {
                Residual = Math.Abs(Get_dF(p));
                if (Residual < 1e-10)
                {
                    return p;
                }
                else if (GetdF_by_dx(p) != 0)
                {
                    p -= Get_dF(p) / GetdF_by_dx(p);
                }
                else
                {
                    p -= 0.0001;
                }  
                i+=1;
            }

            return p;
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
                    for (int gtime = 0; gtime <= GFG.GDEM.GNET.SCE.NN; gtime++)
                    {
                        //
                        mapitem.GasQset.Add(GFG.GDEM.get_QSET(gtime));
                    }
                }

                if (GFG.FGEN != null)
                {
                    for (int etime = 0; etime <= GFG.FGEN.ENET.SCE.NN; etime++)
                    {
                        //
                        mapitem.ElecPmax.Add(GFG.FGEN.get_PMAX(etime));
                    }                    
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

        public List<double> ElecPmax = new List<double>();
        public List<double> GasQset = new List<double>();
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
