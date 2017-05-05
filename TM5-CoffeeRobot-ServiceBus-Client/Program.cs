using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Azure.Devices;
using Newtonsoft.Json.Linq;

namespace TM5_CoffeeRobot_ServiceBus_Client
{
    class DeviceMessage
    {
        public String deviceId { get; set; } //target device id
        public String messageToDevice { get; set; } // what message to target device
    }
        //when receiving the message from service bus
        //1.parse stringjson
        //2.parse message to DeviceMessage type
        //3.send message accroding device id 
    class Program
    {
        static ServiceClient serviceClient;
        static QueueClient queueClient;
        static String servicebus_connectionString = "Endpoint=sb://tm-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=FtMZBn+Y+oX9LPGYF1oV+PFKLRs/iWTBhk6AEtkY5aA=";
        static String iothub_connectionString = "HostName=TM-demo.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+6JO6PrZz1cMo7fEp6s4bJ+F+UifliQTXyupX1G/ehQ=";
        static void Main(string[] args)
        {
            
            var queueName = "tm-queue";
            queueClient = QueueClient.CreateFromConnectionString(servicebus_connectionString, queueName);
            serviceClient = ServiceClient.CreateFromConnectionString(iothub_connectionString);
            // Build the messaging options.
            var eventDrivenMessagingOptions = new OnMessageOptions();
            eventDrivenMessagingOptions.AutoComplete = true;
            eventDrivenMessagingOptions.ExceptionReceived += OnExceptionReceived;
            eventDrivenMessagingOptions.MaxConcurrentCalls = 5;
            queueClient.OnMessage(OnMessageArrived, eventDrivenMessagingOptions);

            Console.WriteLine("Start ServiceBus Message Listing...");
            //avoid app finish
            while (true)
                Console.ReadLine();
        }
        private static void OnMessageArrived(BrokeredMessage message)
        {
            
            //msgBody is the json content upload by device, so we can reference format from decvice code
            String msgBody = message.GetBody<String>();
            //Console.WriteLine(String.Format("Message body: {0}", msgBody));
            System.Diagnostics.Debug.Print(String.Format("Message body{0}", msgBody));
            JObject jObject = JObject.Parse(msgBody); //try-catch
            String deviceId = "";
            if (jObject["DeviceId"] != null)
                 deviceId = jObject["DeviceId"].ToString();
            //use the function formatMessage to generate DeviceMessage from "deviceId" and "jObect"
            DeviceMessage deviceMessage = formatMessage(deviceId,jObject);
            System.Diagnostics.Debug.Print("C2D Message:"+msgBody);
            //starting send c2d Message
            try
            {
                if (deviceMessage != null)
                {
                    Console.WriteLine("Send Cloud-to-Device message to "+ deviceMessage.deviceId);
                    SendCloudToDeviceMessageAsync(deviceMessage.deviceId, deviceMessage.messageToDevice).Wait();
                    Console.WriteLine("Send commplete\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occur: " + ex.Message);
                Console.WriteLine("data: " + ex.Data);
            }
        }

        private static DeviceMessage formatMessage(string deviceId, JObject jObject)
        {
            DeviceMessage deviceMessage = new DeviceMessage();
            if (deviceId.Equals("TM5-coffee-maker"))
            {
                var message = new
                {
                    status = jObject["Status"]
                };
                deviceMessage.deviceId = "TM5-coffee-waiter";
                deviceMessage.messageToDevice = JsonConvert.SerializeObject(message);
                return deviceMessage;
            }
            else if (deviceId.Equals("TM5-coffee-waiter"))
            {
                var message = new
                {
                    Name = "Run",
                    Parameters = new { }
                };
                deviceMessage.deviceId = "TM5-coffee-maker";
                deviceMessage.messageToDevice = JsonConvert.SerializeObject(message);
                return deviceMessage;
            }
            else
            {
                return null;
            }
        }

        static void OnExceptionReceived(object sender, ExceptionReceivedEventArgs e)
        {
            if (e != null && e.Exception != null)
            {
                Console.WriteLine(" > Exception received: {0}", e.Exception.Message);
            }
        }
        //send c2d message to devicce by ServiceClient object 
        private async static Task SendCloudToDeviceMessageAsync(String deviceid, String msgToDevice)
        {
            try
            {
                var commandMessage = new Message(Encoding.ASCII.GetBytes(msgToDevice));
                await serviceClient.SendAsync(deviceid, commandMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Inner Exception Occur: " + ex.Message);
                Console.WriteLine("data: " + ex.Data);
            }
        }
    }
}
