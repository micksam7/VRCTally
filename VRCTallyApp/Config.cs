using System.Xml.Serialization;
using CoreOSC;
using Terminal.Gui;
using YamlDotNet.Serialization;

namespace ConfigXML
{
    public class ProgramConfig
    {
        public required VmixConfig Vmix { get; set; }
        public required OscConfig Osc { get; set; }

        public class VmixConfig
        {
            public required string Ip { get; set; }

            public int Port { get; set; }
            public bool HideAddress { get; set; }

            public required string Username { get; set; }

            public required string Password { get; set; }

            public required string Tally { get; set; }
            public required bool ExactMatch { get; set; }

            public int UpdateRate { get; set; }
        }

        public class OscConfig
        {
            public required int UpdateRate { get; set; }
            public required Parameters parameters { get; set; }

            public class Parameters
            {
                public required Parameter<bool> Preview { get; set; }

                public required Parameter<bool> Program { get; set; }

                public required Parameter<bool> Standby { get; set; }

                public required Parameter<bool> Heartbeat { get; set; }

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

                    public required string[] ParameterStrings { get; set; }

                    [YamlIgnore]
                    public Address[] Addresses =>
                        ParameterStrings.Select(s => new Address(avatarParamPrefix + s)).ToArray();

                    [YamlIgnore]
                    public required T Value { get; set; }

                    public Label GetLabel(string name, Pos x, Pos y)
                    {
                        var lbl = new Label("Preview") { X = x, Y = y, };
                        lbl.DrawContent += (e) =>
                        {
                            lbl.Text = $"{name}: {Value}";
                            foreach (var addr in ParameterStrings)
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
