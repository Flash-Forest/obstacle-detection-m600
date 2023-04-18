#!/usr/bin/env python3
###### TeraRanger Evo Example Code STD #######
#                                            #
# All rights reserved Terabee France (c) 2018#
#                                            #
############ www.terabee.com #################

import sys
import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import time

# SETUP VARIABLES
path = '/home/flash/Documents/Obstacle Detection/Terabee/Logging/'
file1 = 'test.csv'
df = pd.read_csv(path + file1)

fig = plt.figure()
ax = plt.axes()

def animate(i):
    x1 = df.iloc[] 


if __name__ == "__main__":

    
    
    # Initialize detection variables
    cnt = 0
    ref = 0
    thresh = 1.0
    obstacle_flag = False
    t = time.time()

