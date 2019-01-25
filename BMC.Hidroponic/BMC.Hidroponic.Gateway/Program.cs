using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMC.Hidroponic.Gateway
{
    class Program
    {
        static SerialPort xBee;
        private static DeviceClient s_deviceClient;
        static bool IsConnected = false;
        private static HttpClient _client;

        public static HttpClient client
        {
            get
            {
                if (_client == null) _client = new HttpClient();
                return _client;
            }

        }
        static void Main(string[] args)
        {
            Console.WriteLine("gateway service is starting up...");
            Setup();
            StartListener();
            Console.ReadLine();
            xBee.Close();
        }
        static async void SendToPowerBI(SensorData data)
        {
            var url = ConfigurationManager.AppSettings["PowerBiUrl"];
            //SensorData2 data2 = new SensorData2() { Ph = data.Ph, Tds1 = data.Tds1, Tds2 = data.Tds2, Temp1 = data.Temp1, Temp2 = data.Temp2, Temp3 = data.Temp3, WaterDist = data.WaterDist };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content, CancellationToken.None);
            if (res.IsSuccessStatusCode)
            {
                Console.WriteLine("data sent to power bi - " + DateTime.Now);
            }
            else
            {
                Console.WriteLine("Fail to send to Power BI");
            }
        }

        static void Setup()
        {
            try
            {
                if (!IsConnected)
                {
                 
                    if (s_deviceClient != null)
                    {
                        s_deviceClient.Dispose();
                    }
                    var ConStr = ConfigurationManager.AppSettings["DeviceConStr"];
                    // Connect to the IoT hub using the MQTT protocol
                    s_deviceClient = DeviceClient.CreateFromConnectionString(ConStr, TransportType.Mqtt);
                    s_deviceClient.SetMethodHandlerAsync("DoAction", DoAction, null).Wait();
                    //SendDeviceToCloudMessagesAsync();



                    IsConnected = true;
                }
               
            }
            catch
            {

            }
        }
        private static async Task<MethodResponse> DoAction(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            var action = JsonConvert.DeserializeObject<DeviceAction>(data);
            // Check the payload is a single integer value
            if (action != null)
            {
                /*
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();
                */
                switch (action.ActionName)
                {
                    case "PlaySound":

                    case "ChangeLED":

                    case "TurnOffLED":

                    case "OpenURL":
                   
                    case "CCTVStatus":
                     
                    case "CCTVUpdateTime":
                       
                        break;
                    case "Relay1":
                        var state1 = Convert.ToBoolean(action.Params[0]);
                        xBee.WriteLine($"Relay1|{state1.ToString()}");
                        break;
                    case "Relay2":
                        var state2 = Convert.ToBoolean(action.Params[0]);
                        xBee.WriteLine($"Relay2|{state2.ToString()}");
                        break;
                }
                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 400);
            }
        }
        static void StartListener()
        {
            var port = ConfigurationManager.AppSettings["XBeePort"];
            xBee = new SerialPort(port, 9600);
            try
            {
                if (xBee.IsOpen) xBee.Close();
                xBee.Open();
                Console.WriteLine("XBEE is ready...");
                xBee.DataReceived += (object sender, SerialDataReceivedEventArgs e)
                =>
                {
                    var jsonStr = xBee.ReadLine();
                    Console.WriteLine(jsonStr);
                    var node = JsonConvert.DeserializeObject<SensorData>(jsonStr);
                    SendDeviceToCloudMessagesAsync(node);
                    SendToPowerBI(node);
                };
              
             
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async void SendDeviceToCloudMessagesAsync(SensorData data)
        {
            var message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data)));

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            message.Properties.Add("temperatureAlert1", (data.Temp1 > 40) ? "true" : "false");
            message.Properties.Add("temperatureAlert2", (data.Temp2 > 40) ? "true" : "false");
            message.Properties.Add("temperatureAlert3", (data.Temp3 > 40) ? "true" : "false");

            // Send the telemetry message
            await s_deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, "ok");

        }

    }
    public class DeviceAction
    {
        public string ActionName { get; set; }
        public string[] Params { get; set; }
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
