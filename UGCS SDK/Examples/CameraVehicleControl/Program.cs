using System;
using System.IO.Ports;
using System.Collections.Generic;
using UGCS.Sdk.Protocol;
using UGCS.Sdk.Protocol.Encoding;
using UGCS.Sdk.Tasks;
using System.Linq;
using System.Diagnostics;
using UGCS.Sdk.Diagnostics;
using System.Threading;
using System.IO;

namespace CameraVehicleControl
{

    static class Program
    { 
        static void Main()
        {
            //Connect
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("localhost", 3334);
            MessageSender messageSender = new MessageSender(tcpClient.Session);
            MessageReceiver messageReceiver = new MessageReceiver(tcpClient.Session);
            MessageExecutor messageExecutor = new MessageExecutor(messageSender, messageReceiver, new InstantTaskScheduler())
            {
                Configuration =
                    {
                        DefaultTimeout = 10000
                    }
            };
            var notificationListener = new NotificationListener();
            messageReceiver.AddListener(-1, notificationListener);
            //auth
            AuthorizeHciRequest request = new AuthorizeHciRequest
            {
                ClientId = -1,
                Locale = "en-US"
            };
            var future = messageExecutor.Submit<AuthorizeHciResponse>(request);
            future.Wait();
            AuthorizeHciResponse authorizeHciResponse = future.Value;
            int clientId = authorizeHciResponse.ClientId;
            Console.WriteLine("AuthorizeHciResponse processed");
            //login
            LoginRequest loginRequest = new LoginRequest
            {
                UserLogin = "admin",
                UserPassword = "admin",
                ClientId = clientId
            };
            var loginResponseTask = messageExecutor.Submit<LoginResponse>(loginRequest);
            loginResponseTask.Wait();
            GetObjectListRequest getObjectListRequest = new GetObjectListRequest()
            {
                ClientId = clientId,
                ObjectType = "Vehicle",
                RefreshDependencies = true
            };
            getObjectListRequest.RefreshExcludes.Add("PayloadProfile");
            getObjectListRequest.RefreshExcludes.Add("Route");
            var task = messageExecutor.Submit<GetObjectListResponse>(getObjectListRequest);
            task.Wait();
            var list = task.Value;
            foreach (var v in list.Objects)
            {
                System.Console.WriteLine(string.Format("name: {0}; id: {1}; type: {2}",
                       v.Vehicle.Name, v.Vehicle.Id, v.Vehicle.Type.ToString()));
            }
            Vehicle vehicle = task.Value.Objects.FirstOrDefault().Vehicle;
            Vehicle selectedVehicle = list.Objects.Where(v => v.Vehicle.Id == 2).Select(v => v.Vehicle).FirstOrDefault();
            List<Vehicle> vehicles = new List<Vehicle>();
            vehicles.Add(selectedVehicle);
            
            SendCommandRequest request1 = new SendCommandRequest
            {
                ClientId = authorizeHciResponse.ClientId,
                Command = new UGCS.Sdk.Protocol.Encoding.Command
                {
                    Code = "manual",
                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                    Silent = true,
                    ResultIndifferent = true
                },
                //Vehicles = new List<Vehicle>() { selectedVehicle }
            };

            SendCommandRequest request2 = new SendCommandRequest
            {
                ClientId = authorizeHciResponse.ClientId,
                Command = new UGCS.Sdk.Protocol.Encoding.Command
                {
                    Code = "auto",
                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                    Silent = true,
                    ResultIndifferent = true
                },
                //Vehicles = new List<Vehicle>() { selectedVehicle }
            };
            //Vehicle selectedVehicle = list.Objects.Where(v => v.Vehicle.Id == 2).Select(v => v.Vehicle).FirstOrDefault();
            request1.Vehicles.Add(selectedVehicle);
            request2.Vehicles.Add(selectedVehicle);
            //request2.Vehicles.Add(new Vehicle() { Id = vehicle.Id });
            //List<CommandArgument> list2 = new List<CommandArgument>();
            //list2.Add(new CommandArgument { Code = "relativeAltitude", Value = new Value() { StringValue = Settings.Default.MinimumOperatingAltitude.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } });
            //request2.Command.Arguments.AddRange(list2);
            //tcpClient.Close();
            //messageSender.Cancel();
            // messageExecutor.Close();
            //notificationListener.Dispose();

            //Read serial input from telemetry module

            SerialPort mySerialPort = new SerialPort("COM13")
            {
                BaudRate = 57600,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None
            };



            //mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            try
            {   
                while (mySerialPort.IsOpen == false) {
                    try
                    {
                        mySerialPort.Open();
                        System.Console.WriteLine(string.Format("Serial Connection Successful to {0}", mySerialPort.PortName));
                        break;
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine(ex); 
                    }
                }
                

                while(true)
                {
                    string line = mySerialPort.ReadExisting();
                    if (line.StartsWith("1"))
                    {
                        string dist = line.Substring(1);
                        Console.WriteLine($"Obstacle detected at {dist}m. Switch to Manual Mode.");
                        var future1 = messageExecutor.Submit<SendCommandResponse>(request1);
                        future1.Wait();
                    }
                    else if (line == "2")
                    {
                        Console.WriteLine("Switch to Auto Mode");
                        var future2 = messageExecutor.Submit<SendCommandResponse>(request2);
                        future2.Wait();
                    }
                    else if (line == "3")
                    {
                        Console.WriteLine("Switch to Manual Mode and close port.");
                        var future1 = messageExecutor.Submit<SendCommandResponse>(request1);
                        break;
                    }
                }
                mySerialPort.Close();
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }
        }

        /*
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine(indata);
            
            if (indata == "1")
            {
                manual_flag = true;
            }                  
        }
        */
    }   
}