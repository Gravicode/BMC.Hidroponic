using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using ThisBoard = GHI.Pins.FEZRaptor;
using Gadgeteer.SocketInterfaces;
using Microsoft.SPOT.Hardware;
using System.Text;
using System.IO.Ports;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Processor;

namespace BMC.Hidroponic.Device
{
    public partial class Program
    {
        static double WaterDist = 0;
        HC_SR04 DistanceSensor = new HC_SR04(ThisBoard.Socket9.Pin5, ThisBoard.Socket9.Pin6);
        //SimpleSerial UART = null;
        OutputPort relay1 = new OutputPort(ThisBoard.Socket9.Pin3, false);
        OutputPort relay2 = new OutputPort(ThisBoard.Socket9.Pin4, false);
        PhMeter phSensor = new PhMeter(ThisBoard.Socket14.AnalogInput5);
        TdsMeter Tds1 = new TdsMeter(ThisBoard.Socket14.AnalogInput3);
        DS18B20GHI Temp1 = new DS18B20GHI(ThisBoard.Socket1.Pin4);
        DS18B20GHI Temp2 = new DS18B20GHI(ThisBoard.Socket1.Pin3);
        //DS18B20 Temp3 = new DS18B20(ThisBoard.Socket1.Pin5);
        TdsMeter Tds2 = new TdsMeter(ThisBoard.Socket14.AnalogInput4);
        LogHelper logs;
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            Mainboard.LDR0.OnInterrupt += LDR0_OnInterrupt;
            Mainboard.LDR1.OnInterrupt += LDR1_OnInterrupt;
            logs = new LogHelper(usbHost);
           
            Thread thDist = new Thread(new ThreadStart(LoopDistance));
            thDist.Start();

            xBeeAdapter.Configure(9600,SerialParity.None,SerialStopBits.One,8,HardwareFlowControl.NotRequired);
            //StartLora();
            xBeeAdapter.Port.LineReceived += Port_LineReceived;
            GT.Timer timer = new GT.Timer(5000); // every second (1000ms)
            timer.Tick += timer_Tick;
            timer.Start();

            // Timeout 10 seconds
            int timeout = 1000 * 10;

            // Enable Watchdog
            GHI.Processor.Watchdog.Enable(timeout);

            // Start a time counter reset thread
            WDTCounterReset = new Thread(WDTCounterResetLoop);
            WDTCounterReset.Start();
            // Normally, you can read this flag ***ONLY ONCE*** on power up
            if (GHI.Processor.Watchdog.LastResetCause == GHI.Processor.Watchdog.ResetCause.Watchdog)
            {
                logs.WriteLogs("reset by watchdog");
                Debug.Print("Watchdog did Reset");
            }
            else
            {
                logs.WriteLogs("system reboot / start");
                Debug.Print("Reset switch or system power");
            }
        }
        static Thread WDTCounterReset;
        static void WDTCounterResetLoop()
        {
            while (true)
            {
                // reset time counter every 4 seconds
                Thread.Sleep(4000);

                GHI.Processor.Watchdog.ResetCounter();
            }
        }
        void Port_LineReceived(GT.SocketInterfaces.Serial sender, string line)
        {
            try
            {
                Debug.Print(line);
                string[] data = line.Split('|');
                switch (data[0])
                {
                    case
                    "Relay1":
                        {
                            var state = data[1].ToLower() == "true" ? true : false;
                            relay1.Write(state);
                        }
                        break;
                    case "Relay2":
                        {
                            var state = data[1].ToLower() == "true" ? true : false;
                            relay2.Write(state);
                        }
                        break;
                }
            }
            catch (Exception ex) { 
                Debug.Print(ex.ToString());
                logs.WriteLogs("relay error :" + ex);
            }
        }

      

        void LoopDistance()
        {
            while (true)
            {
                long ticks = DistanceSensor.Ping();
                if (ticks > 0L)
                {
                    WaterDist = DistanceSensor.TicksToInches(ticks);
                    //Debug.Print("Distance :" + WaterDist + "inch");
                }
                Thread.Sleep(100);
            }
        }

