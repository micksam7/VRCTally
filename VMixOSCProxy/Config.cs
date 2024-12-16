using System.Xml.Serialization;

namespace ConfigXML
{
    [XmlRoot(ElementName = "vmix")]
    public class Vmix
    {
        [XmlElement(ElementName = "ip")]
        public required string Ip { get; set; }

        [XmlElement(ElementName = "port")]
        public int Port { get; set; }

        [XmlElement(ElementName = "username")]
        public required string Username { get; set; }

        [XmlElement(ElementName = "password")]
        public required string Password { get; set; }

        [XmlElement(ElementName = "tally")]
        public required string Tally { get; set; }
    }

    [XmlRoot(ElementName = "osc")]
    public class Osc
    {
        [XmlElement(ElementName = "parameters")]
        public required Parameters Parameters { get; set; }
    }

    [XmlRoot(ElementName = "parameters")]
    public class Parameters
    {
        [XmlElement(ElementName = "preview")]
        public required string Previewparameter { get; set; }

        [XmlElement(ElementName = "program")]
        public required string Programparameter { get; set; }

        [XmlElement(ElementName = "standby")]
        public required string Standbyparameter { get; set; }

        [XmlElement(ElementName = "heartbeat")]
        public required string Heartbeatparameter { get; set; }

        [XmlElement(ElementName = "error")]
        public required string Errorparameter { get; set; }
    }

    [XmlRoot(ElementName = "config")]
    public class Config
    {
        [XmlElement(ElementName = "vmix")]
        public required Vmix Vmix { get; set; }

        [XmlElement(ElementName = "osc")]
        public required Osc Osc { get; set; }

        [XmlElement(ElementName = "updaterate")]
        public int UpdateRate { get; set; }
    }
}
