using System.Net.Sockets;
using System.Timers;
using System.Xml.Serialization;
using CoreOSC;
using CoreOSC.IO;
using Terminal.Gui;
using VRC.OSCQuery;

namespace ConfigXML
{
    [XmlRoot(ElementName = "osc")]
    public class Osc
    {
        [XmlElement(ElementName = "parameters")]
        public required Parameters Parameters { get; set; }
        [XmlElement(ElementName = "updaterate")]
        public required int UpdateRate { get; set; }

        [XmlIgnore]
        public UdpClient oscClient = new();
        [XmlIgnore]
        public OSCQueryService oscQuery;

        //constructor
        public Osc()
        {
            oscQuery = new OSCQueryServiceBuilder()
                // First, modify class' properties.
                .WithTcpPort(Extensions.GetAvailableTcpPort())
                .WithServiceName("VRCTally")
                // Then activate server with this function
                .WithDefaults()
                .Build();

            FindVRChatOSC().Wait();
        }

        public void StartTimers()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(UpdateOSC);
            timer.Interval = ProgramWindow.config.Osc.UpdateRate;
            timer.Start();

            //heartbeat timer is seperate since it should always be running at a set rate
            System.Timers.Timer heartbeat = new System.Timers.Timer();
            heartbeat.Elapsed += new ElapsedEventHandler(UpdateHeartbeat);
            heartbeat.Interval = 500;
            heartbeat.Start();
        }

        private async void UpdateHeartbeat(object source, ElapsedEventArgs e)
        {
            Parameters.Heartbeat.Value = !Parameters.Heartbeat.Value;
            await SendOSC(Parameters.Heartbeat, BoolToValue(Parameters.Heartbeat.Value));
        }

        private async void UpdateOSC(object source, ElapsedEventArgs e)
        {
            //send all the parameters
            await SendOSC(Parameters.Preview, BoolToValue(Parameters.Preview.Value));
            await SendOSC(Parameters.Program, BoolToValue(Parameters.Program.Value));
            await SendOSC(Parameters.Standby, BoolToValue(Parameters.Standby.Value));
            await SendOSC(Parameters.Error, BoolToValue(Parameters.Error.Value));
        }

        private async Task FindVRChatOSC()
        {
            //oscClient = new("127.0.0.1", 9000);

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
                    VRC.OSCQuery.HostInfo Hostinfo = await Extensions.GetHostInfo(service.address, service.port);
                    //setup OSC client
                    string IP = Hostinfo.oscIP;
                    int port = Hostinfo.oscPort;
                    oscClient = new(IP, port);
                }
            }
        }

        private async Task SendOSC<T>(Parameter<T> parameter, params object[] value)
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

        public Window GetWindow(Pos x, Pos y, Dim width, Dim height)
        {
            //setup two subviews, one for OSC and one for VMix
        Window oscView =
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
            Parameters.GetWindow(
                0,
                Pos.Bottom(oscConnectionInfo),
                Dim.Fill(),
                Dim.Fill()
            )
        );

        return oscView;
        }
    }

    public class Parameter<T>
    {
        public const string avatarParamPrefix = "/avatar/parameters/";

        [XmlElement(ElementName = "parameter")]
        public required string[] Str { get; set; }
        public Address[] Addresses => Str.Select(s => new Address(avatarParamPrefix + s)).ToArray();

        public required T Value { get; set; }

        public Label GetLabel(string name, Pos x, Pos y)
        {
            var lbl = new Label("Preview")
            {
                X = x,
                Y = y,
            };
            lbl.DrawContent += (e) =>
            {
                lbl.Text = $"{name}: {Value}";
                foreach (var addr in Str)
                {
                    lbl.Text += $"\n    {addr}";
                }
            };

            return lbl;
        }
    }

    [XmlRoot(ElementName = "parameters")]
    public class Parameters
    {
        [XmlElement(ElementName = "preview")]
        public required Parameter<bool> Preview { get; set; }

        [XmlElement(ElementName = "program")]
        public required Parameter<bool> Program { get; set; }

        [XmlElement(ElementName = "standby")]
        public required Parameter<bool> Standby { get; set; }

        [XmlElement(ElementName = "heartbeat")]
        public required Parameter<bool> Heartbeat { get; set; }

        [XmlElement(ElementName = "error")]
        public required Parameter<bool> Error { get; set; }

        public View GetWindow(Pos x, Pos y, Dim width, Dim height)
        {
            Window paramView = new("Parameters")
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            };

            var preview = Preview.GetLabel("Preview", 0, 0);
            paramView.Add(preview);
            var program = Program.GetLabel("Program", 0, Pos.Bottom(preview));
            paramView.Add(program);
            var standby = Standby.GetLabel("Standby", 0, Pos.Bottom(program));
            paramView.Add(standby);
            var heartbeat = Heartbeat.GetLabel("Heartbeat", 0, Pos.Bottom(standby));
            paramView.Add(heartbeat);
            var error = Error.GetLabel("Error", 0, Pos.Bottom(heartbeat));
            paramView.Add(error);

            return paramView;
        }
    }
}
