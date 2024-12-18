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
    public Input PreviewInput =>
        Inputs?.Input.FirstOrDefault(i => i.Number == Preview) ?? new Input();

    [XmlElement(ElementName = "active")]
    public int Active { get; set; }
    public Input ActiveInput =>
        Inputs?.Input.FirstOrDefault(i => i.Number == Active) ?? new Input();

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

    [XmlIgnore]
    public TimeSpan deserializationTime { get; set; }

    //try to find the input with the name the user entered, otherwise print an error and skip everything else
    //we need to search with wildcard * in mind
    [XmlIgnore]
    public Input tallyInput =>
        Inputs?.Input.FirstOrDefault(i => i.Title.StartsWith(ProgramWindow.config.Vmix.Tally))
        ?? new Input();

    [XmlIgnore]
    public int xmlCharacterCount;

    [XmlIgnore]
    public bool valid = false;
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
}

[XmlRoot(ElementName = "inputs")]
public class Inputs
{
    [XmlElement(ElementName = "input")]
    public List<Input> Input { get; set; }
}
