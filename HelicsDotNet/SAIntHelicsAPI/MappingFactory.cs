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
                // Set initital publication of thermal power request equivalent to PGMAX for time = 0 and iter = 0;
                if (gtime == 0 && step == 0)
                {
                    pval = m.GFG.FGEN.get_PMAX();
                }

                else
                {                                
                    pval = API.evalFloat(String.Format("ENO.{0}.P.({1}).[MW]", m.GFG.FGENName, gtime * m.GFG.FGEN.Net.SCE.dt / 3600));                    
                }

                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR/3.6 * pval; //Thermal power in [MW]

                h.helicsPublicationPublishDouble(m.ElectricPub, ThermalPower);

                Console.WriteLine(String.Format("Electric-S: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW] \t P = {4:0.0000} [MW] \t  PGMAX = {5:0.0000} [MW]", 
                    Gtime,step, m.GFG.FGEN, ThermalPower,pval,m.GFG.FGEN.get_PMAX()));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    gtime,step, pval, ThermalPower , m.GFG.FGEN.get_PMAX()));
            }
        }

        public static void PublishAvailableThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");
            
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                //double pva3 = API.evalFloat(String.Format("{0}.P.[sm3/s]", m.GasNode));
                double pval = API.evalFloat(String.Format("{0}.P.({1}).[bar-g]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double qval = API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double GCV = API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double qvalN15 = m.GFG.GDEM.get_Q((int)gtime);

                double ThermalPower  = qval * GCV; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.GasPubPth, ThermalPower);
                h.helicsPublicationPublishDouble(m.GasPubPbar, pval-(m.GFG.GDEM.GNET.get_PMIN((int)gtime)-m.GFG.GDEM.GNET.PAMB)/1e5);

                Console.WriteLine(String.Format("Gas-S: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t P {4:0.0000} [bar-g] \t Q {5:0.0000} [sm3/s]", 
                    Gtime, step, m.GFG.GDEM, ThermalPower, pval, qval));
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", 
                    Gtime, step, pval, qval, ThermalPower));
            }
        }

        public static bool SubscribeToAvailableThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            ENET = (ElectricNet)GetObject("get_ENET");
            bool HasViolations = false;
            DateTime Gtime = ENET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)ENET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // subscribe to available thermal power from gas node
                double valPth = h.helicsInputGetDouble(m.GasSubPth);
                // subscribe to pressure difference between nodal pressure and minimum pressure from gas node
                double valPbar = h.helicsInputGetDouble(m.GasSubPbar);

                Console.WriteLine(String.Format("Electric-R: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t dPr = {4:0.0000} [bar]", Gtime, step, m.GFG.FGEN, valPth,valPbar));

                //get currently required thermal power 
                double pval = API.evalFloat(String.Format("ENO.{0}.P.({1}).[MW]", m.GFG.FGENName, gtime * m.GFG.FGEN.Net.SCE.dt / 3600));
                //double pval3 = m.ElectricGen.get_P((int)(gtime * m.ElectricGen.Net.SCE.dt / 3600));
                //double pval2 = m.ElectricGen.get_P((int)gtime);
                double HR = m.GFG.FGEN.HR0 + m.GFG.FGEN.HR1 * pval + m.GFG.FGEN.HR2 * pval * pval;
                double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]

                m.lastVal.Add(valPth);

                if (Math.Abs(ThermalPower-valPth) > eps && step>=0)
                {
                    if (valPbar < eps)
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
                                    Gtime, step, m.GFG.FGEN, evt.ObjVal, EvtVal));
                            }
                            if (evt.ObjPar == CtrlType.PMAX)
                            {
                                double EvtVal = evt.ObjVal;
                                evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.PPOW, SAInt_API.Library.Units.UnitList.MW);
                                evt.ShowVal = string.Format("{0}", PGMAXset);
                                evt.Processed = false;

                                Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PMAXn = {3:0.0000} [MW/s] \t PMAXn-1 = {4:0.0000} [MW]",
                                    Gtime, step, m.GFG.FGEN, evt.ObjVal, EvtVal));
                            }
                        }

                        Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PGMAXnew = {3:0.0000} [MW]", Gtime, step, m.GFG.FGEN, m.GFG.FGEN.get_PMAX((int)gtime)));
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
            return HasViolations;
        }

        public static bool SubscribeToRequiredThermalPower(double gtime, int step, List<ElectricGasMapping> MappingList)
        {
            GNET = (GasNet)GetObject("get_GNET");
            bool HasViolations = false;
            DateTime Gtime = GNET.SCE.StartTime + new TimeSpan(0, 0, (int)gtime * (int)GNET.SCE.dt);

            foreach (ElectricGasMapping m in MappingList)
            {
                // get publication from electric federate
                double val = h.helicsInputGetDouble(m.ElectricSub);
                Console.WriteLine(String.Format("Gas-R: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW]", Gtime, step, m.GFG.GDEM, val));

                m.lastVal.Add(val);

                //get currently available thermal power 
                double GCV = API.evalFloat(String.Format("GFG.{0}.GCV.({1}).[MJ/sm3]", m.GFG.Name, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));
                double pval = GCV*API.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GFG.GDEM, gtime * m.GFG.GDEM.Net.SCE.dt / 3600));   
                //double pval = m.GasNode.get_Q((int)(gtime * m.GasNode.Net.SCE.dt / 3600)) * m.GFG.get_GasNQ((int)(gtime)).GCV/1e6;

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
