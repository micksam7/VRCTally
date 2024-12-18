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
        ProgramConfig config = Task.Run(LoadConfig).Result;

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
    }

    private static async Task<ProgramConfig> LoadConfig()
    {
        Console.WriteLine("Loading config file");
        //load in the config file'
        string xml = await File.ReadAllTextAsync("config.xml");

        ProgramConfig config;

        XmlSerializer serializer = new XmlSerializer(typeof(ProgramConfig));
        using (StringReader reader = new StringReader(xml))
        {
            config = (ProgramConfig)serializer.Deserialize(reader);
            if (config == null)
            {
                throw new Exception("Failed to load config file");
            }
        }

        return config;
    }
}
