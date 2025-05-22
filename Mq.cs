using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using WebScrappingTrades.Models;

namespace WebScrappingTrades
{
    public class Mq
    {
        private readonly IMqttClient client;
        private readonly string clientGuid;
        private readonly Startup _startup;
        public readonly ConcurrentQueue<string> _messageQueue = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Mq"/> class, setting up an MQTT client with a unique
        /// identifier.
        /// </summary>
        /// <remarks>The MQTT client is initialized with a unique identifier in the format
        /// "WebScrapping-{GUID}".</remarks>
        /// <param name="startup">The <see cref="Startup"/> instance used to configure the MQTT client.</param>
        public Mq(Startup startup)
        {
            _startup = startup;
            MqttFactory mqttFactory = new();
            client = mqttFactory.CreateMqttClient();
            clientGuid = $"WebScrapping-{Guid.NewGuid()}";
        }

        /// <summary>
        /// Starts the client processing thread.
        /// </summary>
        /// <remarks>This method initializes and starts a new thread named "ClientRunningThr" to handle
        /// client processing.  If an exception occurs during thread initialization or startup, the exception message is
        /// logged to the console.</remarks>
        public void Start()
        {
            try
            {
                Thread ClientRunningThread = new(ClientRunningThr)
                {
                    Name = "ClientRunningThr"
                };
                ClientRunningThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1x00: {ex.Message}");
            }
        }

        /// <summary>
        /// Establishes a connection to the MQTT server using the specified client configuration.
        /// </summary>
        /// <remarks>This method initializes the MQTT client with the provided client ID, server address,
        /// credentials, and other connection options. It also sets up event handlers for connection, disconnection, and
        /// message reception events. The method attempts to connect to the MQTT server asynchronously. If an exception
        /// occurs during the connection process, it is logged to the console.</remarks>
        /// <param name="obj">An optional parameter that can be used to pass additional data to the method. This parameter is not utilized
        /// in the current implementation.</param>
        public void ClientRunningThr(object? obj)
        {
            try
            {
                Console.WriteLine($"Client connecting to server: {clientGuid}");
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(clientGuid)
                    .WithTcpServer("MQ_server_IP", 1883)
                    .WithCredentials("username", "password")
                    .WithCleanSession()
                    .WithWillTopic($"/testing/JobController/Disconnect/{clientGuid}")
                    .WithWillPayload($"{clientGuid}: Client disconnected.")
                    .Build();
                client.ConnectedAsync += ClientConnectedAsync;
                client.DisconnectedAsync += ClientDisconnectedAsync;
                client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
                client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1x01: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the event triggered when an application message is received from the MQTT client.
        /// </summary>
        /// <remarks>This method processes the received MQTT message by extracting the topic and payload,
        /// and then delegating the handling of the message to an internal handler. Exceptions during processing are
        /// logged to the console.</remarks>
        /// <param name="arg">The event arguments containing details about the received MQTT application message, including the topic and
        /// payload.</param>
        /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                string message = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);
                string topic = arg.ApplicationMessage.Topic;
                HandleMessage(topic, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1x02: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes a message based on the specified topic and performs the appropriate action.
        /// </summary>
        /// <remarks>The method handles messages based on the topic as follows: <list type="bullet">
        /// <item> <description>If the topic contains "StartSetting", the message is deserialized into a <see
        /// cref="ScrapperSetting"/> object, and its value is used to retrieve settings asynchronously.</description>
        /// </item> <item> <description>If the topic contains "Job", the message is added to the internal message queue
        /// for further processing.</description> </item> <item> <description>If the topic does not match any known
        /// patterns, a warning is logged to the console.</description> </item> </list> Exceptions are caught and logged
        /// to the console to prevent the application from crashing due to unexpected errors.</remarks>
        /// <param name="topic">The topic associated with the message. This determines how the message will be handled.</param>
        /// <param name="message">The content of the message to be processed. This is expected to be in a format appropriate for the specified
        /// topic.</param>
        private void HandleMessage(string topic, string message)
        {
            try
            {
                if (topic.Contains("StartSetting"))
                {
                    var scrapperSetting = JsonConvert.DeserializeObject<ScrapperSetting>(message);
                    if (scrapperSetting != null)
                    {
                        _startup.GetSettingAsync(scrapperSetting.Value);
                    }
                }
                else if (topic.Contains("Job"))
                {
                    _messageQueue.Enqueue(message);
                }
                else
                {
                    Console.WriteLine($"Wrong topic -> {topic}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"0x06: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the event when an MQTT client disconnects from the server.
        /// </summary>
        /// <param name="arg">The event arguments containing details about the disconnection, including the reason.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task ClientDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            try
            {
                Console.WriteLine($"User disconnected from server. Reason: {arg.Reason}.");
                // do reconnect
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1x03: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Handles the event when a client successfully connects to the MQTT server.
        /// </summary>
        /// <remarks>This method subscribes the client to specific MQTT topics and sends a notification
        /// about the new connection. Ensure that the client is properly initialized before invoking this
        /// method.</remarks>
        /// <param name="arg">The event arguments containing details about the client connection.</param>
        /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ClientConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine($"Logged to MQ server.");
            client.SubscribeAsync($"/testing/WebScrappingTrades/StartSetting/{clientGuid}");
            client.SubscribeAsync($"/testing/WebScrappingTrades/Job/{clientGuid}");
            SendData(clientGuid, $"/testing/JobController/NewConnection/");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes a message to the specified MQTT topic.
        /// </summary>
        /// <remarks>The method constructs an MQTT application message using the provided topic and
        /// payload,  appends the client identifier to the topic, and publishes the message asynchronously.</remarks>
        /// <param name="toSend">The message payload to be sent. Cannot be null or empty.</param>
        /// <param name="topic">The topic to which the message will be published. Cannot be null or empty.</param>
        public void SendData(string toSend, string topic)
        {
            try
            {
                //Console.WriteLine($"Sending {topic}{clientGuid}: {toSend}");
                var applicationMessage = new MqttApplicationMessageBuilder()
               .WithTopic($"{topic}{clientGuid}")
               .WithPayload(toSend)
               .Build();
                client.PublishAsync(applicationMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"0x04: {ex.Message}");
            }
        }
    }
}