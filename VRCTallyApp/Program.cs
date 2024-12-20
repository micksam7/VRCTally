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

        FrameView oscView;
        FrameView vmixView;

        #region wizard
        //create a setup wizard to edit the config
        var vmixConnectionWizard = new Wizard("VMIX Connection");
        var firstVmixStep = new Wizard.WizardStep("");
        vmixConnectionWizard.AddStep(firstVmixStep);

        Option updateRateField =
            new("Update Rate: ", config.Vmix.UpdateRate.ToString()) { X = 0, Y = 0 };
        firstVmixStep.Add(updateRateField);

        Option ipField = new("IP: ", config.Vmix.Ip) { X = 0, Y = Pos.Bottom(updateRateField), };
        firstVmixStep.Add(ipField);

        Option portField =
            new("Port: ", config.Vmix.Port.ToString()) { X = 0, Y = Pos.Bottom(ipField), };
        firstVmixStep.Add(portField);

        Option usernameField =
            new("Username: ", config.Vmix.Username) { X = 0, Y = Pos.Bottom(portField), };
        firstVmixStep.Add(usernameField);

        Option passwordField =
            new("Password: ", config.Vmix.Password) { X = 0, Y = Pos.Bottom(usernameField), };
        firstVmixStep.Add(passwordField);

        //we need to make a step for selecting the tally
        //query the api once
        var tallyWizard = new Wizard("Tally Selection");
        var tallyWizardStep = new Wizard.WizardStep("");
        tallyWizard.AddStep(tallyWizardStep);
        var notFoundLabel = new Label("No inputs found!") { X = 0, Y = 0, };
        notFoundLabel.Visible = false;
        var inputList = new ListView(new NStack.ustring[0])
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        inputList.AllowsMultipleSelection = false;
        inputList.AllowsMarking = true;
        tallyWizardStep.Add(inputList);
        tallyWizardStep.Add(notFoundLabel);

        tallyWizard.Added += async (e) =>
        {
            await vmix.PopulateSingleShot();

            //populate the options
            NStack.ustring[] inputs = vmix.GetInputs();
            inputList.SetSource(inputs);

            if (inputList.Source.Length == 0)
            {
                notFoundLabel.Visible = true;
                notFoundLabel.Text =
                    $"No inputs found! Check your VMix configuration. \'{config.Vmix.Tally}\' will be used instead.";
            }
            else
            {
                notFoundLabel.Visible = false;

                //we should try and select the one we are already using
                for (int i = 0; i < inputList.Source.Length; i++)
                {
                    if (inputs[i].ToString() == config.Vmix.Tally)
                    {
                        inputList.SelectedItem = i;
                        inputList.MarkUnmarkRow();
                        break;
                    }
                }
            }
        };

        tallyWizard.Finished += (e) =>
        {
            //we need to load in everything possibly just changed in the previous step
            config.Vmix.UpdateRate = int.Parse(updateRateField.tf.Text.ToString());
            config.Vmix.Ip = ipField.tf.Text.ToString();
            config.Vmix.Port = int.Parse(portField.tf.Text.ToString());
            config.Vmix.Username = usernameField.tf.Text.ToString();
            config.Vmix.Password = passwordField.tf.Text.ToString();
            vmix.UpdateConfig(config);
            SaveConfig(config);

            //we now want to read in what the radio label selection was
            int selected = inputList.SelectedItem;
            if (inputList.Source.Length > 0)
            {
                string potentialNewTally = vmix.GetInputs()[selected]?.ToString() ?? "";
                //null check
                if (!string.IsNullOrWhiteSpace(potentialNewTally))
                {
                    config.Vmix.Tally = potentialNewTally;
                }
            }

            vmix.updateTimer.Start();

            Remove(tallyWizard);
        };

        vmixConnectionWizard.Finished += (e) =>
        {
            //we need to load in everything possibly just changed in the previous step
            config.Vmix.UpdateRate = int.Parse(updateRateField.tf.Text.ToString());
            config.Vmix.Ip = ipField.tf.Text.ToString();
            config.Vmix.Port = int.Parse(portField.tf.Text.ToString());
            config.Vmix.Username = usernameField.tf.Text.ToString();
            config.Vmix.Password = passwordField.tf.Text.ToString();
            vmix.UpdateConfig(config);
            SaveConfig(config);

            oscView = osc.GetWindow(0, 0, Dim.Percent(50), Dim.Fill());
            vmixView = vmix.GetWindow(Pos.Right(oscView), 0, Dim.Percent(50), Dim.Fill());

            //restart the vmix timer
            vmix.updateTimer.Start();

            //remove the wizard
            Remove(vmixConnectionWizard);
        };
        #endregion

        var RemoteTallySelectorButton = new Button("Select Existing Tally") { X = 0, Y = 0, };
        RemoteTallySelectorButton.Clicked += () =>
        {
            Add(tallyWizard);
            //pause the vmix timer
            vmix.updateTimer.Stop();
        };
        Add(RemoteTallySelectorButton);

        //setup a button to edit the config
        var VmixConfigButton = new Button("Edit VMix Config") { X = Pos.Right(RemoteTallySelectorButton), Y = 0, };
        VmixConfigButton.Clicked += () =>
        {
            Add(vmixConnectionWizard);
            //pause the vmix timer
            vmix.updateTimer.Stop();
        };
        Add(VmixConfigButton);

        oscView = osc.GetWindow(0, Pos.Bottom(VmixConfigButton), Dim.Percent(50), Dim.Fill());
        Add(oscView);

        vmixView = vmix.GetWindow(
            Pos.Right(oscView),
            Pos.Bottom(VmixConfigButton),
            Dim.Percent(50),
            Dim.Fill()
        );
        Add(vmixView);
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
