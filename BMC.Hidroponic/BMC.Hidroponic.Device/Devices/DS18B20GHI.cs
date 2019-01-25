using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace BMC.Hidroponic.Device
{
    public class DS18B20GHI
    {
        // Change this your correct pin!           
        OutputPort myPin;
        public DS18B20GHI(Cpu.Pin DigitalPin)
        {
            try
            {
                myPin = new OutputPort(DigitalPin, false);
                Thread th2 = new Thread(new ThreadStart(Setup));
                th2.Start();
            }
            catch(Exception ex) {
                Debug.Print("fail to connect DS18B20 : " + ex.Message);
            }
        }
        public double TempValue { get; set; }
        void Setup()
        {
            OneWire ow = new OneWire(myPin);
            ushort temperature;

            // read every second
            while (true)
            {
                if (ow.TouchReset() > 0)
                {
                    ow.WriteByte(0xCC);     // Skip ROM, we only have one device
                    ow.WriteByte(0x44);     // Start temperature conversion

                    while (ow.ReadByte() == 0) ;   // wait while busy

                    ow.TouchReset();
                    ow.WriteByte(0xCC);     // skip ROM
                    ow.WriteByte(0xBE);     // Read Scratchpad

                    temperature = (byte)ow.ReadByte();                 // LSB 
                    temperature |= (ushort)(ow.ReadByte() << 8); // MSB
                    TempValue = temperature / 16;
                    Debug.Print("Temperature: " + TempValue);
                    Thread.Sleep(1000);
                }
                else
                {
                    Debug.Print("Device is not detected.");
                }

                Thread.Sleep(1000);
            }
        }
    }
}
