# -*- coding: utf-8 -*-
import time
import helics as h

from ctypes import *
from math import *

#################################
# Set model folder and API folder
dllfolder="C:\\Program Files\\encoord\\SAInt Software 2.0\\"
netfolder="C:\\Users\\KP\\Documents\\GitHub\\Natural-gas-common-interface\\Demo\\"

# Set return types for API calls
mydll=cdll.LoadLibrary(dllfolder + "SAInt-API.dll")
mydll.evalStr.restype=c_wchar_p
mydll.evalInt.restype=c_int
mydll.evalBool.restype=c_bool
mydll.evalFloat.restype=c_float

# Load Gas Model
mydll.openGNET(netfolder + "GNET25.net")
mydll.openGSCE(netfolder + "CMBSTEOPF.sce")
mydll.showSIMLOG(False)
# Define Gas-Electric Coupling
CoupledGasNode='N15'
CoupledElectricNode='BUS001'
#################################

fedinitstring = "--federates=1"
deltat = 0.01

helicsversion = h.helicsGetVersion()

print("Gas: Helics version = {}".format(helicsversion))

# Create Federate Info object that describes the federate properties */
print("Gas: Creating Federate Info")
fedinfo = h.helicsCreateFederateInfo()

# Set Federate name
print("Gas: Setting Federate Info Name")
h.helicsFederateInfoSetCoreName(fedinfo, "Gas Federate Core")

# Set core type from string
print("Gas: Setting Federate Info Core Type")
h.helicsFederateInfoSetCoreTypeFromString(fedinfo, "zmq")

# Federate init string
print("Gas: Setting Federate Info Init String")
h.helicsFederateInfoSetCoreInitString(fedinfo, fedinitstring)

# Set the message interval (timedelta) for federate. Note that
# HELICS minimum message time interval is 1 ns and by default
# it uses a time delta of 1 second. What is provided to the
# setTimedelta routine is a multiplier for the default timedelta.

# Set one second message interval
print("Gas: Setting Federate Info Time Delta")
h.helicsFederateInfoSetTimeProperty(fedinfo, h.helics_property_time_delta, deltat)

# Create value federate
print("Gas: Creating Value Federate")
vfed = h.helicsCreateValueFederate("Gas Federate", fedinfo)
print("Gas: Value federate created")

# Register the publication #
pubGasOutPut = h.helicsFederateRegisterGlobalTypePublication(vfed, "GasOutPut", "double", "")
print("Gas: Publication registered")

# Subscribe to Electric publication
sub = h.helicsFederateRegisterSubscription(vfed, "ElectricOutPut", "")
print("Gas: Subscription registered")

h.helicsFederateEnterExecutingMode(vfed)
print("Gas: Entering execution mode")

value = 0.0
prevtime = 0

currenttime = -1

while currenttime <= 100:

    currenttime = h.helicsFederateRequestTime(vfed, 100)

    value = h.helicsInputGetDouble(sub)
    mydll.eval("GSYS.SCE.SceList[6].ShowVal='{}'".format(value/20))
    mydll.runGSIM()
    print("Gas: Received value = {} at time {} from Electric federate for active power in [MW]".format(value, currenttime))

    StrNoLP='NO.{}.P.[bar-g]'.format(CoupledGasNode)         
    p=mydll.evalFloat(StrNoLP)
    h.helicsPublicationPublishDouble(pubGasOutPut, p)
    print("Gas: Sending value for pressure in [bar-g] = {} at time {} to Electric federate".format(p, currenttime))
    
h.helicsFederateFinalize(vfed)

h.helicsFederateFree(vfed)
h.helicsCloseLibrary()
print("Gas: Federate finalized")