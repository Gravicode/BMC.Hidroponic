//using Microsoft.Azure.Devices.Client;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Comfile.ComfilePi;
//using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace BMC.Hidroponic.Gateway
{
    class Program
    {
        static LinuxSerialPort xBee;//LinuxSerialPort
       // private static DeviceClient s_deviceClient;
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
        
        static MqttClient MqttClient;
        const string DataTopic = "bmc/hidroponic/data";
        const string ControlTopic = "bmc/hidroponic/control";
        public static void PublishMessage(string Message)
        {
            MqttClient.Publish(DataTopic, Encoding.UTF8.GetBytes(Message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }
        static void SetupMqtt()
        {
            string IPBrokerAddress = ConfigurationManager.AppSettings["MqttHost"];
            string ClientUser = ConfigurationManager.AppSettings["MqttUser"];
            string ClientPass = ConfigurationManager.AppSettings["MqttPass"];

            MqttClient = new MqttClient(IPAddress.Parse(IPBrokerAddress));

            // register a callback-function (we have to implement, see below) which is called by the library when a message was received
            MqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            MqttClient.Subscribe(new string[] { ControlTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            // use a unique id as client id, each time we start the application
            var clientId = "bmc-hidroponic";//Guid.NewGuid().ToString();

            MqttClient.Connect(clientId, ClientUser,ClientPass);
            Console.WriteLine("MQTT is connected");
        } // this code runs when a message was received
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            var msg = JsonConvert.DeserializeObject<DeviceAction>(ReceivedMessage);
            if (e.Topic == ControlTopic)
            {
                //var datastr = ReceivedMessage.Split(':');
                switch (msg.ActionName)
                {
                    case "Relay1":
                        var state1 = Convert.ToBoolean(msg.Params[0]);
                        xBee.WriteLine($"Relay1|{state1.ToString()}");
                        break;
                    case "Relay2":
                        var state2 = Convert.ToBoolean(msg.Params[0]);
                        xBee.WriteLine($"Relay2|{state2.ToString()}");
                        break;
                }
              
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("gateway service is starting up...");
            SetupMqtt();
            //Setup();
            
            StartListener();
            Console.ReadLine();
            xBee.Close();
        }
        static async void SendToPowerBI(SensorData data)
        {
            //"https://api.powerbi.com/beta/e4a5cd36-e58f-4f98-8a1a-7a8e545fc65a/datasets/fdd35f45-854c-48b1-847b-1d0db62076cf/rows?key=C%2FnPcyGr4xDAHDmhgtX1AHEPtrU225NnQExPv%2FBOvPQowBDXQ674MFahutRyCpo0LZmo3BZerFvQE6M8UJ46XA%3D%3D"
            var url = ConfigurationManager.AppSettings["PowerBiUrl"];
            //SensorData2 data2 = new SensorData2() { Ph = data.Ph, Tds1 = data.Tds1, Tds2 = data.Tds2, Temp1 = data.Temp1, Temp2 = data.Temp2, Temp3 = data.Temp3, WaterDist = data.WaterDist };
            var data2 = new SensorData2() { Ph=data.Ph, Relay1=data.Relay1.ToString(), Relay2=data.Relay2.ToString(), Tds1=data.Tds1, Tds2=data.Tds2, Temp1=data.Temp1,  Temp2=data.Temp2, Temp3=data.Temp3, WaterDist=data.WaterDist, TimeStamp=DateTime.Now };
          
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.DateFormatString = "yyyy-MM-ddThh:mm:ss.fffZ";
            Console.WriteLine(JsonConvert.SerializeObject(data2, jsonSettings));
            var content = new StringContent(JsonConvert.SerializeObject(data2, jsonSettings), Encoding.UTF8, "application/json");
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
        /*
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
                    s_deviceClient = DeviceClient.CreateFromConnectionString(ConStr, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
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
        }*/
        static string TempData = "";
        static void StartListener()
        {
            var port = ConfigurationManager.AppSettings["XBeePort"];
            //LinuxSerialPort
            xBee = new LinuxSerialPort(port, 9600,System.IO.Ports.Parity.None,8,System.IO.Ports.StopBits.One);
            try
            {
                if (xBee.IsOpen)
                    xBee.Close();
                xBee.Open();
                Console.WriteLine("XBEE is ready...");
                xBee.DataReceived += (object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
                =>
                {
                   
                    try
                    {
                        var jsonStr = xBee.ReadLine();//TempData;
                        Console.WriteLine(jsonStr);
                        var node = JsonConvert.DeserializeObject<SensorData>(jsonStr);
                        
                        if (node != null)
                        {
                            //SendDeviceToCloudMessagesAsync(node);
                            PublishMessage(jsonStr);
                            SendToPowerBI(node);
                        }
                        else
                        {
                            Console.WriteLine("serialize to json failed");
                        }
                       
                    }catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                 
                    
                };
              
             
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static string ToStr(byte[] bytes)
        {
            string hexString = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString += bytes[i].ToString("X2");
            }
            return hexString;
        }
        /*
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

        }*/

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
    public class SensorData2
    {
        public double Tds1 { get; set; }
        public double Tds2 { get; set; }
        public double Temp1 { get; set; }
        public double Temp2 { get; set; }
        public double Temp3 { get; set; }
        public double Ph { get; set; }
        public double WaterDist { get; set; }
        public string Relay1 { get; set; }
        public string Relay2 { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
