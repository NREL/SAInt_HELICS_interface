using System;
using System.Collections.Generic;
using System.IO;
//using s = SAInt_API.SAInt;
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

        public static void PublishRequiredThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
       {
            ENET = (ElectricNet)GetObject("get_ENET");
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                double pval;
                // Set initial publication of thermal power request equivalent to PGMAX for time = 0 and iter = 0;
                if (gtime == 0 && step == 0)
                {
                    pval = m.GFG.FGEN.get_PMAX();
                }

                else
                {                                
                    //pval = API.evalFloat(String.Format("ENO.{0}.P.({1}).[MW]", m.GFG.FGENName, gtime * m.GFG.FGEN.Net.SCE.dt / 3600));
                    pval = m.GFG.FGEN.get_P((int)(gtime * m.GFG.FGEN.Net.SCE.dt / 3600));
                }

                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR/3.6 * pval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.ElectricPubPthe, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW] \t P = {4:0.0000} [MW] \t  PGMAX = {5:0.0000} [MW]", 
                    Gtime,step, m.GFG.FGEN, ThermalPower,pval,m.GFG.FGEN.get_PMAX()));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    gtime,step, pval, ThermalPower , m.GFG.FGEN.get_PMAX()));
            }
        }
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
        public static void PublishAvailableThermalPower(double gtime, int Iter, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");
            
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                //double pva3 = API.evalFloat(String.Format("{0}.P.[sm3/s]", m.GasNode));
                //double pval = API.evalFloat(String.Format("{0}.P.({1}).[bar-g]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double pval = m.GFG.GDEM.get_P((int)(gtime * m.GFG.GDEM.Net.SCE.dt / 3600));

                //double qval = API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double qval = m.GFG.GDEM.get_Q((int)(gtime * m.GFG.GDEM.Net.SCE.dt / 3600));

                //double GCV =  API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));                
                double GCV = m.GFG.get_GCV((int)(gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double qvalN15 = m.GFG.GDEM.get_Q((int)gtime);

                double ThermalPower  = qval * GCV; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.GasPubPth, ThermalPower);
                h.helicsPublicationPublishDouble(m.GasPubQ_sm3s, qval);
                h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.GNET.get_PMIN((int)gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);

                Console.WriteLine(String.Format("Gas-S: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t P {4:0.0000} [bar-g] \t Q {5:0.0000} [sm3/s]", 
                    Gtime, Iter, m.GFG.GDEM, ThermalPower, pval, qval));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    Gtime, Iter, pval, qval, ThermalPower));
            }
        }

        public static bool SubscribeToAvailableThermalPower(double gtime, int Iter, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // subscribe to available thermal power from gas node
                double valPth = h.helicsInputGetDouble(m.ElecSubPthg);

                // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                double valPbar = h.helicsInputGetDouble(m.ElecSubPbar);

                double qval = h.helicsInputGetDouble(m.ElecSubQ_sm3s);

                if (Init == "Initialization")
                {
                    HasViolations = (valPth < 0) || (valPbar < 0) || (qval < 0);
                    if (HasViolations) break;
                    else
                    {
                        Console.WriteLine(String.Format("Electric-R: Initialization Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t dPr = {4:0.0000} [bar]", Gtime, Iter, m.GFG.FGEN, valPth, valPbar));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Electric-R: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t dPr = {4:0.0000} [bar]", Gtime, Iter, m.GFG.FGEN, valPth, valPbar));


                //get currently required thermal power 
                //double pval = API.evalFloat(String.Format("ENO.{0}.P.({1}).[MW]", m.GFG.FGENName, gtime * m.GFG.FGEN.Net.SCE.dt / 3600));
                double pval = m.GFG.FGEN.get_P((int)(gtime * m.GFG.FGEN.Net.SCE.dt / 3600));
                //double pval2 = m.ElectricGen.get_P((int)gtime);
                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                m.lastVal.Add(valPth);

                if (Math.Abs(ThermalPower-valPth) > eps && Iter>=0)
                {
                    if (valPbar < eps || qval >= m.Qmax)
                    {
                        double PG = GetActivePowerFromAvailableThermalPower(m, valPth, pval);
                        double PGMAXset = Math.Max(0, Math.Min(PG, m.NCAP));

                        foreach (var evt in m.GFG.FGEN.SceList)
                        {
                           
                            if (evt.ObjPar == CtrlType.PMIN)
                            {
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                                evt.ShowVal = string.Format("{0}",PGMAXset);
                                evt.Processed = false;

                                Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PMINn = {3:0.0000} [MW/s] \t PMINn-1 = {4:0.0000} [MW]",
                                    Gtime, Iter, m.GFG.FGEN, evt.ObjVal, EvtVal));
                            }
                            if (evt.ObjPar == CtrlType.PMAX)
                            {
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                                evt.ShowVal = string.Format("{0}", PGMAXset);
                                evt.Processed = false;

                                Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PMAXn = {3:0.0000} [MW/s] \t PMAXn-1 = {4:0.0000} [MW]",
                                    Gtime, Iter, m.GFG.FGEN, evt.ObjVal, EvtVal));
                            }
                        }

                        Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PGMAXnew = {3:0.0000} [MW]", Gtime, Iter, m.GFG.FGEN, m.GFG.FGEN.get_PMAX((int)gtime)));
                    }
                    HasViolations = true;
                }
                else
                {
                    if (m.lastVal.Count > 2)
                    {
                        //if ((Math.Abs(m.lastVal[m.lastVal.Count - 1] - m.lastVal[m.lastVal.Count - 2]) > eps) || (Math.Abs(m.lastVal[m.lastVal.Count - 2] - m.lastVal[m.lastVal.Count - 3]) > eps))
                        if (Math.Abs(m.lastVal[m.lastVal.Count - 1] - m.lastVal[m.lastVal.Count - 2]) > eps) 
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

        public static bool SubscribeToRequiredThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList, string Init = "Execute")
        {
            GNET = (GasNet)GetObject("get_GNET");
            bool HasViolations = false;
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // get publication from electric federate
                double val = h.helicsInputGetDouble(m.GasSubPthe);
                

                if (Init == "Initialization")
                {
                    HasViolations = (val < 0);
                    if (HasViolations) break;
                    else
                    {
                        Console.WriteLine(String.Format("Gas-R: Initialization Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW]", Gtime, step, m.GFG.GDEM, val));
                        continue;
                    }
                }

                Console.WriteLine(String.Format("Gas-R: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW]", Gtime, step, m.GFG.GDEM, val));

                m.lastVal.Add(val);

                //get currently available thermal power 
                //double GCV = API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double GCV = m.GFG.get_GCV((int)(gtime * m.GFG.GDEM.Net.SCE.dt / 3600));

                //double pval = GCV*API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));   
                //double pval = m.GasNode.get_Q((int)(gtime * m.GasNode.Net.SCE.dt / 3600)) * m.GFG.get_GasNQ((int)(gtime)).GCV/1e6;
                double pval = GCV * m.GFG.GDEM.get_Q((int)(gtime * m.GFG.GDEM.Net.SCE.dt / 3600));

                if (Math.Abs(pval - val) > eps )
                {               
                    // calculate offtakes at corresponding node using heat rates
                    foreach (ScenarioEvent evt in m.GFG.GDEM.SceList)
                    {
                        if (evt.ObjPar == SAInt_API.Model.CtrlType.QSET)
                        {
                            double EvtVal = evt.ObjVal;
                            evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                            //evt.ShowVal = string.Format("{0}", 1E6 * val /m.GFG.get_GasNQ((int)gtime).GCV); // converting thermal power to flow rate using calorific value
                            evt.ShowVal = string.Format("{0}", val / GCV);
                            evt.StartTime = Gtime;
                            evt.Processed = false;
                            evt.Active = true;

                            Console.WriteLine(String.Format("Gas-E: Time {0} \t iter {1} \t {2} \t QSETn = {3:0.0000} [sm3/s] \t QSETn-1 = {4:0.0000} [sm3/s]", 
                                Gtime, step, m.GFG.GDEM, evt.ObjVal,EvtVal));
                        }
                    }
                    HasViolations = true;
                }
                else
                {
                    if (m.lastVal.Count > 2)
                    {
                        if (Math.Abs(m.lastVal[m.lastVal.Count - 2] - m.lastVal[m.lastVal.Count - 1]) > eps)
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
            int maxiter = 20;
            int i=0;
            double p=initVal;

            while (i<maxiter)
            {
                p -=  GetF(p)/GetdFdx(p);

                Res =Math.Abs(GetF(p));

                if (Res < eps) { return p; }

                i+=1;
            }

            return p;
        }

        public static List<ElectricGasMapping> GetMappingFromHubs(IList<GasFiredGenerator> GFGs)
        {
            List<ElectricGasMapping> MappingList = new List<ElectricGasMapping>();

            foreach (GasFiredGenerator m in GFGs)
            {
                
                var mapitem = new ElectricGasMapping();
                mapitem.GFG = m;
                mapitem.lastVal = new List<double>();
                if (m.GDEM != null) mapitem.Qmax = m.GDEM.get_QMAX();
                if (m.FGEN != null) mapitem.NCAP = m.FGEN.get_PMAX();
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

        public double NCAP;

        public double Qmax;

        public double PreVal;

        public List<double> lastVal;

        public SWIGTYPE_p_void GasPubPth;
        public SWIGTYPE_p_void GasPubPbar;
        public SWIGTYPE_p_void ElecSubPthg;
        public SWIGTYPE_p_void ElecSubPbar;
        public SWIGTYPE_p_void GasPubQ_sm3s;
        public SWIGTYPE_p_void ElecSubQ_sm3s;

        public SWIGTYPE_p_void ElectricPubPthe;
        public SWIGTYPE_p_void GasSubPthe;

        public StreamWriter sw;
    }

    public class TimeStepInfo
    {
        public int timestep, itersteps;
        public DateTime time;
    }

    public class NotConverged
    {
        public int timestep, itersteps;
        public DateTime time;
    }

}
