using System.Net.Sockets;
using System.Timers;
using System.Xml.Serialization;
using CoreOSC;
using CoreOSC.IO;
using Terminal.Gui;
using VRC.OSCQuery;

namespace ConfigXML
{
    public class Osc
    {
        public UdpClient oscClient = new();
        public OSCQueryService oscQuery;

        private ProgramConfig config;
        //constructor
        public Osc(ProgramConfig conf)
        {
            config = conf ?? throw new ArgumentNullException(nameof(conf));

            oscQuery = new OSCQueryServiceBuilder()
                // First, modify class' properties.
                .WithTcpPort(Extensions.GetAvailableTcpPort())
                .WithServiceName("VRCTally")
                // Then activate server with this function
                .WithDefaults()
                .Build();

            FindVRChatOSC().Wait();

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(UpdateOSC);
            timer.Interval = config.Osc.UpdateRate;
            timer.Start();

            //heartbeat timer is seperate since it should always be running at a set rate
            System.Timers.Timer heartbeat = new System.Timers.Timer();
            heartbeat.Elapsed += new ElapsedEventHandler(UpdateHeartbeat);
            heartbeat.Interval = 500;
            heartbeat.Start();
        }

        private async void UpdateHeartbeat(object? source, ElapsedEventArgs e)
        {
            config.Osc.parameters.Heartbeat.Value = !config.Osc.parameters.Heartbeat.Value;
            await SendOSC(config.Osc.parameters.Heartbeat, BoolToValue(config.Osc.parameters.Heartbeat.Value));
            
            ProgramWindow.InvokeApplicationRefresh();
        }

        private async void UpdateOSC(object? source, ElapsedEventArgs e)
        {
            //send all the parameters
            await SendOSC(config.Osc.parameters.Preview, BoolToValue(config.Osc.parameters.Preview.Value));
            await SendOSC(config.Osc.parameters.Program, BoolToValue(config.Osc.parameters.Program.Value));
            await SendOSC(config.Osc.parameters.Standby, BoolToValue(config.Osc.parameters.Standby.Value));
            await SendOSC(config.Osc.parameters.Error, BoolToValue(config.Osc.parameters.Error.Value));

            ProgramWindow.InvokeApplicationRefresh();
        }

        private async Task FindVRChatOSC()
        {
            string IP = "127.0.0.1";
            int port = 9000;
            oscClient.Connect(IP, port);
            return;
            
            List<OSCQueryServiceProfile> services =
            [
                //add all to a list
                .. oscQuery.GetOSCQueryServices(),
            ];

            //we now need to find the specific connection for VRChat
            foreach (var service in services)
            {
                //check if it has the endpoint we need
                var tree = await Extensions.GetOSCTree(service.address, service.port);
                if (tree.GetNodeWithPath("/chatbox/input") != null) //this is just a endpoint we know *has* to exist in VRChat
                {
                    //get host info
                    HostInfo Hostinfo = await Extensions.GetHostInfo(service.address, service.port);
                    //setup OSC client
                    string IP = Hostinfo.oscIP;
                    int port = Hostinfo.oscPort;
                    oscClient.Connect(IP, port);
                    return;
                }
            }
        }

        private async Task SendOSC<T>(ProgramConfig.OscConfig.Parameters.Parameter<T> parameter, params object[] value)
        {
            try {
                foreach (var addr in parameter.Addresses)
                {
                    var message = new OscMessage(addr, value);
                    await oscClient.SendMessageAsync(message);
                }
            }
            catch
            {
                await FindVRChatOSC();
            }
        }

        /// <summary>
        /// Convert a boolean to a OSC value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object BoolToValue(bool value)
        {
            return value ? OscTrue.True : OscFalse.False;
        }

        public FrameView GetWindow(Pos x, Pos y, Dim width, Dim height)
        {
            //setup two subviews, one for OSC and one for VMix
        FrameView oscView =
            new("OSC")
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            };

        var oscQueryInfo = new Label("Hello, world!") { X = 0, Y = 0, };
        oscQueryInfo.DrawContent += (e) =>
        {
            oscQueryInfo.Text =
                $"OSCQuery Service running at TCP {oscQuery.TcpPort} and UDP {oscQuery.TcpPort}";
        };
        oscView.Add(oscQueryInfo);

        var oscConnectionInfo = new Label("Hello, world!") { Y = Pos.Bottom(oscQueryInfo), };
        oscConnectionInfo.DrawContent += (e) =>
        {
            if (oscClient.Client.Connected)
            {
                oscConnectionInfo.Text =
                    $"VRChat OSC Client running at {oscClient.Client.RemoteEndPoint}";
            }
            else
            {
                oscConnectionInfo.Text = "VRChat OSC Client not connected!";
            }
        };
        oscView.Add(oscConnectionInfo);

        //we want to add a sub view that shows all the parameters
        oscView.Add(
            config.Osc.parameters.GetWindow(
                0,
                Pos.Bottom(oscConnectionInfo),
                Dim.Fill(),
                Dim.Fill()
            )
        );

        return oscView;
        }
    }
}
