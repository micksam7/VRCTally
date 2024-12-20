using System.Xml.Serialization;
using CoreOSC;
using Terminal.Gui;

namespace ConfigXML
{
    [XmlRoot(ElementName = "config")]
    public class ProgramConfig
    {
        [XmlElement(ElementName = "vmix")]
        public required VmixConfig Vmix { get; set; }

        [XmlElement(ElementName = "osc")]
        public required OscConfig Osc { get; set; }

        [XmlRoot(ElementName = "vmix")]
        public class VmixConfig
        {
            [XmlElement(ElementName = "ip")]
            public required string Ip { get; set; }

            [XmlElement(ElementName = "port")]
            public int Port { get; set; }

            [XmlElement(ElementName = "username")]
            public required string Username { get; set; }

            [XmlElement(ElementName = "password")]
            public required string Password { get; set; }

            [XmlElement(ElementName = "tally")]
            public required string Tally { get; set; }

            [XmlElement(ElementName = "updaterate")]
            public int UpdateRate { get; set; }
        }

        [XmlRoot(ElementName = "osc")]
        public class OscConfig
        {
            [XmlElement(ElementName = "parameters")]
            public required Parameters parameters { get; set; }

            [XmlElement(ElementName = "updaterate")]
            public required int UpdateRate { get; set; }

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

                public FrameView GetWindow(Pos x, Pos y, Dim width, Dim height)
                {
                    FrameView paramView =
                        new("Parameters")
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

                public class Parameter<T>
                {
                    public const string avatarParamPrefix = "/avatar/parameters/";

                    [XmlElement(ElementName = "parameter")]
                    public required string[] Str { get; set; }
                    public Address[] Addresses =>
                        Str.Select(s => new Address(avatarParamPrefix + s)).ToArray();

                    public required T Value { get; set; }

                    public Label GetLabel(string name, Pos x, Pos y)
                    {
                        var lbl = new Label("Preview") { X = x, Y = y, };
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
            }
        }
    }
}
