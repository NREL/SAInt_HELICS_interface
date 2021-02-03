# -*- coding: utf-8 -*-
import time
import helics as h

initBrokerString = "-f 2 --name=mainbroker"
fedinitstring = "--broker=mainbroker --federates=1"
deltat = 0.01

helicsversion = h.helicsGetVersion()

print("GasElectricBroker: Helics version = {}".format(helicsversion))

# Create broker #
print("Creating Broker")
broker = h.helicsCreateBroker("zmq", "", initBrokerString)
print("Created Broker")

print("Checking if Broker is connected")
isconnected = h.helicsBrokerIsConnected(broker)
print("Checked if Broker is connected")

if isconnected == 1:
    print("Broker created and connected")


while h.helicsBrokerIsConnected(broker):
    time.sleep(1)

h.helicsCloseLibrary()

print("GasElectricBroker: Broker disconnected")