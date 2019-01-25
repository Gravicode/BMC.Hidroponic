using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace BMC.Hidroponic.Device
{
    public class PhMeter:IDisposable
    {
        double avgValue;  //Store the average value of the sensor feedback
        float b;
        double []buf;
      
        double temp;
        Thread th1;
        AnalogInput phSensor;
        public PhMeter(Cpu.AnalogChannel AnalogPin)
        {
            try
            {
                phSensor = new AnalogInput(AnalogPin);

                buf = new double[10];

                PhValue = 0;
                th1 = new Thread(new ThreadStart(Loop));
                th1.Start();
            }
            catch (Exception ex)
            {
                Debug.Print("Ph Sensor Error"+ex.Message);
            }
        }
        public double PhValue { get; set; }
        void Loop()
        {
            while (true)
            {
                for (int i = 0; i < 10; i++)       //Get 10 sample value from the sensor for smooth the value
                {
                    buf[i] = phSensor.ReadRaw();
                  
                    Thread.Sleep(100);
                }
                for (int i = 0; i < 9; i++)        //sort the analog from small to large
                {
                    for (int j = i + 1; j < 10; j++)
                    {
                        if (buf[i] > buf[j])
                        {
                            temp = buf[i];
                            buf[i] = buf[j];
                            buf[j] = temp;
                        }
                    }
                }
                avgValue = 0;
                for (int i = 2; i < 8; i++)                      //take the average value of 6 center sample
                    avgValue += buf[i];
                PhValue = avgValue * 5.0 / 1024 / 6; //convert the analog into millivolt
                PhValue = 3.5 * PhValue;                      //convert the millivolt into pH value
                //Serial.print("    pH:");
                //Serial.print(phValue, 2);
                //Serial.println(" ");
                //digitalWrite(13, HIGH);
                Thread.Sleep(2000);
                //digitalWrite(13, LOW); 
            }
        }

        public void Dispose()
        {
            th1.Abort();
        }
    }
}
