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
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT.Hardware;
using GHI.Glide;
using GHI.Glide.Geom;
using GHI.Processor;
using Gadgeteer.SocketInterfaces;
using BogTechFlush;
using GHI.Glide.UI;

namespace BMC.Hidroponic.Simulator
{//driver for touch screen
    public class CapacitiveTouchController
    {
        private InterruptPort touchInterrupt;
        private I2CDevice i2cBus;
        private I2CDevice.I2CTransaction[] transactions;
        private byte[] addressBuffer;
        private byte[] resultBuffer;

        private static CapacitiveTouchController _this;

        public static void Initialize(Cpu.Pin PortId)
        {
            if (_this == null)
                _this = new CapacitiveTouchController(PortId);
        }

        private CapacitiveTouchController()
        {
        }

        private CapacitiveTouchController(Cpu.Pin portId)
        {
            transactions = new I2CDevice.I2CTransaction[2];
            resultBuffer = new byte[1];
            addressBuffer = new byte[1];
            i2cBus = new I2CDevice(new I2CDevice.Configuration(0x38, 400));
            touchInterrupt = new InterruptPort(portId, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
            touchInterrupt.OnInterrupt += (a, b, c) => this.OnTouchEvent();
        }

        private void OnTouchEvent()
        {
            for (var i = 0; i < 5; i++)
            {
                var first = this.ReadRegister((byte)(3 + i * 6));
                var x = ((first & 0x0F) << 8) + this.ReadRegister((byte)(4 + i * 6));
                var y = ((this.ReadRegister((byte)(5 + i * 6)) & 0x0F) << 8) + this.ReadRegister((byte)(6 + i * 6));

                if (x == 4095 && y == 4095)
                    break;

                if (((first & 0xC0) >> 6) == 1)
                    GlideTouch.RaiseTouchUpEvent(null, new GHI.Glide.TouchEventArgs(new Point(x, y)));
                else
                    GlideTouch.RaiseTouchDownEvent(null, new GHI.Glide.TouchEventArgs(new Point(x, y)));
            }
        }

        private byte ReadRegister(byte address)
        {
            this.addressBuffer[0] = address;

            this.transactions[0] = I2CDevice.CreateWriteTransaction(this.addressBuffer);
            this.transactions[1] = I2CDevice.CreateReadTransaction(this.resultBuffer);

            this.i2cBus.Execute(this.transactions, 1000);

            return this.resultBuffer[0];
        }
    }
    public partial class Program
    {
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            setup();
        }
        static DataGrid GvData;
        static int counter;
        static bool IsEmpty = true;
        static GHI.Glide.Display.Window MainWindow;
        //static GHI.Glide.UI.Image img;
        //static GHI.Glide.UI.Button btn;
        static GHI.Glide.UI.TextBlock txt;
        static Bitmap picAvail, picNotAvail;
        static I2CDevice i2cDevice;
        static CapTouchDriver CapDriver;
        static Random rnd = new Random();
        void setup()
        {
            //rnd = new Random();
            Display.Width = 800;

            Display.Height = 480;

            Display.HorizontalSyncPulseWidth = 1;

            Display.HorizontalBackPorch = 88;

            Display.HorizontalFrontPorch = 40;

            Display.VerticalSyncPulseWidth = 3;

            Display.VerticalBackPorch = 32;

            Display.VerticalFrontPorch = 13;

            Display.PixelClockRateKHz = 25000;

            Display.OutputEnableIsFixed = true;

            Display.OutputEnablePolarity = true;

            Display.HorizontalSyncPolarity = false;

            Display.VerticalSyncPolarity = false;

            Display.PixelPolarity = true;

            Display.Type = Display.DisplayType.Lcd;



            if (Display.Save())      // Reboot required?
            {

                PowerState.RebootDevice(false);

            }

            //init touch

            i2cDevice = new I2CDevice(new I2CDevice.Configuration(0x38, 400));

            CapDriver = new CapTouchDriver(i2cDevice);

            CapDriver.SetBacklightTime(0);

            CapDriver.ResetBacklight();
            

            //CapacitiveTouchController.Initialize(GHI.Pins.G120.P2_21);
            
            //CapacitiveTouchController.Initialize(GHI.Pins.FEZCobraII.Socket4.Pin3);
            
            GlideTouch.Initialize();

            MainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.Form1));
            //img = (GHI.Glide.UI.Image)MainWindow.GetChildByName("img");
            //btn = (GHI.Glide.UI.Button)MainWindow.GetChildByName("BtnChange");
            txt = (GHI.Glide.UI.TextBlock)MainWindow.GetChildByName("txtStatus");
            //GT.Picture pic = new GT.Picture(Resources.GetBytes(Resources.BinaryResources.empty), GT.Picture.PictureEncoding.JPEG);
            //img.Bitmap = pic.MakeBitmap();
            
