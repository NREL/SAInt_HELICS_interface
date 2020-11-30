# -*- coding: utf-8 -*-
import time
import helics as h

from ctypes import *
from math import *

############################
# Set model folder and API folder
dllfolder="C:\\Program Files\\encoord\\SAInt Software 2.0\\"
netfolder="C:\\Users\\KP\\Documents\\GitHub\\Natural-gas-common-interface\\Demo\\"

# Set return types for API calls
mydll=cdll.LoadLibrary(dllfolder + "SAInt-API.dll")
mydll.evalStr.restype=c_wchar_p
mydll.evalInt.restype=c_int
mydll.evalBool.restype=c_bool
mydll.evalFloat.restype=c_float

# Load Electric Model
mydll.openENET(netfolder + "ENET30.enet")
mydll.openESCE(netfolder + "CMBSTEOPF.esce")
mydll.showSIMLOG(False)
# Define Gas-Electric Coupling
CoupledGasNode='N15'
CoupledElectricNode='BUS001'
#######################
  
initBrokerString = "-f 2 --name=mainbroker"
fedinitstring = "--broker=mainbroker --federates=1"
deltat = 0.01

helicsversion = h.helicsGetVersion()

print("Electric: Helics version = {}".format(helicsversion))

# Create broker #
print("Creating Broker")
broker = h.helicsCreateBroker("zmq", "", initBrokerString)
print("Created Broker")

print("Checking if Broker is connected")
isconnected = h.helicsBrokerIsConnected(broker)
print("Checked if Broker is connected")

if isconnected == 1:
    print("Broker created and connected")

# Create Federate Info object that describes the federate properties #
fedinfo = h.helicsCreateFederateInfo()

# Set Federate name #
h.helicsFederateInfoSetCoreName(fedinfo, "Electric Federate Core")

# Set core type from string #
h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "zmq")

# Federate init string #
h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring)

# Set the message interval (timedelta) for federate. Note th#
# HELICS minimum message time interval is 1 ns and by default
# it uses a time delta of 1 second. What is provided to the
# setTimedelta routine is a multiplier for the default timedelta.

# Set one second message interval #
h.helicsFederateInfoSetTimeProperty(fedinfo, h.helics_property_time_delta, deltat)

# Create value federate #
vfed = h.helicsCreateValueFederate("Electric Federate", fedinfo)
print("Electric: Value federate created")

# Register the publication #
pubElectricOutPut = h.helicsFederateRegisterGlobalTypePublication(vfed, "ElectricOutPut", "double", "")
print("Electric: Publication registered")

# Subscribe to Gas publication
sub = h.helicsFederateRegisterSubscription(vfed, "GasOutPut", "")
print("Electric: Subscription registered")

# Enter execution mode #
h.helicsFederateEnterExecutingMode(vfed)
print("Electric: Entering execution mode")

for t in range(5, 10):
    currenttime = h.helicsFederateRequestTime(vfed, t)
    mydll.runESIM()
    StrNoLP='BUS.{}.PG.[MW]'.format(CoupledElectricNode)         
    p=mydll.evalFloat(StrNoLP)
    h.helicsPublicationPublishDouble(pubElectricOutPut, p)
    print("Electric: Sending value for active power in [MW] = {} at time {} to Gas federate".format(p, currenttime))
    value = h.helicsInputGetDouble(sub)
    print("Electric: Received value = {} at time {} from Gas federate for pressue in [bar-g]".format(value, currenttime))

    #mydll.eval("ESYS.SCE.SceList[6].ShowVal='{}'".format(value/20))

    time.sleep(1)

h.helicsFederateFinalize(vfed)
print("Electric: Federate finalized")

while h.helicsBrokerIsConnected(broker):
     time.sleep(1)

h.helicsFederateFree(vfed)
h.helicsCloseLibrary()

print("GasElectric: Broker disconnected")