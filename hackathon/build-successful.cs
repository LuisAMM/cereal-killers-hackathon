using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MQTTnet;
using MQTTnet.Client;

using Microsoft.Extensions.Logging;

namespace cereal_killers;

public class build_successful
{
    private readonly ILogger _logger;
    private static string _broker_Url = "broker.emqx.io";

    public build_successful(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<build_successful>();
    }

    [Function("build_successful")]
    public async Task<HttpResponseData> RunSuccessful([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("process successful");

        await PublishPipelineStatus("green");
        var response = req.CreateResponse(HttpStatusCode.OK);

        return response;
    }

    [Function("build_failed")]
    public async  Task<HttpResponseData> RunFailed([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("process failed");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await PublishPipelineStatus("red");

        return response;
    }

    public static async Task PublishPipelineStatus(string payload)
    {
        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_broker_Url)
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("cerealkillers/build")
                .WithPayload(payload)
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            await mqttClient.DisconnectAsync();
        }
    }
}
