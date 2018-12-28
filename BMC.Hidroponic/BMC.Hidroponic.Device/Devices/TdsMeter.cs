using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace BMC.Hidroponic.Device
{
    public class TdsMeter:IDisposable
    {
        public double tdsValue { get; set; }
        const double VREF = 5.0;      // analog reference voltage(Volt) of the ADC
        const int SCOUNT = 30;           // sum of sample point
        double[] analogBuffer;    // store the analog value in the array, read from ADC
        double[] analogBufferTemp;
        int analogBufferIndex = 0, copyIndex = 0;
        double averageVoltage = 0, temperature = 25;
        AnalogInput tdsSensor;
        Thread th1;
        public TdsMeter(Cpu.AnalogChannel AnalogPin)
        {
            analogBuffer = new double[SCOUNT];
            analogBufferTemp = new double[SCOUNT];
            tdsSensor = new AnalogInput(AnalogPin);
            tdsValue = 0;
            th1 = new Thread(new ThreadStart(Loop));
            analogSampleTimepoint = DateTime.Now;
            printTimepoint = DateTime.Now;
            th1.Start();
        }
        static DateTime analogSampleTimepoint;
        static DateTime printTimepoint;
        void Loop()
        {
            while (true)
            {
                var gap = DateTime.Now - analogSampleTimepoint;
                if (gap.Ticks / 10000.0 > 40U)     //every 40 milliseconds,read the analog value from the ADC
                {
                    analogSampleTimepoint = DateTime.Now;
                    analogBuffer[analogBufferIndex] = tdsSensor.ReadRaw();    //read the analog value and store into the buffer
                    analogBufferIndex++;
                    if (analogBufferIndex == SCOUNT)
                        analogBufferIndex = 0;
                }

                var gap2 = DateTime.Now - printTimepoint;
                if (gap2.Ticks / 10000.0 > 800U)
                {
                    printTimepoint = DateTime.Now;
                    for (copyIndex = 0; copyIndex < SCOUNT; copyIndex++)
                        analogBufferTemp[copyIndex] = analogBuffer[copyIndex];
                    averageVoltage = getMedianNum(analogBufferTemp, SCOUNT) * VREF / 1024.0; // read the analog value more stable by the median filtering algorithm, and convert to voltage value
                    double compensationCoefficient = 1.0 + 0.02 * (temperature - 25.0);    //temperature compensation formula: fFinalResult(25^C) = fFinalResult(current)/(1.0+0.02*(fTP-25.0));
                    double compensationVolatge = averageVoltage / compensationCoefficient;  //temperature compensation
                    tdsValue = (133.42 * compensationVolatge * compensationVolatge * compensationVolatge - 255.86 * compensationVolatge * compensationVolatge + 857.39 * compensationVolatge) * 0.5; //convert voltage value to tds value
                    //Serial.print("voltage:");
                    //Serial.print(averageVoltage,2);
                    //Serial.print("V   ");
                    //Serial.print("TDS Value:");
                    //Serial.print(tdsValue,0);
                    //Serial.println("ppm");
                }
                //Thread.Sleep(20);
            }
        }
        double getMedianNum(double[] bArray, int iFilterLen)
        {
            double[] bTab = new double[iFilterLen];
            for (byte x = 0; x < iFilterLen; x++)
                bTab[x] = bArray[x];
            int i, j;
                double bTemp;
            for (j = 0; j < iFilterLen - 1; j++)
            {
                for (i = 0; i < iFilterLen - j - 1; i++)
                {
                    if (bTab[i] > bTab[i + 1])
                    {
                        bTemp = bTab[i];
                        bTab[i] = bTab[i + 1];
                        bTab[i + 1] = bTemp;
                    }
                }
            }
            if ((iFilterLen & 1) > 0)
                bTemp = bTab[(iFilterLen - 1) / 2];
            else
                bTemp = (bTab[iFilterLen / 2] + bTab[iFilterLen / 2 - 1]) / 2;
            return bTemp;

        }

        public void Dispose()
        {
            th1.Abort();
        }
    }
}
