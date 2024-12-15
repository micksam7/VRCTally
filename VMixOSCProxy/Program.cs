// See https://aka.ms/new-console-template for more information
using System.Timers;
using System.Xml.Serialization;

public static class Program
{
    private static HttpClient client = new();
    private static Config config;

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
        client = new() { BaseAddress = new Uri("http://localhost:8088/API") };

        // Create a timer
        System.Timers.Timer myTimer = new System.Timers.Timer();
        myTimer.Elapsed += new ElapsedEventHandler(myEventAsync);
        myTimer.Interval = 1000;
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
        string xml = await client.GetStringAsync("");

        //replace all "False" with "false" and "True" with "true"
        xml = xml.Replace("False", "false").Replace("True", "true");

        XmlSerializer serializer = new XmlSerializer(typeof(Vmix));
        using (StringReader reader = new StringReader(xml))
        {
            Vmix vmix = (Vmix)serializer.Deserialize(reader);

            //clear console
            Console.Clear();

            //print VMix version
            Console.WriteLine($"VMix version: {vmix.Version}");

            //try to find the input with the name the user entered, otherwise print an error and skip everything else
            Input input = vmix.Inputs.Input.FirstOrDefault(i => i.Title == config.Tally);
            if (input == null)
            {
                Console.WriteLine($"Input with name '{config.Tally}' not found");
                return;
            }
            Console.WriteLine($"Your Tally is: {input.Title}");

            //print current in preview and in active
            Console.WriteLine($"Currently {VMixState.Preview} in VMix: {vmix.PreviewInput.Title}");
            Console.WriteLine($"Currently {VMixState.Live} in VMix: {vmix.ActiveInput.Title}");

            //determine if we are live, preview or standby
            VMixState state;
            if (vmix.Preview == input.Number)
            {
                state = VMixState.Preview;
            }
            else if (vmix.Active == input.Number)
            {
                state = VMixState.Live;
            }
            else
            {
                state = VMixState.Standby;
            }
            Console.WriteLine($"You are currently: {state}");
        }
    }
}
