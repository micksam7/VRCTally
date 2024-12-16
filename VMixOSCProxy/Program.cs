// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using VMixAPI;
using CoreOSC;
using CoreOSC.IO;
using System.Net.Sockets;

public static class Program
{
    private static HttpClient vmixclient = new();
    private static UdpClient oscClient = new();
    private static Config config;
    private static bool Heartbeat = false;
    private static bool Error = false;
    const string avatarParamPrefix = "/avatar/parameters/";

    public static async Task Main()
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

        //setup OSC client
        oscClient = new(config.Osc.Ip, config.Osc.Port);

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

        //wait infinitely
        await Task.Delay(-1);
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
            Address heartbeat = new(avatarParamPrefix + config.Osc.Parameters.Heartbeatparameter);
            Heartbeat = !Heartbeat;
            await SendOSC(heartbeat, BoolToValue(Heartbeat));

            Address error = new(avatarParamPrefix + config.Osc.Parameters.Errorparameter);
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
                    Console.WriteLine($"You are currently: {state}");

                    //setup endpoints
                    Address preview =
                        new(avatarParamPrefix + config.Osc.Parameters.Previewparameter);
                    Address program =
                        new(avatarParamPrefix + config.Osc.Parameters.Programparameter);
                    Address standby =
                        new(avatarParamPrefix + config.Osc.Parameters.Standbyparameter);

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
                Console.WriteLine(
                    "Could not communicate to VMix over HTTP, is the IP/Port correct or is VMix running?"
                );
                Console.WriteLine($"VMix Configured Endpoint: {config.Vmix.Ip}:{config.Vmix.Port}");
                break;
            case SocketException:
                Console.WriteLine(
                    "Could not communicate to VRChat over OSC, is the port/IP correct or is VRChat running?"
                );
                Console.WriteLine(
                    $"VRChat OSC Configured Endpoint: {config.Osc.Ip}:{config.Osc.Port}"
                );
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

    public static object BoolToValue(bool value)
    {
        return value ? OscTrue.True : OscFalse.False;
    }
}
