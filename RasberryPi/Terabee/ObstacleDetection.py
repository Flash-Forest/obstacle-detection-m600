#!/usr/bin/env python3
###### TeraRanger Evo Example Code STD #######
#                                            #
# All rights reserved Terabee France (c) 2018#
#                                            #
############ www.terabee.com #################

import serial
import serial.tools.list_ports
import RPi.GPIO as GPIO
import sys
import crcmod.predefined  # To install: pip install crcmod
import pandas as pd
import time


def findEvo():
    # Find Live Ports, return port name if found, NULL if not
    print('Scanning all live ports on this PC')
    ports = list(serial.tools.list_ports.comports())
    for p in ports:
        # print p # This causes each port's information to be printed out.
        if "5740" in p[2]:
            print('Evo found on port ' + p[0])
            return p[0]
    return 'NULL'


def openEvo(portname):
    print('Attempting to open port...')
    # Open the Evo and catch any exceptions thrown by the OS
    print(portname)
    evo = serial.Serial(portname, baudrate=115200, timeout=2)
    # Send the command "Binary mode"
    set_bin = (0x00, 0x11, 0x02, 0x4C)
    # Flush in the buffer
    evo.flushInput()
    # Write the binary command to the Evo
    evo.write(set_bin)
    # Flush out the buffer
    evo.flushOutput()
    print('Serial port opened')
    return evo


def get_evo_range(evo_serial):
    crc8_fn = crcmod.predefined.mkPredefinedCrcFun('crc-8')
    # Read one byte
    data = evo_serial.read(1)
    if data == b'T':
        # After T read 3 bytes
        frame = data + evo_serial.read(3)
        if frame[3] == crc8_fn(frame[0:3]):
            # Convert binary frame to decimal in shifting by 8 the frame
            rng = frame[1] << 8
            rng = rng | (frame[2] & 0xFF)
        else:
            return "CRC mismatch. Check connection or make sure only one progam access the sensor port."
    # Check special cases (limit values)
    else:
        return "Wating for frame header"

    # Checking error codes
    if rng == 65535: # Sensor measuring above its maximum limit
        dec_out = float('inf')
    elif rng == 1: # Sensor not able to measure
        dec_out = float('nan')
    elif rng == 0: # Sensor detecting object below minimum range
        dec_out = -float('inf')
    else:
        # Convert frame in meters
        dec_out = rng / 1000.0
    return dec_out


if __name__ == "__main__":
    # SETUP VARIABLES
    use_radio = False
    use_logging = True
    
    # Logging variables
    T = []
    S = []
    D = []
    
    # Connect to radio if connected
    if use_radio:
        radio_port = '/dev/ttyUSB0'
        print('Connecting to radio telemetry module on port:' + radio_port)

        try:
            radio = serial.Serial('/dev/ttyUSB0', baudrate=57600, timeout=1)
            print('Radio successfully connected')
        except:
            pass

    
    print('Starting Evo data streaming')
    # Get the port the evo has been connected to
    ports = ['/dev/ttyACM0', '/dev/ttyACM1']
    evos = []
    # Connect to each port
    for port in ports:
        if port == 'NULL':
            print("Sorry couldn't find the Evo. Exiting.")
            sys.exit()
        else:
            evo = openEvo(port)
            evos.append(evo)

    # Initialize detection variables
    cnt = 0
    ref = 0
    thresh = 1.0
    obstacle_flag = False
    t = time.time()

    while True:
            
        try:
            # Obstacle detection logic            
            for sensor in evos:
                ref+=1
                dist = get_evo_range(sensor)
                print('Sensor: ' + sensor.name + ', Distance: ' + str(dist))
                
                # If saving data set up dataframe                
                if use_logging:
                    T.append(time.time() - t)
                    S.append(sensor.name)
                    D.append(str(dist))

                # Check if obstacle is within thresh
                if type(dist) == float:
                    if dist < thresh:                     
                        cnt+=1
                
                # Detect obstacle 3 times within 10 iterations      
                if cnt > 3:
                    cnt = 0
                    obstacle_flag = True

                if ref > 10:
                    ref = 0
                    cnt = 0

            if obstacle_flag:
                print('Obstacle detected. Press enter to resume mission...')
                if use_radio:
                    radio.write(str(1).encode())
                break
                
                

        except serial.serialutil.SerialException:
            print("Device disconnected (or multiple access on port). Exiting...")
            break

    if use_logging:
        path = '/home/flash/Documents/Obstacle Detection/Terabee/Logging/'
        file = sys.argv[1]
        df = pd.DataFrame(list(zip(T,S,D)), columns = ['Time', 'Sensor', 'Distance'])
        df.to_csv(path + file)

    evo.close()
    sys.exit()
