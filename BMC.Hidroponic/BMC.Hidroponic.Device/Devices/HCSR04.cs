using System;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;

namespace BMC.Hidroponic.Device
{
    /// Class for controlling the HC-SR04 Ultrasonic Range detector
    /// Written by John E. Wilson
    public class HC_SR04
    {
        private OutputPort portOut;
        private InterruptPort interIn;
        private long beginTick;
        private long endTick;
        private long minTicks; // System latency, subtracted off ticks to find actual sound travel time
        private double inchConversion;
        private double version;
        public HC_SR04(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
        {
            portOut = new OutputPort(pinTrig, false);
            interIn = new InterruptPort(pinEcho, false, Port.ResistorMode.Disabled,
           Port.InterruptMode.InterruptEdgeLow);
            interIn.OnInterrupt += new NativeEventHandler(interIn_OnInterrupt);
            minTicks = 6200L;
            inchConversion = 1440.0;
            version = 1.1;
        }
        public double Version
        {
            get { return version; }
        }
        /// <returns>Number of ticks it takes to get back sonic pulse</returns>
        public long Ping()
        {
            // Reset Sensor
            portOut.Write(true);
            Thread.Sleep(1);
            // Start Clock
            endTick = 0L;
            beginTick = System.DateTime.Now.Ticks;
            // Trigger Sonic Pulse
            portOut.Write(false);
            // Wait 1/20 second (this could be set as a variable instead of constant)
            Thread.Sleep(50);
            if (endTick > 0L)
            {
                // Calculate Difference
                long elapsed = endTick - beginTick;
                // Subtract out fixed overhead (interrupt lag, etc.)
                elapsed -= minTicks;
                if (elapsed < 0L)
                {
                    elapsed = 0L;
                }
                // Return elapsed ticks
                return elapsed;
            }
            // Sonic pulse wasn't detected within 1/20 second
            return -1L;

        }

        /// <param name="data1">Not used</param>
        /// <param name="data2">Not used</param>
        /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
        void interIn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            // Save the ticks when pulse was received back
            endTick = time.Ticks;
        }
        /// <param name="ticks"></param>
        public double TicksToInches(long ticks)
        {
            return (double)ticks / inchConversion;
        }
        /// The ticks to inches conversion factor
        public double InchCoversionFactor
        {
            get { return inchConversion; }
            set { inchConversion = value; }
        }
        /// The system latency (minimum number of ticks)
        /// This number will be subtracted off to find actual sound travel time
        public long LatencyTicks
        {
            get { return minTicks; }
            set { minTicks = value; }
        }
    }
}
