// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using Terminal.Gui;

//start main
Application.Run<ProgramWindow>();
await Task.Delay(-1);

public class ProgramWindow : Window
{
    public ProgramWindow()
        : base("VRCTally - VMix OSC Proxy - Happyrobot33")
    {
        ProgramConfig config = LoadConfig();

        Osc osc = new(config);
        //setup the vmix object
        Vmix vmix = new(config);

        System.Timers.Timer appRefresh = new System.Timers.Timer();
        appRefresh.Elapsed += new ElapsedEventHandler(
            (source, e) => Application.MainLoop.Invoke(() => Application.Refresh())
        );
        appRefresh.Interval = 100;
        appRefresh.Start();

        //wait infinitely
        //await Task.Delay(-1);

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var oscView = osc.GetWindow(0, 0, Dim.Percent(50), Dim.Fill());
        Add(oscView);

        var vmixView = vmix.GetWindow(Pos.Right(oscView), 0, Dim.Percent(50), Dim.Fill());
        Add(vmixView);

        //create a setup wizard to edit the config
        var vmixWizard = new Wizard("test");
        TextField updateRateField = Option(config.Vmix.UpdateRate.ToString());
        vmixWizard.Add(updateRateField.GetTopSuperView());

        TextField ipField = Option(config.Vmix.Ip);
        ipField.GetTopSuperView().Y = Pos.Bottom(updateRateField.GetTopSuperView());
        vmixWizard.Add(ipField.GetTopSuperView());

        TextField portField = Option(config.Vmix.Port.ToString());
        portField.GetTopSuperView().Y = Pos.Bottom(ipField.GetTopSuperView());
        vmixWizard.Add(portField.GetTopSuperView());

        TextField usernameField = Option(config.Vmix.Username);
        usernameField.GetTopSuperView().Y = Pos.Bottom(portField.GetTopSuperView());
        vmixWizard.Add(usernameField.GetTopSuperView());

        TextField passwordField = Option(config.Vmix.Password);
        passwordField.GetTopSuperView().Y = Pos.Bottom(usernameField.GetTopSuperView());
        vmixWizard.Add(passwordField.GetTopSuperView());


        vmixWizard.Finished += (e) =>
        {
            config.Vmix.UpdateRate = int.Parse(updateRateField.Text.ToString());
            config.Vmix.Ip = ipField.Text.ToString();
            config.Vmix.Port = int.Parse(portField.Text.ToString());
            config.Vmix.Username = usernameField.Text.ToString();
            config.Vmix.Password = passwordField.Text.ToString();
            SaveConfig(config);

            oscView = osc.GetWindow(0, 0, Dim.Percent(50), Dim.Fill());
            vmixView = vmix.GetWindow(Pos.Right(oscView), 0, Dim.Percent(50), Dim.Fill());

            //restart the vmix timer
            vmix.updateTimer.Start();

            //remove the wizard
            Remove(vmixWizard);
        };


        //watch for letter e input
        KeyDown += (e) =>
        {
            if (e.KeyEvent.Key == Key.e)
            {
                Add(vmixWizard);
                //pause the vmix timer
                vmix.updateTimer.Stop();
            }
        };
    }

    private static TextField Option(string str)
    {
        View option = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
        };
        var lbl = new Label("Update Rate: ")
        {
            X = 0,
            Y = 0,
        };
        option.Add(lbl);
        var field = new TextField("")
        {
            X = Pos.Right(lbl),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = str,
        };
        option.Add(field);
        return field;
    }

    private static void SaveConfig(ProgramConfig config)
    {
        Console.WriteLine("Saving config file");

        XmlSerializer serializer = new XmlSerializer(typeof(ProgramConfig));
        using (FileStream writer = new FileStream("config.xml", FileMode.Create, FileAccess.Write))
        {
            serializer.Serialize(writer, config);
        }
    }

    private static ProgramConfig LoadConfig()
    {
        Console.WriteLine("Loading config file");

        ProgramConfig? config;

        XmlSerializer serializer = new XmlSerializer(typeof(ProgramConfig));
        using (FileStream reader = new FileStream("config.xml", FileMode.Open, FileAccess.Read))
        {
            object? configObject = serializer.Deserialize(reader);
            config = configObject as ProgramConfig;
            if (config == null)
            {
                throw new Exception("Failed to load config file");
            }
        }

        return config;
    }
}
