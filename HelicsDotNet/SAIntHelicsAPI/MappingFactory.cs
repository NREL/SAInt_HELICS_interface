using System;
using System.Collections.Generic;
using System.IO;
using s = SAInt_API.SAInt;
using SAInt_API;
using SAInt_API.Model.Network.Hub;
using SAInt_API.Model.Network.Electric;
using SAInt_API.Model.Network.Fluid.Gas;
using SAInt_API.Model;
//using SAInt_API.Network.Electric;
//using SAInt_API.Network.Gas;
using h = helics;

namespace SAIntHelicsLib
{
    public static class MappingFactory
    {
        //public static CombinedSystem ENET { get; set; }
        public static ElectricNet ENET { get; set; }

        //public static CombinedSystem GNET { get; set; }
        public static GasNet GNET { get; set; }
        public static FuelGenerator FGEN { get; set; }

        public static StreamWriter gasSw;
        public static StreamWriter elecSw;
        public static double eps = 0.001;
        
        public static void PublishRequiredThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
       {
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                double pval;
                // Set initital publication of thermal power request equivalent to PGMAX for time = 0 and iter = 0;
                if (gtime == 0 && step == 0)
                {
                    pval = m.ElectricGen.FGEN.get_PMAX();
                 }

                else
                {
                    pval = APIExport.evalFloat(String.Format("{0}.PG.({1}).[MW]", m.ElectricGenID, gtime * m.ElectricGen.Net.SCE.dt / 3600));
                }
                double HR = m.ElectricGen.HR0 + m.ElectricGen.HR1 * pval + m.ElectricGen.HR2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR/3.6 * pval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.ElectricPub, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW] \t P = {4:0.0000} [MW] \t  PGMAX = {5:0.0000} [MW]", 
                    Gtime,step, m.ElectricGen, ThermalPower,pval,m.ElectricGen.FGEN.get_PMAX()));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    gtime,step, pval, ThermalPower , m.ElectricGen.FGEN.get_PMAX()));
            }
        }

        public static void PublishAvailableThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                double pval = APIExport.evalFloat(String.Format("{0}.P.({1}).[bar-g]", m.GasNodeID,gtime * m.GasNode.Net.SCE.dt / 3600));
                double qval = APIExport.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GasNodeID,gtime * m.GasNode.Net.SCE.dt / 3600));

                double ThermalPower  = qval * m.GasNode.get_GNQ((int)gtime).GCV/ 1E6; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.GasPubPth, ThermalPower);
                h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GasNode.SceList.PMIN.Val-m.GasNode.GNET.PAMB)/1e5);

                Console.WriteLine(String.Format("Gas-S: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t P {4:0.0000} [bar-g] \t Q {5:0.0000} [sm3/s]", 
                    Gtime, step, m.GasNode, ThermalPower, pval, qval));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    Gtime, step, pval, qval, ThermalPower));
            }
        }

        public static bool SubscribeToAvailableThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            bool HasViolations = false;
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // subscribe to available thermal power from gas node
                double valPth = h.helicsInputGetDouble(m.GasSubPth);
                // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                double valPbar = h.helicsInputGetDouble(m.GasSubPbar);

                Console.WriteLine(String.Format("Electric-R: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t dPr = {4:0.0000} [bar]", Gtime, step, m.ElectricGen, valPth,valPbar));

                //get currently required thermal power 
                double pval = APIExport.evalFloat(String.Format("{0}.PG.({1}).[MW]", m.ElectricGenID,gtime*m.ElectricGen.Net.SCE.dt/3600));
                double HR = m.ElectricGen.HR0 + m.ElectricGen.HR1 * pval + m.ElectricGen.HR2 * pval * pval;
                double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                m.lastVal.Add(valPth);

                if (Math.Abs(ThermalPower-valPth) > eps && step>=0)
                {
                    if (valPbar < eps)
                    { 
                        double PG = GetActivePowerFromAvailableThermalPower(m, valPth, pval);
                        double PGMAXset = Math.Max(0, Math.Min(PG, m.NCAP));
                        m.ElectricGen.SceList.PMIN = PGMAXset;
                        m.ElectricGen.SceList.PMAX = PGMAXset; 
                        Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PGMAXnew = {3:0.0000} [MW]", Gtime, step, m.ElectricGen, m.ElectricGen.PGMAX));
                    }
                    HasViolations = true;
                }
                else
                {
                    if (m.lastVal.Count > 1)
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
            return HasViolations;
        }

        public static bool SubscribeToRequiredThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            bool HasViolations = false;
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // get publication from electric federate
                double val = h.helicsInputGetDouble(m.ElectricSub);
                Console.WriteLine(String.Format("Gas-R: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW]", Gtime, step, m.GasNode, val));

                m.lastVal.Add(val);

                //get currently available thermal power 
                double pval = APIExport.evalFloat(String.Format("{0}.Q.({1}).[sm3/s] * {0}.CV.({1}).[MJ/sm3]", m.GasNodeID, gtime * m.GasNode.Net.SCE.dt / 3600));

                if (Math.Abs(pval - val) > eps )
                {               
                    // calculate offtakes at corresponding node using heat rates
                    foreach (var evt in m.GasNode.SceList)
                    {
                        if (evt.ObjPar == SAInt_API.Model.CtrlType.QSET)
                        {
                            double EvtVal = evt.ObjVal;
                            evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                            evt.ShowVal = string.Format("{0}", 1E6 * val /m.GasNode.get_GNQ((int)gtime).GCV); // converting thermal power to flow rate using calorific value
                            evt.Processed = false;

                            Console.WriteLine(String.Format("Gas-E: Time {0} \t iter {1} \t {2} \t QSETn = {3:0.0000} [sm3/s] \t QSETn-1 = {4:0.0000} [sm3/s]", 
                                Gtime, step, m.GasNode, evt.ObjVal,EvtVal));
                        }
                    }
                    HasViolations = true;
                }
                else
                {
                    if (m.lastVal.Count > 1)
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
            return HasViolations;
        }

        public static double GetActivePowerFromAvailableThermalPower(ElectricGasMapping m,double Pth, double initVal)
        {
            Func<double,double> GetHR = (x) => m.ElectricGen.HR0 + m.ElectricGen.HR1 * x + m.ElectricGen.HR2 * x * x;
            Func<double, double> GetF = (x) => 3.6 * Pth - x * GetHR(x);
            Func<double, double> GetdFdx = (x) => -(m.ElectricGen.HR0 + 2*m.ElectricGen.HR1 * x + 3*m.ElectricGen.HR2 * x * x);

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
            //throw new Exception("No solution found for given thermal power");
        }

        public static List<ElectricGasMapping> GetMappingFromFile(string filename)
        {
            List<ElectricGasMapping> MappingList = new List<ElectricGasMapping>();

            if (File.Exists(filename))
            {
                MappingList.Clear();
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        var zeile = new string[0];
                        while (sr.Peek() != -1)
                        {
                            zeile = sr.ReadLine().Split(new[] { (char)9 }, StringSplitOptions.RemoveEmptyEntries);

                            if (zeile.Length > 1)
                            {
                                if (!zeile[0].Contains("%"))
                                {
                                    var mapitem = new ElectricGasMapping();
                                    mapitem.ElectricGenID = zeile[0];
                                    mapitem.GasNodeID = zeile[1];
                                    mapitem.ElectricGen = ENET[mapitem.ElectricGenID] as GasFiredGenerator;
                                    mapitem.GasNode = GNET[mapitem.GasNodeID] as GasNode;
                                    mapitem.lastVal = new List<double>();
                                    if (mapitem.ElectricGen != null) mapitem.NCAP = mapitem.ElectricGen.PMAX;
                                    MappingList.Add(mapitem);
                                    
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception(string.Format("File {0} does not exist!", filename));
            }
            return MappingList;
        }
    }

    public class ElectricGasMapping
    {
        public string GasNodeID;
        public GasNode GasNode;
        //public GasDemand GasNode=GFG(1);

        public string ElectricGenID;
        //public ElectricExternal ElectricGen;
        //public GasFiredGenerator GFG;
        public GasFiredGenerator ElectricGen;
        //public GasDemand GasNode = GFG.GDEM;

        public double NCAP;

        public double PreVal;

        public List<double> lastVal;

        public SWIGTYPE_p_void GasPubPth;
        public SWIGTYPE_p_void GasPubPbar;
        public SWIGTYPE_p_void GasSubPth;
        public SWIGTYPE_p_void GasSubPbar;

        public SWIGTYPE_p_void ElectricPub;
        public SWIGTYPE_p_void ElectricSub;

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
