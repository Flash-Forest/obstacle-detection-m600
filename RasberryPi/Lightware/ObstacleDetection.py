#!/usr/bin/env python3
###### TeraRanger Evo Example Code STD #######
#                                            #
# All rights reserved Terabee France (c) 2018#
#                                            #
############ www.terabee.com #################
import os
os.chdir('/home/flash/Documents/Obstacle Detection/Lightware')
import serial
import serial.tools.list_ports
import sys
import crcmod.predefined  # To install: pip install crcmod
import pandas as pd
import time
from datetime import datetime


def get_distance(port):
    # Each reading is contained on a single line.
    distanceStr = port.readline()
    
    # Convert the string to a numeric distance value.
    try:
        splitStr = distanceStr.decode().split(" ")
        distance = float(splitStr[0])
    except ValueError:
        # It is possible that the SF30 does not get a valid signal, we represent this case as a -1.0m.
        distance = -1.0		
    
    return distance


if __name__ == "__main__":
    # SETUP VARIABLES
    if len(sys.argv) > 2:
        use_radio = sys.argv[2]
    else:
        use_radio = True
    
    if len(sys.argv) > 3:
        use_logging = sys.argv[3]
    else:
        use_logging = False
    
    # Logging variables
    T = []
    S = []
    D = []
    
    # Connect to radio if connected
    if use_radio:
        radio_port = '/dev/ttyUSB0'
        print('Connecting to radio telemetry module on port:' + radio_port)

        try:
            radio = serial.Serial(radio_port, baudrate=57600, timeout=1)
            print('Radio successfully connected')
        except:
            pass

    
    print('Running SF30 sample.')

    # Make a connection to the com port. 
    serialPortName = '/dev/ttyUSB1'
    serialPortBaudRate = 115200
    port = serial.Serial(serialPortName, serialPortBaudRate, timeout=0.1)

    # Clear buffer of any partial responses.
    port.readline()

    # Initialize detection variables
    if len(sys.argv) > 1:
        thresh = sys.agrv[1]
    else:
        thresh = 20.0
    cnt = 0
    ref = 0
    obstacle_flag = False
    t = time.time()
    file_date = datetime.now().strftime("%d%m%Y_%H:%M:%S")
    print(file_date)

    while True:
        try:
            # Obstacle detection logic            
            
            ref+=1
            dist = get_distance(port)

            # Do what you want with the distance information here.
            print('Sensor: ' + port.name + ', Distance: ' + str(dist))
            
            # If saving data set up dataframe                
            if use_logging:
                T.append(time.time() - t)
                S.append(port.name)
                D.append(str(dist))

            # Check if obstacle is within thresh
            if type(dist) == float:
                if (dist < thresh) and (dist != -1.0):                     
                    cnt+=1
            
            # Detect obstacle 3 times within 10 iterations      
            if cnt > 3:
                cnt = 0
                obstacle_flag = True

            if ref > 10:
                ref = 0
                cnt = 0

            if obstacle_flag:
                print('Obstacle detected. Mission terminated.')
                if use_radio:
                    radio.write((str(1) + str(dist)).encode())
                    obstacle_flag = False
                
                
        except serial.serialutil.SerialException:
            print("Device disconnected (or multiple access on port). Exiting...")
            break

    if use_logging:
        path = '/home/flash/Documents/Obstacle Detection/Lightware/Logging/'
        file1 = 'test_'
        df = pd.DataFrame(list(zip(T,S,D)), columns = ['Time', 'Sensor', 'Distance'])
        df.to_csv(path + file1 + file_date)
        print('Log saved to ' + path + file1 + file_date)

    port.close()
    sys.exit()
