using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MQTTnet;
using MQTTnet.Client;

using Microsoft.Extensions.Logging;
using System.Text;

namespace cereal_killers;

public class build_successful
{
    private readonly ILogger _logger;
    private readonly StringReader reader;
    private static string _broker_Url = "broker.emqx.io";

    public build_successful(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<build_successful>();
    }

    [Function("build_successful")]
    public async Task<HttpResponseData> RunSuccessful([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("process successful");
        

        var response = req.CreateResponse(HttpStatusCode.OK);
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        response.Body = req.Body;

        _logger.LogInformation(response.Body.ToString());
        await PublishPipelineStatus("green");

        return response;
    }

    [Function("build_failed")]
    public async  Task<HttpResponseData> RunFailed([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("process failed");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Body = req.Body;
        _logger.LogInformation(response.Body.ToString());
        await PublishPipelineStatus("red");

        return response;
    }

    [Function("build_hasWarnings")]
    public async  Task<HttpResponseData> RunsWithWarning([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("process has warnings");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Body = req.Body;
        _logger.LogWarning(response.Body.ToString());
        await PublishPipelineStatus("yellow");

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
    public static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}
