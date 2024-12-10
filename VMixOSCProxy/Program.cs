// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;
//XML
using System.Xml;

Console.WriteLine("Hello, World!");

//localhost on port 8099
var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8099);

using TcpClient client = new();
await client.ConnectAsync(ipEndPoint);
await using NetworkStream stream = client.GetStream();

//read initial version ok packet
string versionOk = ReadFromStream(stream);

//send initial tally subscription message
//await SendPacket(stream, "SUBSCRIBE TALLY");

//get the info of all the inputs
string rawXML = await SendPacket(stream, "XML");
Console.WriteLine(rawXML);
//parse the XML
//XmlDocument doc = new();
//doc.LoadXml(rawXML);
//print it
//Console.WriteLine(doc.OuterXml);

/* while (true)
{
    string _Message = ReadFromStream(stream);

    Console.Write(_Message);
} */

static async Task<string> SendPacket(NetworkStream stream, string message)
{
    var messageBytes = Encoding.UTF8.GetBytes(message + "\r\n");
    await stream.WriteAsync(messageBytes);
    return ReadFromStream(stream);
}

static string ReadFromStream(NetworkStream stream)
{
    string _Message = "";
    while (true)
    {
        // Create Byte to Receive Data:
        byte[] _Buffer = new byte[1024];
        // Create integer to hold how large the Data Received is:
        int _DataReceived = stream.Read(_Buffer, 0, _Buffer.Length);
        // Convert Buffer to a String:
        _Message += Encoding.ASCII.GetString(_Buffer);

        //watch for \r\n
        if (_Message.EndsWith("\r\n"))
        {
            //if its a XML message, we need to wait for one more \r\n
            if (_Message.StartsWith("XML"))
            {
                continue;
            }
            else
            {
                break;
            }
        }
    }

    return _Message;
}

/* var message = $"📅 {DateTime.Now} 🕛";
var dateTimeBytes = Encoding.UTF8.GetBytes(message);
//await stream.WriteAsync(dateTimeBytes);

//Console.WriteLine($"Sent message: \"{message}\""); */
