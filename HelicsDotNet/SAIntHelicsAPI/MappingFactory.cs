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

        public static void PublishRequiredThermalPower(double gtime, List<Mapping> MappingList)
       {
            foreach (Mapping m in MappingList)
            {
                double pval = APIExport.evalFloat(String.Format("{0}.PG.[MW]", m.ElectricGenID));
                double HR = m.ElectricGen.K_0 + m.ElectricGen.K_1 * pval + m.ElectricGen.K_2 * pval * pval;
                // relation between thermal efficiency and heat rate: eta_th[-]=3.6/HR[MJ/kWh]
                double ThermalPower = HR/3.6 * pval; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.ElectricPub, ThermalPower);
                Console.WriteLine(String.Format("Electric: Sending value for required thermal power for Generator {2} in [MW] = {0} at time {1} to Gas federate", ThermalPower, gtime, m.ElectricGen));
                if (gtime == 0) {
                    m.sw.WriteLine("iter \t PG[MW] \t ThPow [MW]");
                }
                m.sw.WriteLine(String.Format("{0} \t {1} \t {2}", gtime, pval, ThermalPower ));
            }
        }

        public static void PublishAvailableThermalPower(double gtime, List<Mapping> MappingList)
        {
            foreach (Mapping m in MappingList)
            {
                double pval = APIExport.evalFloat(String.Format("{0}.P.[bar-g]", m.GasNodeID));
                double qval = APIExport.evalFloat(String.Format("{0}.Q.[sm3/s]", m.GasNodeID));
                double ThermalPower  = qval * s.GNET.CV / 1E6; //Thermal power in [MW]
                h.helicsPublicationPublishDouble(m.GasPub, ThermalPower);
                Console.WriteLine(String.Format("Gas: Sending value for available thermal power for node {2} in [MW] = {0} for delivery pressure {3} [bar-g] at time {1} to Electric federate",ThermalPower, gtime, m.GasNode,pval));
                if (gtime == 0){
                    m.sw.WriteLine("iter \t P[bar-g] \t Q [sm3/s] \t ThPow [MW]");
                }

                m.sw.WriteLine(String.Format("{0} \t {1} \t {2} \t {3}", gtime, pval, qval, ThermalPower));
            }
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

        public SWIGTYPE_p_void GasPub;
        public SWIGTYPE_p_void GasSub;

        public SWIGTYPE_p_void ElectricPub;
        public SWIGTYPE_p_void ElectricSub;

        public StreamWriter sw;
    }

}