        void timer_Tick(GT.Timer timer)
        {
            //Debug.Print("PH :" + phSensor.PhValue);
            //Debug.Print("Tds 1 :" + Tds1.tdsValue);
            //Debug.Print("Temp 1 :" + Temp1.ConvertAndReadTemperature());
            //Debug.Print("Temp 2 :" + Temp2.ConvertAndReadTemperature());
            //Debug.Print("Temp 3 :" + Temp3.ConvertAndReadTemperature());


            var data = new SensorData()
            {
                Ph = phSensor.PhValue,
                Relay1 = relay1.Read(),
                Relay2 = relay2.Read(),
                Tds1 = Tds1.tdsValue,
                Tds2 = Tds2.tdsValue,
                Temp1 = Temp1.TempValue,//.ConvertAndReadTemperature(),
                Temp2 = Temp2.TempValue,//.ConvertAndReadTemperature(),
                //Temp3 = Temp3.ConvertAndReadTemperature(),
                WaterDist = WaterDist

            };
            var jsonStr = Json.NETMF.JsonSerializer.SerializeObject(data);
            Debug.Print("kirim :" + jsonStr);
            xBeeAdapter.Port.WriteLine(jsonStr);
            /*
            //USING LORA
            //PrintToLcd("send count: " + counter);
            sendData(jsonStr);
            Thread.Sleep(5000);
            byte[] rx_data = new byte[20];

            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read:" + hasil);

                    //mac_rx 2 AABBCC
                }
            }
            var TimeStr = DateTime.Now.ToString("dd/MM/yy HH:mm");
            //insert to db
            */






        }
        #region LORA
        /*
        void StartLora()
        {
            UART = new SimpleSerial(ThisBoard.Socket4.SerialPortName, 57600);
            UART.ReadTimeout = 0;
            UART.DataReceived += UART_DataReceived;
            Debug.Print("57600");
            Debug.Print("RN2483 Test");
            //PrintToLcd("RN2483 Test");
            OutputPort reset = new OutputPort(GHI.Pins.FEZRaptor.Socket4.Pin6, false);
            OutputPort reset2 = new OutputPort(GHI.Pins.FEZRaptor.Socket4.Pin3, false);

            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(100);
            reset.Write(false);
            reset2.Write(false);

            Thread.Sleep(100);
            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(100);

            waitForResponse();

            sendCmd("sys factoryRESET");
            sendCmd("sys get hweui");
            sendCmd("mac get deveui");
            Thread.Sleep(3000);
            // For TTN
            sendCmd("mac set devaddr AAABBBEE");  // Set own address
            Thread.Sleep(3000);
            sendCmd("mac set appskey 2B7E151628AED2A6ABF7158809CF4F3D");
            Thread.Sleep(3000);

            sendCmd("mac set nwkskey 2B7E151628AED2A6ABF7158809CF4F3D");
            Thread.Sleep(3000);

            sendCmd("mac set adr off");
            Thread.Sleep(3000);

            sendCmd("mac set rx2 3 868400000");//869525000
            Thread.Sleep(3000);

            sendCmd("mac join abp");
            Thread.Sleep(3000);
            sendCmd("mac get status");
            sendCmd("mac get devaddr");
            Thread.Sleep(2000);


        }
        private static string[] _dataInLora;
        private static string rx;


        void UART_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            _dataInLora = UART.Deserialize();
            for (int index = 0; index < _dataInLora.Length; index++)
            {
                rx = _dataInLora[index];
                //if error
                if (_dataInLora[index].Length > 5)
                {

                    //if receive data
                    if (rx.Substring(0, 6) == "mac_rx")
                    {
                        string hex = _dataInLora[index].Substring(9);

                        //update display
                        //txtMessage.Text = hex;//Unpack(hex);
                        //txtMessage.Invalidate();
                        //window.Invalidate();
                        byte[] data = StringToByteArrayFastest(hex);
                        string decoded = new String(UTF8Encoding.UTF8.GetChars(data));
                        Debug.Print("decoded:" + decoded);

                    }
                }
            }
            Debug.Print(rx);
        }

        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
        void sendCmd(string cmd)
        {
            byte[] rx_data = new byte[20];
            Debug.Print(cmd);
            Debug.Print("\n");
            // flush all data
            UART.Flush();
            // send some data
            var tx_data = Encoding.UTF8.GetBytes(cmd);
            UART.Write(tx_data, 0, tx_data.Length);
            tx_data = Encoding.UTF8.GetBytes("\r\n");
            UART.Write(tx_data, 0, tx_data.Length);
            Thread.Sleep(100);
            while (!UART.IsOpen)
            {
                UART.Open();
                Thread.Sleep(100);
            }
            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count cmd:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read cmd:" + hasil);
                }
            }
        }

        void waitForResponse()
        {
            byte[] rx_data = new byte[20];

            while (!UART.IsOpen)
            {
                UART.Open();
                Thread.Sleep(100);
            }
            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count res:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read res:" + hasil);
                }

            }
        }
        public static string Unpack(string input)
        {
            byte[] b = new byte[input.Length / 2];

            for (int i = 0; i < input.Length; i += 2)
            {
                b[i / 2] = (byte)((FromHex(input[i]) << 4) | FromHex(input[i + 1]));
            }
            return new string(Encoding.UTF8.GetChars(b));
        }
        public static int FromHex(char digit)
        {
            if ('0' <= digit && digit <= '9')
            {
                return (int)(digit - '0');
            }

            if ('a' <= digit && digit <= 'f')
                return (int)(digit - 'a' + 10);

            if ('A' <= digit && digit <= 'F')
                return (int)(digit - 'A' + 10);

            throw new ArgumentException("digit");
        }

        char getHexHi(char ch)
        {
            int nibbleInt = ch >> 4;
            char nibble = (char)nibbleInt;
            int res = (nibble > 9) ? nibble + 'A' - 10 : nibble + '0';
            return (char)res;
        }
        char getHexLo(char ch)
        {
            int nibbleInt = ch & 0x0f;
            char nibble = (char)nibbleInt;
            int res = (nibble > 9) ? nibble + 'A' - 10 : nibble + '0';
            return (char)res;
        }

        void sendData(string msg)
        {
            byte[] rx_data = new byte[20];
            char[] data = msg.ToCharArray();
            Debug.Print("mac tx uncnf 1 ");
            var tx_data = Encoding.UTF8.GetBytes("mac tx uncnf 1 ");
            UART.Write(tx_data, 0, tx_data.Length);

            // Write data as hex characters
            foreach (char ptr in data)
            {
                tx_data = Encoding.UTF8.GetBytes(new string(new char[] { getHexHi(ptr) }));
                UART.Write(tx_data, 0, tx_data.Length);
                tx_data = Encoding.UTF8.GetBytes(new string(new char[] { getHexLo(ptr) }));
                UART.Write(tx_data, 0, tx_data.Length);


                Debug.Print(new string(new char[] { getHexHi(ptr) }));
                Debug.Print(new string(new char[] { getHexLo(ptr) }));
            }
            tx_data = Encoding.UTF8.GetBytes("\r\n");
            UART.Write(tx_data, 0, tx_data.Length);
            Debug.Print("\n");
            Thread.Sleep(5000);

            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count after:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read after:" + hasil);
                }
            }
        }
         */
        #endregion
        void LDR1_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            TurnSolenoid(!relay2.Read());
        }

        void LDR0_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            TurnPump(!relay1.Read());
        }

        void TurnPump(bool State)
        {
            relay1.Write(State);
        }
        void TurnSolenoid(bool State)
        {
            relay2.Write(State);
        }
    }
    public class SensorData
    {
        public double Tds1 { get; set; }
        public double Tds2 { get; set; }
        public double Temp1 { get; set; }
        public double Temp2 { get; set; }
        public double Temp3 { get; set; }
        public double Ph { get; set; }
        public double WaterDist { get; set; }
        public bool Relay1 { get; set; }
        public bool Relay2 { get; set; }
    }
}
