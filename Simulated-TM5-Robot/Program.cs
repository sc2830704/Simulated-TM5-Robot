using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using Microsoft.Azure.Devices.Client;

namespace Simulated_TM5_Robot
{
    class Counter
    {
        public int count;
        public int preStatus = -1;
        public Counter (){
            count = 0;
        }
    }
    class RobotMessage
    {
        public String DeviceId;
        public int Status;
        public int StatusID;

    }
    class Program
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "TM-demo.azure-devices.net";
        static string deviceKey = "ZHra8y7KsZNMquvL68TCc7fMz028vqAe6MSfOFYX56U=";
        static string deviceId = "TM5-coffee-maker";
        //static string deviceKey = "kGR8U/ZYhz74OV6Z5f5fSUrm01gNoljz5oMnI2lvMPY=";
        //static string deviceId = "TM5-coffee-test";
        static Timer timer1,timer2;
        static RobotMessage robotMessage = new RobotMessage();
        public static bool isRunning = false;
        public static string preStatus = "";
        public static string curStatus = "connect";
        private static readonly string MESSAGE_PROPERTY_MESSAGE_CATALOG_ID = "MessageCatalogId";
        public static string datePatt = @"yyyy-MM-ddTHH:mm:ss";
        class RobotMessage
        {
            public String Status = "Standby";
            public int companyId = 5;
            public string msgTimestamp;
            public string equipmentId = "TMCoffeeRobot";
            public int equipmentRunStatus = 1;
        }
        public static async void TimerCallback(Object state)
        {
            int status = ((Counter)state).count % 3;

            if (isRunning)
            {
                //do something
                Console.WriteLine("Take some time to make coffee!");
                //Thread.Sleep(3000);
                curStatus = "completed";
                robotMessage.Status = "completed";
                isRunning = false;
            }
            else
            {
                curStatus = "Standby";
                robotMessage.Status = "Standby";
            }
            
            if(robotMessage.Status!=preStatus)
            {
                preStatus = curStatus;
                Console.WriteLine("start sending message...");
                robotMessage.msgTimestamp = DateTime.UtcNow.ToString(datePatt);
                var messageString = JsonConvert.SerializeObject(robotMessage);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add(MESSAGE_PROPERTY_MESSAGE_CATALOG_ID, "73");
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

            }
            ((Counter)state).preStatus = status;
        }
        public static async void TimerCallback_Receiver(Object state)
        {
            //接收來自IotHub的c2d message
            Message receivedMessage = await deviceClient.ReceiveAsync();
            if (receivedMessage != null)
            {
                ((Counter)state).count++;
                String msg = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Get Message:" + msg);
                isRunning = true;
                await deviceClient.CompleteAsync(receivedMessage);
            }
        }
        static void Main(string[] args)
        {
            Counter counter = new Counter();
            //deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
            deviceClient = DeviceClient.CreateFromConnectionString(deviveConnStr);
            timer1 = new Timer(TimerCallback, counter, 3000, 10000);
            timer2 = new Timer(TimerCallback_Receiver, counter, 3000, 1000);
            connect();
            while (true)
            {
                //receive();
            }
        }

        private static async void receive()
        {
            Message receivedMessage = await deviceClient.ReceiveAsync();
            if (receivedMessage != null)
            {
                String msg = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Get Message:" + msg);
                robotMessage.Status = new Random().Next().ToString();
                await deviceClient.CompleteAsync(receivedMessage);
            }
        }
        private static async void connect()
        {
            Console.WriteLine("start sending message...");
            robotMessage.msgTimestamp = DateTime.UtcNow.ToString(datePatt);
            var messageString = JsonConvert.SerializeObject(robotMessage);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            message.Properties.Add(MESSAGE_PROPERTY_MESSAGE_CATALOG_ID, "73");
            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
        }
    }
}