            GvData = (DataGrid)MainWindow.GetChildByName("GvData");
            GvData.AddColumn(new DataGridColumn("Time", 100));
            GvData.AddColumn(new DataGridColumn("Temp 1", 100));
            GvData.AddColumn(new DataGridColumn("Temp 2", 100));
            GvData.AddColumn(new DataGridColumn("Water Dist", 100));
            GvData.AddColumn(new DataGridColumn("Tds 1", 100));
            GvData.AddColumn(new DataGridColumn("Tds 2", 100));

            Random rnd = new Random();
            counter = 0;
            
          
            Glide.MainWindow = MainWindow;
            //btn.ReleaseEvent += btn_ReleaseEvent;

            Glide.FitToScreen = true;
            
            //Thread th1 = new Thread(new ThreadStart(LoopButton));
            //th1.Start();
            //Mainboard.LDR0.OnInterrupt += LDR0_OnInterrupt;
            //Mainboard.LDR1.OnInterrupt += LDR1_OnInterrupt;
            //logs = new LogHelper(usbHost);


            xBeeAdapter.Configure(9600, SerialParity.None, SerialStopBits.One, 8, HardwareFlowControl.NotRequired);
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
                //logs.WriteLogs("reset by watchdog");
                Debug.Print("Watchdog did Reset");
            }
            else
            {
                //logs.WriteLogs("system reboot / start");
                Debug.Print("Reset switch or system power");
            }
        
        }

        void btn_ReleaseEvent(object sender)
        {
            changeState();
        }

        void LoopButton()
        {
            while (true)
            {
                if (!Mainboard.LDR0.Read())
                {
                    changeState();
                }
                Thread.Sleep(200);
            }
        }
        void changeState()
        {
         
        }

        void btn_TapEvent(object sender)
        {
            changeState();
        }

        static double WaterDist = 0;
        
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
                            Debug.Print("relay 1:"+state);
                        }
                        break;
                    case "Relay2":
                        {
                            var state = data[1].ToLower() == "true" ? true : false;
                            Debug.Print("relay 2:" + state);
                        }
                        break;
                }
            }
            catch (Exception ex) { 
                Debug.Print(ex.ToString());
                //logs.WriteLogs("relay error :" + ex);
            }
        }




        void timer_Tick(GT.Timer timer)
        {

            var data = new SensorData()
            {
                Ph = 1 + rnd.Next(10),
                Relay1 = rnd.Next(2) > 0 ? true : false,
                Relay2 = rnd.Next(2) > 0 ? true : false,
                Tds1 = 1 + rnd.Next(100),
                Tds2 = 1 + rnd.Next(100),
                Temp1 = 25 + rnd.Next(7),//.ConvertAndReadTemperature(),
                Temp2 = 25 + rnd.Next(8),//.ConvertAndReadTemperature(),
                //Temp3 = Temp3.ConvertAndReadTemperature(),
                WaterDist = 1 + rnd.Next(10)

            };
            //insert to db
            var item = new DataGridItem(new object[] { DateTime.Now.ToString("HH:mm:ss"), data.Temp1.ToString("n2") + "C", data.Temp2.ToString("n2") + "C", data.WaterDist.ToString("n2") + "CM", data.Tds1.ToString("n2"), data.Tds2.ToString("n2") });
            //add data to grid
            GvData.AddItem(item);
            GvData.Invalidate();
            if (counter++ > 9)
            {
                counter = 0;
                GvData.Clear();
            }

            var jsonStr = Json.NETMF.JsonSerializer.SerializeObject(data);
            Debug.Print("kirim :" + jsonStr);
            xBeeAdapter.Port.WriteLine(jsonStr);




        }
        void LDR1_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            //TurnSolenoid(!relay2.Read());
        }

        void LDR0_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            //TurnPump(!relay1.Read());
        }

        void TurnPump(bool State)
        {
            //relay1.Write(State);
        }
        void TurnSolenoid(bool State)
        {
            //relay2.Write(State);
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
