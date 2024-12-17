// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using VMixAPI;
using Terminal.Gui;

//start main
await ProgramWindow.Main();
Application.Run<ProgramWindow>();

public class ProgramWindow : Window
{
    private static HttpClient vmixclient = new();
    private static Config config;
    private static string VMixXML = "";
    private static Input input = new();
    private static VMixAPI.Vmix vmix = new();

    public ProgramWindow()
        : base("VRCTally - VMix OSC Proxy - Happyrobot33")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        //setup two subviews, one for OSC and one for VMix
        Window oscView =
            new("OSC")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill(),
            };
        Add(oscView);

        var oscQueryInfo = new Label("Hello, world!") { X = 0, Y = 0, };
        oscQueryInfo.DrawContent += (e) =>
        {
            oscQueryInfo.Text =
                $"OSCQuery Service running at TCP {config.Osc.oscQuery.TcpPort} and UDP {config.Osc.oscQuery.TcpPort}";
        };
        oscView.Add(oscQueryInfo);

        var oscConnectionInfo = new Label("Hello, world!") { Y = Pos.Bottom(oscQueryInfo), };
        oscConnectionInfo.DrawContent += (e) =>
        {
            if (config.Osc.oscClient.Client.Connected)
            {
                oscConnectionInfo.Text =
                    $"VRChat OSC Client running at {config.Osc.oscClient.Client.RemoteEndPoint}";
            }
            else
            {
                oscConnectionInfo.Text = "VRChat OSC Client not connected!";
            }
        };
        oscView.Add(oscConnectionInfo);

        //we want to add a sub view that shows all the parameters
        oscView.Add(
            config.Osc.Parameters.GetWindow(
                0,
                Pos.Bottom(oscConnectionInfo),
                Dim.Fill(),
                Dim.Fill()
            )
        );

        Window vmixView =
            new("VMix")
            {
                X = Pos.Right(oscView),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
        Add(vmixView);

        var vmixConnectionInfo = new Label("Hello, world!") { X = 0, Y = 0, };
        vmixConnectionInfo.DrawContent += (e) =>
        {
            //TODO: Make this tell if we have a connection or not
            vmixConnectionInfo.Text = $"Connected to VMix at {vmixclient.BaseAddress}";
        };
        vmixView.Add(vmixConnectionInfo);

        var vmixXMLInfo = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixConnectionInfo), };
        vmixXMLInfo.DrawContent += (e) =>
        {
            //TODO: Make this tell if we have a connection or not
            vmixXMLInfo.Text = $"VMix XML Character Count: {VMixXML.Length}";
        };
        vmixView.Add(vmixXMLInfo);

        var vmixVersionInfo = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixXMLInfo), };
        vmixVersionInfo.DrawContent += (e) =>
        {
            vmixVersionInfo.Text = $"VMix Version: {vmix.Version}";
        };
        vmixView.Add(vmixVersionInfo);

        var trackedTally = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixVersionInfo), };
        trackedTally.DrawContent += (e) =>
        {
            trackedTally.Text = $"Configured Tally: {config.Vmix.Tally}";
            trackedTally.Text += $"\nVMix Matched Input: {input.Title}";
        };
        vmixView.Add(trackedTally);

        var currentOutputs = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(trackedTally), };
        currentOutputs.DrawContent += (e) =>
        {
            currentOutputs.Text =
                $"Current {VMixState.Preview} in VMix: {InterpretInputToState(vmix.PreviewInput)}";
            currentOutputs.Text +=
                $"\nCurrent {VMixState.Live} in VMix: {InterpretInputToState(vmix.ActiveInput)}";
        };
        vmixView.Add(currentOutputs);

        var currentTallyStatus = new Label("Hello, world!")
        {
            X = 0,
            Y = Pos.Bottom(currentOutputs),
        };
        currentTallyStatus.DrawContent += (e) =>
        {
            currentTallyStatus.Text = $"Current Tally Status: {InterpretInputToState(input)}";
        };
        vmixView.Add(currentTallyStatus);
    }

    public static async Task Main()
    {
        //load in the config file'
        string xml = await File.ReadAllTextAsync("config.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(Config));
        using (StringReader reader = new StringReader(xml))
        {
            config = (Config)serializer.Deserialize(reader);
            config.Osc.StartTimers(config);
        }

        //setup HTTP[s] request
        vmixclient = new()
        {
            BaseAddress = new Uri($"http://{config.Vmix.Ip}:{config.Vmix.Port}/API"),
            //set the timeout really low
            Timeout = TimeSpan.FromSeconds(1)
        };
        //username and password requirement
        vmixclient.DefaultRequestHeaders.Authorization = new(
            "Basic",
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{config.Vmix.Username}:{config.Vmix.Password}")
            )
        );

        // Create a timer
        System.Timers.Timer mainTimer = new System.Timers.Timer();
        mainTimer.Elapsed += new ElapsedEventHandler(WatchVMIX);
        mainTimer.Interval = config.Vmix.UpdateRate;
        mainTimer.Start();

        System.Timers.Timer appRefresh = new System.Timers.Timer();
        mainTimer.Elapsed += new ElapsedEventHandler((source, e) => Application.Refresh());
        mainTimer.Interval = 100;
        mainTimer.Start();

        //wait infinitely
        //await Task.Delay(-1);
    }

    //create a enum to represent the state of the VMix
    public enum VMixState
    {
        Live,
        Preview,
        Standby,
        Unknown
    }

    public static async void WatchVMIX(object source, ElapsedEventArgs e)
    {
        try
        {
            //request VMix XML
            VMixXML = await vmixclient.GetStringAsync("");

            //replace all "False" with "false" and "True" with "true"
            VMixXML = VMixXML.Replace("False", "false").Replace("True", "true");

            vmix = VMixAPI.Vmix.FromXML(VMixXML);

            //try to find the input with the name the user entered, otherwise print an error and skip everything else
            //we need to search with wildcard * in mind
            input = vmix.Inputs.Input.FirstOrDefault(i => i.Title.StartsWith(config.Vmix.Tally));
            if (input == null)
            {
                config.Osc.Parameters.Error.Value = true;
            }
            else
            {
                VMixState state = InterpretInputToState(input);

                //send OSC updates
                config.Osc.Parameters.Preview.Value = state == VMixState.Preview;
                config.Osc.Parameters.Program.Value = state == VMixState.Live;
                config.Osc.Parameters.Standby.Value = state == VMixState.Standby;
                //clear error state
                config.Osc.Parameters.Error.Value = false;
            }
        }
        catch (Exception ex)
        {
            //set error state
            config.Osc.Parameters.Error.Value = true;
        }
    }

    private static VMixState InterpretInputToState(Input? input)
    {
        if (input == null || input?.Number == -1)
        {
            return VMixState.Unknown;
        }

        //determine if we are live, preview or standby
        VMixState state;
        if (vmix.ActiveInput == input) //this needs to be first to ensure that if you are both active and previewed, that you are considered live
        {
            state = VMixState.Live;
        }
        else if (vmix.PreviewInput == input)
        {
            state = VMixState.Preview;
        }
        else
        {
            state = VMixState.Standby;
        }

        return state;
    }
}
