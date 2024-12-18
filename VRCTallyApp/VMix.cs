using System.Text;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using ConfigXML;
using Terminal.Gui;

public class Vmix
{
    private VmixAPIData data = new();

    private static HttpClient vmixclient = new();

    //store the reference to the config
    private static ProgramConfig config;

    public Vmix(ProgramConfig conf)
    {
        config = conf;

        //setup HTTP[s] request
        vmixclient = new()
        {
            BaseAddress = new Uri($"http://{conf.Vmix.Ip}:{conf.Vmix.Port}/API"),
            //set the timeout really low
            //Timeout = TimeSpan.FromSeconds(1)
        };
        //username and password requirement
        vmixclient.DefaultRequestHeaders.Authorization = new(
            "Basic",
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{conf.Vmix.Username}:{conf.Vmix.Password}")
            )
        );

        //setup the internal timer to load the xml
        // Create a timer
        System.Timers.Timer mainTimer = new System.Timers.Timer();
        mainTimer.Elapsed += new ElapsedEventHandler(WatchVMIX);
        mainTimer.Interval = conf.Vmix.UpdateRate;
        mainTimer.Start();
    }

    //create a enum to represent the state of the VMix
    public enum VMixState
    {
        Live,
        Preview,
        Standby,
        Unknown
    }

    public async void WatchVMIX(object source, ElapsedEventArgs e)
    {
        try
        {
            try
            {
                data = FromXML(await vmixclient.GetStringAsync(""));
            }
            catch
            {
                //explicitely clear the vmix info
                data = new();
                throw;
            }

            Input? tallyInput = data.FindInput(config.Vmix.Tally);

            if (tallyInput == null)
            {
                config.Osc.parameters.Error.Value = true;
            }
            else
            {
                VMixState state = InterpretInputToState(tallyInput);

                //send OSC updates
                config.Osc.parameters.Preview.Value = state == VMixState.Preview;
                config.Osc.parameters.Program.Value = state == VMixState.Live;
                config.Osc.parameters.Standby.Value = state == VMixState.Standby;
                //clear error state, but make sure if we cant find the input that we still error
                config.Osc.parameters.Error.Value = state == VMixState.Unknown;
            }
        }
        catch (Exception ex)
        {
            //set error state
            config.Osc.parameters.Error.Value = true;
        }
    }

    public VMixState InterpretInputToState(Input? input)
    {
        if (input == null || input?.Number == -1)
        {
            return VMixState.Unknown;
        }

        //determine if we are live, preview or standby
        VMixState state;
        if (data.ActiveInput == input) //this needs to be first to ensure that if you are both active and previewed, that you are considered live
        {
            state = VMixState.Live;
        }
        else if (data.PreviewInput == input)
        {
            state = VMixState.Preview;
        }
        else
        {
            state = VMixState.Standby;
        }

        return state;
    }

    public static VmixAPIData FromXML(string xml)
    {
        //start a timer
        var watch = System.Diagnostics.Stopwatch.StartNew();

        StringBuilder sb = new StringBuilder(xml);
        sb.Replace("True", "true");
        sb.Replace("False", "false");

        var serializer = new XmlSerializer(typeof(VmixAPIData));
        using (var reader = new StringReader(sb.ToString()))
        {
            VmixAPIData data = (VmixAPIData)serializer.Deserialize(reader);
            //stop the timer
            watch.Stop();
            if (data != null)
            {
                data.xmlCharacterCount = xml.Length;
                //store the time it took to deserialize
                data.deserializationTime = watch.Elapsed;
                data.valid = true;
            }
            else
            {
                data = new VmixAPIData();
            }

            return data;
        }
    }

    public Window GetWindow(Pos x, Pos y, Dim width, Dim height)
    {
        Window vmixView =
            new("VMix")
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            };

        var vmixConnectionInfo = new Label("Hello, world!") { X = 0, Y = 0, };
        vmixConnectionInfo.DrawContent += (e) =>
        {
            if (data.valid)
            {
                vmixConnectionInfo.Text = $"Connected to VMix at {vmixclient.BaseAddress}";
            }
            else
            {
                vmixConnectionInfo.Text =
                    $"Attempting to connect to VMix at {vmixclient.BaseAddress}";
            }
        };
        vmixView.Add(vmixConnectionInfo);

        var vmixXMLInfo = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixConnectionInfo), };
        vmixXMLInfo.DrawContent += (e) =>
        {
            //TODO: Make this tell if we have a connection or not
            vmixXMLInfo.Text = $"VMix XML Character Count: {data.xmlCharacterCount}";
            vmixXMLInfo.Text +=
                $"\nDeserialization Time: {data.deserializationTime.TotalMicroseconds}μs";
        };
        vmixView.Add(vmixXMLInfo);

        var vmixVersionInfo = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixXMLInfo), };
        vmixVersionInfo.DrawContent += (e) =>
        {
            vmixVersionInfo.Text = $"VMix Version: {data.Version}";
        };
        vmixView.Add(vmixVersionInfo);

        var trackedTally = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(vmixVersionInfo), };
        trackedTally.DrawContent += (e) =>
        {
            trackedTally.Text = $"Configured Tally: {config.Vmix.Tally}";
            Input tallyinput = data.FindInput(config.Vmix.Tally) ?? new();
            trackedTally.Text += $"\nVMix Matched Input: {tallyinput.Title}";
        };
        vmixView.Add(trackedTally);

        var currentOutputs = new Label("Hello, world!") { X = 0, Y = Pos.Bottom(trackedTally), };
        currentOutputs.DrawContent += (e) =>
        {
            currentOutputs.Text =
                $"Current {VMixState.Preview} in VMix: {data.PreviewInput?.Title}";
            currentOutputs.Text +=
                $"\nCurrent {VMixState.Preview} in VMix: {data.ActiveInput?.Title}";
        };
        vmixView.Add(currentOutputs);

        var currentTallyStatus = new Label("Hello, world!")
        {
            X = 0,
            Y = Pos.Bottom(currentOutputs),
        };
        currentTallyStatus.DrawContent += (e) =>
        {
            Input tallyinput = data.FindInput(config.Vmix.Tally) ?? new();
            currentTallyStatus.Text = $"Current Tally Status: {InterpretInputToState(tallyinput)}";
        };
        vmixView.Add(currentTallyStatus);

        return vmixView;
    }
}
