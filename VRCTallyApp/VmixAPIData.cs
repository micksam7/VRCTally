using System.Xml;
using System.Xml.Serialization;

[XmlRoot(ElementName = "vmix")]
public class VmixAPIData
{
    [XmlElement(ElementName = "version")]
    public string Version { get; set; } = "???";

    [XmlElement(ElementName = "edition")]
    public string? Edition { get; set; }

    [XmlElement(ElementName = "inputs")]
    public Inputs? Inputs { get; set; }

    [XmlElement(ElementName = "preview")]
    public int Preview { get; set; }

    [XmlElement(ElementName = "active")]
    public int Active { get; set; }

    [XmlElement(ElementName = "fadeToBlack")]
    public bool FadeToBlack { get; set; }

    [XmlElement(ElementName = "recording")]
    public bool Recording { get; set; }

    [XmlElement(ElementName = "external")]
    public bool External { get; set; }

    [XmlElement(ElementName = "streaming")]
    public bool Streaming { get; set; }

    [XmlElement(ElementName = "playList")]
    public bool PlayList { get; set; }

    [XmlElement(ElementName = "multiCorder")]
    public bool MultiCorder { get; set; }

    [XmlElement(ElementName = "fullscreen")]
    public bool Fullscreen { get; set; }

    [XmlElement(ElementName = "mix")]
    public List<Mix> Mixes { get; set; } = new();

    [XmlIgnore]
    public TimeSpan deserializationTime { get; set; }

    //try to find the input with the name the user entered, otherwise print an error and skip everything else
    public Input? FindInput(string title, bool exactMatch)
    {
        if (exactMatch)
        {
            return Inputs?.Input.FirstOrDefault(i => i.Title == title);
        }
        //this is nieve, this doesnt actually work as we care about the priority of the input itself
        /* return Inputs?.Input.FirstOrDefault(i => i.Title.StartsWith(title)); */
        Input? highestPriorityInput = null;
        foreach (Input input in Inputs?.Input ?? new List<Input>())
        {
            if (!input.Title.Contains(title))
            {
                continue;
            }

            //we want to update the input with the highest priority one
            Input.VMixState state = input.GetTallyState(this);

            if (state == Input.VMixState.Unknown)
            {
                continue;
            }

            if (highestPriorityInput == null)
            {
                highestPriorityInput = input;
            }
            else
            {
                //as long as the order in the enum doesnt change, this should work
                if (state < highestPriorityInput.GetTallyState(this))
                {
                    highestPriorityInput = input;
                }
            }
        }

        return highestPriorityInput;
    }

    public Input? FindInput(int number)
    {
        if (Inputs == null)
        {
            return null;
        }

        //find the input with the number
        return Inputs.Input.FirstOrDefault(i => i.Number == number);
    }

    [XmlIgnore]
    public int xmlCharacterCount;

    [XmlIgnore]
    public bool valid = false;

    public List<Input> GetAllActiveIDs()
    {
        List<Input> activeInputs = new();
        if (Inputs == null)
        {
            return activeInputs;
        }

        //add the easy one
        var basic = FindInput(Active);
        if (basic != null)
        {
            activeInputs.Add(basic);
        }

        //add in all the mixes
        foreach (Mix mix in Mixes)
        {
            var possibleActive = FindInput(mix.Active);
            if (possibleActive != null)
            {
                activeInputs.Add(possibleActive);
            }
        }

        return activeInputs;
    }

    public List<Input> GetAllPreviewIDs()
    {
        List<Input> previewInputs = new();
        if (Inputs == null)
        {
            return previewInputs;
        }

        //add the easy one
        var basic = FindInput(Preview);
        if (basic != null)
        {
            previewInputs.Add(basic);
        }

        //add in all the mixes
        foreach (Mix mix in Mixes)
        {
            var possiblePreview = FindInput(mix.Preview);
            if (possiblePreview != null)
            {
                previewInputs.Add(possiblePreview);
            }
        }

        return previewInputs;
    }
}

[XmlRoot(ElementName = "input")]
public class Input
{
    [XmlAttribute(AttributeName = "key")]
    public string Key { get; set; } = "???";

    [XmlAttribute(AttributeName = "number")]
    public int Number { get; set; } = -1;

    [XmlAttribute(AttributeName = "type")]
    public string Type { get; set; } = "???";

    [XmlAttribute(AttributeName = "title")]
    public string Title { get; set; } = "???";

    [XmlAttribute(AttributeName = "shortTitle")]
    public string ShortTitle { get; set; } = "???";

    [XmlAttribute(AttributeName = "state")]
    public string State { get; set; } = "???";

    [XmlAttribute(AttributeName = "position")]
    public int Position { get; set; }

    [XmlAttribute(AttributeName = "duration")]
    public int Duration { get; set; }

    [XmlAttribute(AttributeName = "loop")]
    public bool Loop { get; set; }

    [XmlText]
    public string Text { get; set; } = "???";

    public enum VMixState
    {
        Program = 0,
        Preview = 1,
        Standby = 2,
        Unknown = 3,
    }

    public VMixState GetTallyState(VmixAPIData data)
    {
        if (data == null || Number == -1)
        {
            return VMixState.Unknown;
        }

        var previews = data.GetAllPreviewIDs();
        var actives = data.GetAllActiveIDs();

        if (actives.Contains(this))
        {
            return VMixState.Program;
        }
        else if (previews.Contains(this))
        {
            return VMixState.Preview;
        }
        else
        {
            return VMixState.Standby;
        }
    }
}

[XmlRoot(ElementName = "inputs")]
public class Inputs
{
    [XmlElement(ElementName = "input")]
    public List<Input> Input { get; set; } = new();
}

[XmlRoot(ElementName = "mix")]
public class Mix
{
    [XmlElement(ElementName = "preview")]
    public int Preview { get; set; }

    [XmlElement(ElementName = "active")]
    public int Active { get; set; }
}
