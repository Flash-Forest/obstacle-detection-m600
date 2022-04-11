﻿using System;
using System.Collections.Generic;
using UGCS.Sdk.Protocol;
using UGCS.Sdk.Protocol.Encoding;
using UGCS.Sdk.Tasks;

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
            Console.WriteLine("AuthorizeHciResponse precessed");

            //login
            LoginRequest loginRequest = new LoginRequest
            {
                UserLogin = "admin",
                UserPassword = "admin",
                ClientId = clientId
            };
            var loginResponseTask = messageExecutor.Submit<LoginResponse>(loginRequest);
            loginResponseTask.Wait();

            var req = new GetObjectListRequest
            {
                ClientId = clientId,
                ObjectType = typeof(Vehicle).Name,
                RefreshDependencies = false,
            };
            var res = messageExecutor.Submit<GetObjectListResponse>(req);
            res.Wait();

            foreach (var vehicle in res.Value.Objects)
            {
                Console.WriteLine("id: {0} vehicle: {1}", vehicle.Vehicle.Id, vehicle.Vehicle.Name);
            }
            
            Console.WriteLine("Specify Vehicle ID to connect");
            string s = Console.ReadLine();
            if (s == null)
            {
                return;
            }

            int vehicleId = Int32.Parse(s);
            // Id of the emu-copter is 2
            var vehicleToControl = new Vehicle { Id = vehicleId };

            Console.WriteLine("Vehicle ID {0} selected", vehicleId);
            
            bool command = true;
            while (command)
            {

                Console.WriteLine("Specify camera_attitude_command parameters. Hit x then enter = Exit app.");
                Console.WriteLine("Specify pitch (float). example 1.4");
                s = Console.ReadLine();
                if (s == null || s == "x")
                {
                    command = false;
                    continue;
                }
                float pitch = float.Parse(s);
                
                Console.WriteLine("Specify roll (float)");
                s = Console.ReadLine();
                if (s == null || s == "x")
                {
                    command = false;
                    continue;
                }
                float roll = float.Parse(s);
                
                Console.WriteLine("Specify yaw (float)");
                s = Console.ReadLine();
                if (s == null || s == "x")
                {
                    command = false;
                    continue;
                }
                float yaw = float.Parse(s);
                
                Console.WriteLine("Specify zoom (int)");
                s = Console.ReadLine();
                if (s == null || s == "x")
                {
                    command = false;
                    continue;
                }
                int zoom = Int32.Parse(s);
                
                SendCommandRequest cameraAttitude = new SendCommandRequest
                {
                    ClientId = clientId,
                    Command = new Command
                    {
                        Code = "camera_attitude_command",
                        Subsystem = Subsystem.S_CAMERA,
                        Silent = false,
                        ResultIndifferent = false
                    }
                };
                cameraAttitude.Vehicles.Add(vehicleToControl);

                //List of current joystick values to send to vehicle.
                List<CommandArgument> listCameraAttitude = new List<CommandArgument>
                {
                    new CommandArgument
                    {
                        Code = "pitch",
                        Value = new Value { FloatValue = pitch }
                    },
                    new CommandArgument
                    {
                        Code = "roll",
                        Value = new Value { FloatValue = roll }
                    },
                    new CommandArgument
                    {
                        Code = "yaw",
                        Value = new Value { FloatValue = yaw }
                    },
                    new CommandArgument
                    {
                        Code = "zoom",
                        Value = new Value { IntValue = zoom }
                    }
                };

                cameraAttitude.Command.Arguments.AddRange(listCameraAttitude);

                var sendCameraAttitudeResponse = messageExecutor.Submit<SendCommandResponse>(cameraAttitude);
                sendCameraAttitudeResponse.Wait();

                Console.WriteLine("camera_attitude_command executed");
            }

            tcpClient.Close();
            messageSender.Cancel();
            messageReceiver.Cancel();
            messageExecutor.Close();
            notificationListener.Dispose();
        }
    }
}
