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

    public static async Task Main()
    {
        //load in the config file
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
        System.Timers.Timer myTimer = new System.Timers.Timer();
        myTimer.Elapsed += new ElapsedEventHandler(myEventAsync);
        myTimer.Interval = config.UpdateRate;
        myTimer.Start();

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

    public static async void myEventAsync(object source, ElapsedEventArgs e)
    {
        //request VMix XML
        string xml = await vmixclient.GetStringAsync("");

        //replace all "False" with "false" and "True" with "true"
        xml = xml.Replace("False", "false").Replace("True", "true");

        XmlSerializer serializer = new XmlSerializer(typeof(VMixAPI.Vmix));
        using (StringReader reader = new StringReader(xml))
        {
            VMixAPI.Vmix vmix = (VMixAPI.Vmix)serializer.Deserialize(reader);

            //clear console
            Console.Clear();

            //print VMix version
            Console.WriteLine($"VMix version: {vmix.Version}");

            //try to find the input with the name the user entered, otherwise print an error and skip everything else
            Input input = vmix.Inputs.Input.FirstOrDefault(i => i.Title == config.Vmix.Tally);
            if (input == null)
            {
                Console.WriteLine($"Input with name '{config.Vmix.Tally}' not found");
                return;
            }
            Console.WriteLine($"Your Tally is: {input.Title}");

            //print current in preview and in active
            Console.WriteLine($"Current {VMixState.Preview} in VMix: {vmix.PreviewInput.Title}");
            Console.WriteLine($"Current {VMixState.Live} in VMix: {vmix.ActiveInput.Title}");

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
            const string avatarParamPrefix = "/avatar/parameters/";
            Address preview = new(avatarParamPrefix + config.Osc.Parameters.Previewparameter);
            Address program = new(avatarParamPrefix + config.Osc.Parameters.Programparameter);
            Address standby = new(avatarParamPrefix + config.Osc.Parameters.Standbyparameter);
            Address heartbeat = new(avatarParamPrefix + config.Osc.Parameters.Heartbeatparameter);

            //send OSC updates
            await SendOSC(preview, BoolToValue(state == VMixState.Preview));
            await SendOSC(program, BoolToValue(state == VMixState.Live));
            await SendOSC(standby, BoolToValue(state == VMixState.Standby));
            //flip heartbeat
            Heartbeat = !Heartbeat;
            await SendOSC(heartbeat, BoolToValue(Heartbeat));
        }
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
