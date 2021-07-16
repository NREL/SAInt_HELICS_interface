using System;
using System.Collections.Generic;
using System.IO;
using s = SAInt_API.SAInt;
using SAInt_API;
using SAInt_API.Network.Electric;
using SAInt_API.Network.Gas;
using gmlc;
using h = gmlc.helics;

namespace SAIntHelicsLib
{
    public static class MappingFactory
    {
        public static StreamWriter gasSw;
        public static StreamWriter elecSw;
        public static double eps = 0.001;

        public static void PublishRequiredThermalPower(double gtime, int step, List<Mapping> MappingList)
       {
            foreach (Mapping m in MappingList)
            {
                double pval = APIExport.evalFloat(String.Format("{0}.PG.({1}).[MW]", m.ElectricGenID,  gtime * m.ElectricGen.Net.SCE.dT / 3600));
                double HR = m.ElectricGen.K_0 + m.ElectricGen.K_1 * pval + m.ElectricGen.K_2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR/3.6 * pval; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.ElectricPub, ThermalPower);
                Console.WriteLine(String.Format("Electric-S: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW] \t P = {4:0.0000} [MW] \t  PGMAX = {5:0.0000} [MW]", gtime,step, m.ElectricGen, ThermalPower,pval,m.ElectricGen.PGMAX));

                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", gtime,step, pval, ThermalPower , m.ElectricGen.PGMAX));
            }
        }

        public static void PublishAvailableThermalPower(double gtime, int step, List<Mapping> MappingList)
        {
            foreach (Mapping m in MappingList)
            {
                double pval = APIExport.evalFloat(String.Format("{0}.P.({1}).[bar-g]", m.GasNodeID,gtime * m.GasNode.Net.SCE.dT / 3600));
                double qval = APIExport.evalFloat(String.Format("{0}.Q.({1}).[sm3/s]", m.GasNodeID,gtime * m.GasNode.Net.SCE.dT / 3600));
                double ThermalPower  = qval * s.GNET.CV / 1E6; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.GasPub, ThermalPower);
                Console.WriteLine(String.Format("Gas-S: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW] \t P {4:0.0000} [bar-g] \t Q {5:0.0000} [sm3/s]", gtime, step, m.GasNode, ThermalPower, pval, qval));

                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3} \t {4}", gtime, step, pval, qval, ThermalPower));
            }
        }

        public static bool SubscribeToAvailableThermalPower(double gtime, int step, List<Mapping> MappingList)
        {
            bool HasViolations = false;

            foreach (Mapping m in MappingList)
            {
                double val = h.helicsInputGetDouble(m.GasSub);
                Console.WriteLine(String.Format("Electric-R: Time {0} \t iter {1} \t {2} \t Pthg = {3:0.0000} [MW]", gtime, step, m.ElectricGen, val));

                //get currently required thermal power 
                double pval = APIExport.evalFloat(String.Format("{0}.PG.({1}).[MW]", m.ElectricGenID,gtime*m.ElectricGen.Net.SCE.dT/3600));
                double HR = m.ElectricGen.K_0 + m.ElectricGen.K_1 * pval + m.ElectricGen.K_2 * pval * pval;
                double ThermalPower = HR / 3.6 * pval; //Thermal power in [MW]; // eta_th=3.6/HR[MJ/kWh]
                
                if ((  ThermalPower-val) > eps)
                {
                    double PG = GetActivePowerFromAvailableThermalPower(m, val, pval);
                    m.ElectricGen.PGMAX = PG;
                    HasViolations = true;
                    Console.WriteLine(String.Format("Electric-E: Time {0} \t iter {1} \t {2} \t PGMAXnew = {3:0.0000} [MW]", gtime, step, m.ElectricGen, PG));
                }
                //else if (Math.Abs(ThermalPower - val) > eps)
                //{
                //    HasViolations = true;
                //}
            }

            return HasViolations;
        }

        public static bool SubscribeToRequiredThermalPower(double gtime, int step, List<Mapping> MappingList)
        {
            bool HasViolations = false;
            foreach (Mapping m in MappingList)
            {
                // get publication from electric federate
                double val = h.helicsInputGetDouble(m.ElectricSub);
                Console.WriteLine(String.Format("Gas-R: Time {0} \t iter {1} \t {2} \t Pthe = {3:0.0000} [MW]", gtime, step, m.GasNode, val));
                
                //get currently available thermal power 
                double pval = APIExport.evalFloat(String.Format("{0}.Q.({1}).[sm3/s] * {0}.CV.({1}).[MJ/sm3]", m.GasNodeID, gtime * m.GasNode.Net.SCE.dT / 3600));

                if (Math.Abs(pval - val) > eps)
                {               
                    // calculate offtakes at corresponding node using heat rates
                    foreach (var evt in m.GasNode.EventList)
                    {
                        if (evt.ObjPar == SAInt_API.Network.CtrlType.QSET)
                        {
                            double EvtVal = evt.ObjVal;
                            evt.Unit = new SAInt_API.Library.Units.Units(SAInt_API.Library.Units.UnitTypeList.Q, SAInt_API.Library.Units.UnitList.sm3_s);
                            evt.ShowVal = string.Format("{0}", 1E6 * val /s.GNET.CV); // converting thermal power to flow rate using calorific value
                            evt.Processed = false;

                            Console.WriteLine(String.Format("Gas-E: Time {0} \t iter {1} \t {2} \t QSETn = {3:0.0000} [sm3/s] \t QSETn-1 = {4:0.0000} [sm3/s]", gtime, step, m.GasNode, evt.ObjVal,EvtVal));
                            //if (!HasViolations) HasViolations = Math.Abs(EvtVal - evt.ObjVal) > eps;
                        }
                    }
                    HasViolations = true;
                }
            }
            return HasViolations;
        }

        public static double GetActivePowerFromAvailableThermalPower(Mapping m,double Pth, double initVal)
        {
            Func<double,double> GetHR = (x) => m.ElectricGen.K_0 + m.ElectricGen.K_1 * x + m.ElectricGen.K_2 * x * x;
            Func<double, double> GetF = (x) => 3.6 * Pth - x * GetHR(x);
            Func<double, double> GetdFdx = (x) => -(m.ElectricGen.K_0 + 2*m.ElectricGen.K_1 * x + 3*m.ElectricGen.K_2 * x * x);

            double Res = Math.Abs(GetF(initVal));
            int maxiter = 10;
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

        public static List<Mapping> GetMappingFromFile(string filename)
        {
            List<Mapping> MappingList = new List<Mapping>();

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
                                    var mapitem = new Mapping();
                                    mapitem.ElectricGenID = zeile[0];
                                    mapitem.GasNodeID = zeile[1];
                                    mapitem.ElectricGen = s.ENET[mapitem.ElectricGenID] as eGen;
                                    mapitem.GasNode = s.GNET[mapitem.GasNodeID] as GasNode;
                                    if (mapitem.ElectricGen != null) mapitem.NCAP = mapitem.ElectricGen.PGMAX;
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

    public class Mapping
    {
        public string GasNodeID;
        public GasNode GasNode;

        public string ElectricGenID;
        public eGen ElectricGen;

        public double NCAP;

        public SWIGTYPE_p_void GasPub;
        public SWIGTYPE_p_void GasSub;

        public SWIGTYPE_p_void ElectricPub;
        public SWIGTYPE_p_void ElectricSub;

        public StreamWriter sw;
    }

}
