// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using Terminal.Gui;

//start main
await ProgramWindow.Main();
Application.Run<ProgramWindow>();
await Task.Delay(-1);

public class ProgramWindow : Window
{
    public static ProgramConfig config;
    private static Vmix vmix;
    private static Osc osc;

    public ProgramWindow()
        : base("VRCTally - VMix OSC Proxy - Happyrobot33")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var oscView = osc.GetWindow(0, 0, Dim.Percent(50), Dim.Fill());
        Add(oscView);

        var vmixView = vmix.GetWindow(Pos.Right(oscView), 0, Dim.Percent(50), Dim.Fill());
        Add(vmixView);
    }

    public static async Task Main()
    {
        //load in the config file'
        string xml = await File.ReadAllTextAsync("config.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(ProgramConfig));
        using (StringReader reader = new StringReader(xml))
        {
            config = (ProgramConfig)serializer.Deserialize(reader);
            if (config == null)
            {
                throw new Exception("Failed to load config file");
            }
            osc = new(ref config);
        }

        //setup the vmix object
        vmix = new(ref config);

        System.Timers.Timer appRefresh = new System.Timers.Timer();
        appRefresh.Elapsed += new ElapsedEventHandler(
            (source, e) => Application.MainLoop.Invoke(() => Application.Refresh())
        );
        appRefresh.Interval = 100;
        appRefresh.Start();

        //wait infinitely
        //await Task.Delay(-1);
    }
}
