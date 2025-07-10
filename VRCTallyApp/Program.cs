// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using ConfigXML;
using Terminal.Gui;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

//start main
Application.Run<ProgramWindow>();
await Task.Delay(-1);


public class ProgramWindow : Window
{
    const string VERSION = "v1.0.0";
    public ProgramWindow()
        : base($"VRCTally - VMix OSC Proxy - {VERSION} - Happyrobot33")
    {
        ProgramConfig config = LoadConfig();

        Osc osc = new(config);
        //setup the vmix object
        Vmix vmix = new(config);

        //wait infinitely
        //await Task.Delay(-1);

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        FrameView oscView;
        FrameView vmixView;

        var RemoteTallySelectorButton = new Button("Select Existing Tally") { X = 0, Y = 0, };
        RemoteTallySelectorButton.Clicked += () =>
        {
            var tallyWizard = new Wizard("Tally Selection");
            var tallyWizardStep = new Wizard.WizardStep("");
            tallyWizardStep.HelpText =
                "Select the input you want to follow with your tally light. Scroll up/down to see more inputs.";
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

                if (inputs.Length == 0)
                {
                    notFoundLabel.Visible = true;
                    notFoundLabel.Text =
                        $"No inputs found! Check your VMix configuration. \'{config.Vmix.Tally}\' will be used instead.";
                }
                else
                {
                    notFoundLabel.Visible = false;

                    //we should try and select the one we are already using
                    for (int i = 0; i < inputs.Length; i++)
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
                config.Vmix.ExactMatch = true;

                vmix.UpdateConfig(config);
                SaveConfig(config);

                vmix.updateTimer.Start();

                Remove(tallyWizard);
            };
            Add(tallyWizard);
            //pause the vmix timer
            vmix.updateTimer.Stop();
        };
        Add(RemoteTallySelectorButton);

        var WildcardTallySelectorButton = new Button("Wildcard Tally")
        {
            X = Pos.Right(RemoteTallySelectorButton),
            Y = 0,
        };
        WildcardTallySelectorButton.Clicked += () =>
        {
            //create a setup wizard to edit the config
            var wildcardTallyWizard = new Wizard("Wildcard Tally");
            var firstStep = new Wizard.WizardStep("");
            firstStep.HelpText =
                "Enter a section of a input name to automatically find the input with that string in it. For example, if you want to track a input that looks like \"Cam 4 - Happyrobot33\", you can just enter \"Happy\" and it will automatically find the highest priority input that matches. IMPORTANT: This is case-sensitive!";
            wildcardTallyWizard.AddStep(firstStep);
            Option wildcardField = new("Wildcard Tally: ", config.Vmix.Tally) { X = 0, Y = 0, };
            firstStep.Add(wildcardField);
            wildcardTallyWizard.Finished += (e) =>
            {
                config.Vmix.Tally = wildcardField.tf.Text.ToString() ?? "INVALIDINVALID";
                config.Vmix.ExactMatch = false;
                vmix.UpdateConfig(config);
                SaveConfig(config);

                vmix.updateTimer.Start();

                Remove(wildcardTallyWizard);
            };
            Add(wildcardTallyWizard);
        };
        Add(WildcardTallySelectorButton);

        //setup a button to edit the config
        var VmixConfigButton = new Button("Edit VMix Config")
        {
            X = Pos.Right(WildcardTallySelectorButton),
            Y = 0,
        };
        VmixConfigButton.Clicked += () =>
        {
            //create a setup wizard to edit the config
            var vmixConnectionWizard = new Wizard("VMIX Connection");
            var firstVmixStep = new Wizard.WizardStep("");
            vmixConnectionWizard.AddStep(firstVmixStep);

            Option updateRateField =
                new("Update Rate: ", config.Vmix.UpdateRate.ToString()) { X = 0, Y = 0 };
            firstVmixStep.Add(updateRateField);

            Option ipField =
                new("IP: ", config.Vmix.Ip) { X = 0, Y = Pos.Bottom(updateRateField), };
            firstVmixStep.Add(ipField);

            Option portField =
                new("Port: ", config.Vmix.Port.ToString()) { X = 0, Y = Pos.Bottom(ipField), };
            firstVmixStep.Add(portField);

            Option usernameField =
                new("Username: ", config.Vmix.Username) { X = 0, Y = Pos.Bottom(portField), };
            firstVmixStep.Add(usernameField);

            Option passwordField =
                new("Password: ", config.Vmix.Password) { X = 0, Y = Pos.Bottom(usernameField), };
            passwordField.tf.Secret = true;
            /* passwordField.tf.Enter += (e) =>
            {
                passwordField.tf.Secret = false;
            };
            passwordField.tf.Leave += (e) =>
            {
                passwordField.tf.Secret = true;
            }; */
            firstVmixStep.Add(passwordField);

            CheckBox exactMatchField = new("Look For Exact Input Names", config.Vmix.ExactMatch) { X = 0, Y = Pos.Bottom(passwordField), };
            exactMatchField.Toggled += (e) =>
            {
                config.Vmix.ExactMatch = exactMatchField.Checked;
            };
            firstVmixStep.Add(exactMatchField);
            CheckBox hideAddressField =
                new("Hide Address", config.Vmix.HideAddress) { X = 0, Y = Pos.Bottom(exactMatchField), };
            hideAddressField.Toggled += (e) =>
            {
                config.Vmix.HideAddress = hideAddressField.Checked;
            };
            firstVmixStep.Add(hideAddressField);
            firstVmixStep.HelpText =
                $"Setup all of the connection info for communicating to VMix. The \"{exactMatchField.Text}\" option will make the app look for exact input names instead of generalizing and looking for inputs that contain the string.";

            vmixConnectionWizard.Finished += (e) =>
            {
                //we need to load in everything possibly just changed in the previous step
                config.Vmix.UpdateRate = int.Parse(updateRateField.tf.Text.ToString() ?? "1000");
                config.Vmix.Ip = ipField.tf.Text.ToString() ?? "localhost";
                config.Vmix.Port = int.Parse(portField.tf.Text.ToString() ?? "8088");
                config.Vmix.Username = usernameField.tf.Text.ToString() ?? "admin";
                config.Vmix.Password = passwordField.tf.Text.ToString() ?? "password";
                vmix.UpdateConfig(config);
                SaveConfig(config);

                oscView = osc.GetWindow(0, 0, Dim.Percent(50), Dim.Fill());
                vmixView = vmix.GetWindow(Pos.Right(oscView), 0, Dim.Percent(50), Dim.Fill());

                //restart the vmix timer
                vmix.updateTimer.Start();

                //remove the wizard
                Remove(vmixConnectionWizard);
            };
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

    public static void InvokeApplicationRefresh()
    {
        Application.MainLoop.Invoke(() => Application.Refresh());
    }

    private static void SaveConfig(ProgramConfig config)
    {
        Console.WriteLine("Saving config file");

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(config);
        using (FileStream writer = new FileStream("config.tally", FileMode.Create, FileAccess.Write))
        {
            using (StreamWriter sw = new StreamWriter(writer, Encoding.UTF8))
            {
                sw.Write("# This is a YML file format\n" + yaml);
            }
        }
    }

    private static ProgramConfig LoadConfig()
    {
        Console.WriteLine("Loading config file");

        ProgramConfig? config;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using (FileStream reader = new FileStream("config.tally", FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(reader, Encoding.UTF8))
            {
                // Deserialize the YAML file into the ProgramConfig object
                config = deserializer.Deserialize<ProgramConfig>(sr);
                if (config == null)
                {
                    throw new Exception("Failed to load config file");
                }
            }
        }

        return config;
    }
}
