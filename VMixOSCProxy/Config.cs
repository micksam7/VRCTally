using System.Xml.Serialization;

namespace ConfigXML
{
    [XmlRoot(ElementName = "vmix")]
    public class Vmix
    {
        [XmlElement(ElementName = "ip")]
        public string Ip { get; set; }

        [XmlElement(ElementName = "port")]
        public int Port { get; set; }

        [XmlElement(ElementName = "username")]
        public string Username { get; set; }

        [XmlElement(ElementName = "password")]
        public string Password { get; set; }

        [XmlElement(ElementName = "tally")]
        public string Tally { get; set; }
    }

    [XmlRoot(ElementName = "osc")]
    public class Osc
    {
        [XmlElement(ElementName = "ip")]
        public string Ip { get; set; }

        [XmlElement(ElementName = "port")]
        public int Port { get; set; }

        [XmlElement(ElementName = "parameters")]
        public Parameters Parameters { get; set; }
    }

    [XmlRoot(ElementName = "parameters")]
    public class Parameters
    {
        [XmlElement(ElementName = "preview")]
        public string Previewparameter { get; set; }

        [XmlElement(ElementName = "program")]
        public string Programparameter { get; set; }

        [XmlElement(ElementName = "standby")]
        public string Standbyparameter { get; set; }

        [XmlElement(ElementName = "heartbeat")]
        public string Heartbeatparameter { get; set; }
    }

    [XmlRoot(ElementName = "config")]
    public class Config
    {
        [XmlElement(ElementName = "vmix")]
        public Vmix Vmix { get; set; }

        [XmlElement(ElementName = "osc")]
        public Osc Osc { get; set; }

        [XmlElement(ElementName = "updaterate")]
        public int UpdateRate { get; set; }
    }
}
