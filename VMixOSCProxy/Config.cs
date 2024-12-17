using System.Xml.Serialization;
using CoreOSC;

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

        [XmlElement(ElementName = "updaterate")]
        public int UpdateRate { get; set; }
    }

    [XmlRoot(ElementName = "config")]
    public class Config
    {
        [XmlElement(ElementName = "vmix")]
        public required Vmix Vmix { get; set; }

        [XmlElement(ElementName = "osc")]
        public required Osc Osc { get; set; }
    }
}
