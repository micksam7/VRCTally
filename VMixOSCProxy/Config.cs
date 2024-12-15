using System.Xml.Serialization;

[XmlRoot(ElementName = "osc")]
public class Osc
{
    [XmlElement(ElementName = "previewparameter")]
    public string Previewparameter { get; set; }

    [XmlElement(ElementName = "programparameter")]
    public string Programparameter { get; set; }
}

[XmlRoot(ElementName = "config")]
public class Config
{
    [XmlElement(ElementName = "tally")]
    public string Tally { get; set; }

    [XmlElement(ElementName = "osc")]
    public Osc Osc { get; set; }
}
