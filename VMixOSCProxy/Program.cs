// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using VMixAPI;
using CoreOSC;
using CoreOSC.IO;
using VRC.OSCQuery;
using Terminal.Gui;
using System.Net.Sockets;

ProgramWindow.Startup().Wait();
Application.IsMouseDisabled = true;
Application.Run<ProgramWindow>();

public class ProgramWindow : Window
{
    private static HttpClient vmixclient = new();
    private static UdpClient oscClient = new();
    private static OSCQueryService oscQuery;
    private static Config config;
    private static bool Heartbeat = false;
    private static bool Error = false;
    const string avatarParamPrefix = "/avatar/parameters/";

    private static Label OSCQueryLabel;
    private static Label OSCLabel;
    private static Label VMixLabel;
    private static Label VMixEndpointLabel;
    private static Label OSCParametersList;
    private static Label CurrentTallyStateLabel;
    private static Label UpdateRateLabel;

    public ProgramWindow()
    {
        Title = "VMix OSC Proxy";

        CurrentTallyStateLabel = new Label{
            Text = "Current Tally State",
            X = 0,
            Y = 0
        };
        Add(CurrentTallyStateLabel);

        UpdateRateLabel = new Label{
            Text = "Update Rate",
            X = Pos.Left(CurrentTallyStateLabel),
            Y = Pos.Bottom(CurrentTallyStateLabel)
        };
        Add(UpdateRateLabel);

        //we want to make a set of sub windows on the right side
        var OSCWindow = new Window("OSC")
        {
            X = 0,
            Y = Pos.Bottom(UpdateRateLabel),
            Width = Dim.Percent(50),
            Height = Dim.Fill()
        };
        Add(OSCWindow);

        OSCQueryLabel = new Label("OSCQuery");
        OSCLabel = new Label{Text = "OSC", X = Pos.Left(OSCQueryLabel), Y = Pos.Bottom(OSCQueryLabel)};
        var OSCParametersSubview = new Window("Parameters")
        {
            X = Pos.Left(OSCLabel),
            Y = Pos.Bottom(OSCLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        OSCParametersList = new Label{Text = "Parameters"};
        OSCParametersSubview.Add(OSCParametersList);
        OSCWindow.Add(OSCQueryLabel, OSCLabel, OSCParametersSubview);

        var VMixWindow = new Window("VMix")
        {
            X = Pos.Right(OSCWindow),
            Y = Pos.Top(OSCWindow),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        Add(VMixWindow);

        VMixEndpointLabel = new Label(0, 0, "VMix endpoint: ");
        VMixLabel = new Label{Text = "VMix", X = Pos.Left(VMixEndpointLabel), Y = Pos.Bottom(VMixEndpointLabel)};
        VMixWindow.Add(VMixEndpointLabel, VMixLabel);
    }

    private static void RefreshLabels()
    {
        OSCQueryLabel.Text = $"OSCQuery Service running at TCP {oscQuery.TcpPort} and UDP {oscQuery.TcpPort}";

        OSCParametersList.Text = "";
        //add all the parameters from the config
        foreach (var param in config.Osc.Parameters.GetType().GetProperties())
        {
            OSCParametersList.Text += $"{param.Name}: {param.GetValue(config.Osc.Parameters)}\n";
        }

        VMixEndpointLabel.Text = $"VMix endpoint: {vmixclient.BaseAddress}";

        UpdateRateLabel.Text = $"Update Rate: {config.UpdateRate}ms (every {config.UpdateRate / 1000f}s)";
    }

    public static async Task Startup()
    {
        //load in the config file'
        string xml = await File.ReadAllTextAsync("config.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(Config));
        using (StringReader reader = new StringReader(xml))
        {
            config = (Config)serializer.Deserialize(reader);
        }

        //setup HTTP[s] request
        vmixclient = new()
        {
            BaseAddress = new Uri($"http://{config.Vmix.Ip}:{config.Vmix.Port}/API")
        };
        //username and password requirement
        vmixclient.DefaultRequestHeaders.Authorization = new(
            "Basic",
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{config.Vmix.Username}:{config.Vmix.Password}")
            )
        );

        //startup oscquery service
        oscQuery = new OSCQueryServiceBuilder()
            .WithDefaults()
            .WithServiceName("VMix Tally Light Proxy")
            .Build();

        await FindVRChatOSC();

        // Create a timer
        System.Timers.Timer mainTimer = new System.Timers.Timer();
        mainTimer.Elapsed += new ElapsedEventHandler(WatchVMIX);
        mainTimer.Interval = config.UpdateRate;
        mainTimer.Start();

        //setup a seperate timer that always has the same update rate for the heartbeat
        System.Timers.Timer heartbeatTimer = new System.Timers.Timer();
        heartbeatTimer.Elapsed += new ElapsedEventHandler(SendProgramStatus);
        heartbeatTimer.Interval = 500;
        heartbeatTimer.Start();
    }

    private static async Task FindVRChatOSC()
    {
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
                //setup OSC client
                string IP = service.address.ToString();
                int port = service.port;
                oscClient = new(IP, port);
            }
        }
    }

    //create a enum to represent the state of the VMix

    public enum VMixState
    {
        Live,
        Preview,
        Standby
    }

    public static async void SendProgramStatus(object source, ElapsedEventArgs e)
    {
        try
        {
            Address heartbeat = new(avatarParamPrefix + config.Osc.Parameters.Heartbeat);
            Heartbeat = !Heartbeat;
            await SendOSC(heartbeat, BoolToValue(Heartbeat));

            Address error = new(avatarParamPrefix + config.Osc.Parameters.Error);
            await SendOSC(error, BoolToValue(Error));
        }
        catch (Exception ex)
        {
            //if log exception is true, throw
            if (LogException(ex).Result)
            {
                throw;
            }
        }

        RefreshLabels();
        //also refresh the screen here
        Application.Refresh();
    }

    public static async void WatchVMIX(object source, ElapsedEventArgs e)
    {
        //clear console
        Console.Clear();
        try
        {
            //request VMix XML
            string xml = await vmixclient.GetStringAsync("");

            //replace all "False" with "false" and "True" with "true"
            xml = xml.Replace("False", "false").Replace("True", "true");

            XmlSerializer serializer = new XmlSerializer(typeof(VMixAPI.Vmix));
            using (StringReader reader = new StringReader(xml))
            {
                VMixAPI.Vmix vmix = (VMixAPI.Vmix)serializer.Deserialize(reader);

                //print VMix version
                Console.WriteLine($"VMix version: {vmix.Version}");

                //try to find the input with the name the user entered, otherwise print an error and skip everything else
                Input input = vmix.Inputs.Input.FirstOrDefault(i => i.Title == config.Vmix.Tally);
                if (input == null)
                {
                    Console.WriteLine($"Input with name '{config.Vmix.Tally}' not found");
                    Error = true;
                }
                else
                {
                    Console.WriteLine($"Your Tally is: {input.Title}");

                    //print current in preview and in active
                    Console.WriteLine(
                        $"Current {VMixState.Preview} in VMix: {vmix.PreviewInput.Title}"
                    );
                    Console.WriteLine(
                        $"Current {VMixState.Live} in VMix: {vmix.ActiveInput.Title}"
                    );

                    //determine if we are live, preview or standby
                    VMixState state;
                    if (vmix.Active == input.Number) //this needs to be first to ensure that if you are both active and previewed, that you are considered live
                    {
                        state = VMixState.Live;
                    }
                    else if (vmix.Preview == input.Number)
                    {
                        state = VMixState.Preview;
                    }
                    else
                    {
                        state = VMixState.Standby;
                    }
                    CurrentTallyStateLabel.Text = $"Current Tally State: {state}";

                    //setup endpoints
                    Address preview =
                        new(avatarParamPrefix + config.Osc.Parameters.Preview);
                    Address program =
                        new(avatarParamPrefix + config.Osc.Parameters.Program);
                    Address standby =
                        new(avatarParamPrefix + config.Osc.Parameters.Standby);

                    //send OSC updates
                    await SendOSC(preview, BoolToValue(state == VMixState.Preview));
                    await SendOSC(program, BoolToValue(state == VMixState.Live));
                    await SendOSC(standby, BoolToValue(state == VMixState.Standby));
                    //clear error state
                    Error = false;
                }
            }
        }
        catch (Exception ex)
        {
            //if log exception is true, throw
            if (LogException(ex).Result)
            {
                throw;
            }

            //set error state
            Error = true;
        }
    }

    private static async Task<bool> LogException(Exception ex)
    {
        switch (ex)
        {
            case HttpRequestException:
                VMixLabel.Text = "Could not communicate to VMix over HTTP, is the IP/Port correct or is VMix running?";
                break;
            case SocketException:
                OSCLabel.Text = "Could not communicate to VRChat over OSC, is VRChat running?";
                await FindVRChatOSC();
                break;
            default:
                //return failed
                return true;
        }
        //return success
        return false;
    }

    public static async Task SendOSC(Address address, params object[] values)
    {
        var message = new OscMessage(address, values);
        await oscClient.SendMessageAsync(message);
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
}
