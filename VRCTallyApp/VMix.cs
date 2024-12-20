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
    private ProgramConfig config;

    public System.Timers.Timer updateTimer = new System.Timers.Timer();

    public Vmix(ProgramConfig conf)
    {
        UpdateConfig(conf);

        updateTimer.Elapsed += new ElapsedEventHandler(async (source, e) => await WatchVMIX(source, e));
        updateTimer.Start();
    }

    public async Task PopulateSingleShot()
    {
        await WatchVMIX(null, null);
    }

    public void UpdateConfig(ProgramConfig conf)
    {
        config = conf ?? throw new ArgumentNullException(nameof(conf));
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
        updateTimer.Interval = conf.Vmix.UpdateRate;
    }

    public NStack.ustring[] GetInputs()
    {
        /* //generate a bunch of dummy inputs
        NStack.ustring[] inputs = new NStack.ustring[40];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = (NStack.ustring)$"Input {i}";
        }
        return inputs; */
        return data.Inputs?.Input.Select(input => (NStack.ustring)input.Title).ToArray()
            ?? new NStack.ustring[0];

        //dummy optionms
        //return new NStack.ustring[0];
    }

    public async Task WatchVMIX(object? source, ElapsedEventArgs e)
    {
        try
        {
            data = FromXML(await vmixclient.GetStringAsync(""));
        }
        catch (HttpRequestException)
        {
            //explicitely clear the vmix info
            data = new();
            config.Osc.parameters.Error.Value = true;
            return;
        }

        Input? tallyInput = data.FindInput(config.Vmix.Tally);

        if (tallyInput == null)
        {
            config.Osc.parameters.Error.Value = true;
        }
        else
        {
            //send OSC updates
            config.Osc.parameters.Preview.Value = tallyInput == data.PreviewInput;
            config.Osc.parameters.Program.Value = tallyInput == data.ActiveInput;
            config.Osc.parameters.Standby.Value =
                tallyInput != data.PreviewInput && tallyInput != data.ActiveInput;
            //clear error state, but make sure if we cant find the input that we still error
            config.Osc.parameters.Error.Value = false;
        }
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
            object? dataObject = serializer.Deserialize(reader);
            VmixAPIData? data = dataObject as VmixAPIData;
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

    public FrameView GetWindow(Pos x, Pos y, Dim width, Dim height)
    {
        FrameView vmixView =
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
                $"Current {Input.VMixState.Preview} in VMix: {data.PreviewInput?.Title}";
            currentOutputs.Text +=
                $"\nCurrent {Input.VMixState.Program} in VMix: {data.ActiveInput?.Title}";
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
            currentTallyStatus.Text = $"Current Tally Status: {tallyinput.GetTallyState(data)}";
        };
        vmixView.Add(currentTallyStatus);

        return vmixView;
    }
}
