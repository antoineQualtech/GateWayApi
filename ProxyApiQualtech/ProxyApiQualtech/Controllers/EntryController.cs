using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using ProxyApiQualtech.Model;
using ProxyApiQualtech.Services.ControllerEntryData;
using ProxyApiQualtech.Services.FileWriter;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProxyApiQualtech.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("fixed")]
    public class EntryController : ControllerBase
    {

        private IEntryDataInterpreter _interpreter;
        private IFileWriter _filewriter;
        private readonly IConfiguration _config;
        private readonly ILogger<EntryController> _logger;

        public EntryController(IEntryDataInterpreter interpreter, IConfiguration configuration,IFileWriter fileWriter, ILogger<EntryController> logger)
        {
            _interpreter = interpreter;
            _config = configuration;
            _filewriter = fileWriter;
            _logger = logger;
        }
        //url api epicor 10.4.100.93:443

        [HttpPost("[action]")]
        public async Task<IActionResult> EntryPoint([FromBody] EntryDataModel entryData, [FromHeader(Name = "API_KEY")] string? apikey)
        {
            

            // logging d'ip
            string ipDist = HttpContext.Connection.RemoteIpAddress?.ToString();
            string portDist = HttpContext.Connection.RemotePort.ToString();
            string iplocal = HttpContext.Connection.LocalIpAddress?.ToString();
            string portlocal = HttpContext.Connection.LocalPort.ToString();
    
            _filewriter.WriteLogFile("");
            _filewriter.WriteLogFile("At " + DateTime.Now + " accessed entry point on: " + iplocal + ":" + portlocal + " from: " + ipDist + ":" + portDist);
            _filewriter.WriteLogFile("At " + DateTime.Now + ipDist + ":" + portDist + " accessed api point " + entryData.UrlEndPoint);

            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;

            //_logger.LogInformation("At " + DateTime.Now + " accessed entry point on: " + iplocal + ":" + portlocal + " from: " + ipDist + ":" + portDist +"\n"+ "At " + DateTime.Now + ipDist + ":" + portDist + " accessed api point " + entryData.UrlEndPoint);
            Console.WriteLine("");
            Console.WriteLine("At " + DateTime.Now + " accessed entry point on: " + iplocal + ":" + portlocal + " from: " + ipDist + ":" + portDist);
            Console.WriteLine("At " + DateTime.Now + ipDist + ":" + portDist + " accessed api point " + entryData.UrlEndPoint);
            Console.ResetColor();

            //ip null on ferme
            if (ipDist == null)
            {
                return ErrorResult(
                    StatusCodes.Status400BadRequest,
                    "INVALID_CLIENT_IP",
                    "Client IP address could not be determined."
                );
            }

            //if api key est null on ferme
            if (apikey != _config["API_KEY"]?.ToString())
            {
                return ErrorResult(
                    StatusCodes.Status401Unauthorized,
                    "MISSING_API_KEY",
                    "The API key header (API_KEY) is required."
                );
            }

            //validation api key
            if (apikey != _config["API_KEY"].ToString())
            {
                return ErrorResult(
                    StatusCodes.Status401Unauthorized,
                    "INVALID_API_KEY",
                    "The provided API key is invalid."
                );
            }

            //si entry data null on ferme
            if (entryData == null)
            {
                return ErrorResult(
                    StatusCodes.Status400BadRequest,
                    "INVALID_ENTRY_DATA",
                    "The request body (EntryData) is required and could not be parsed."
                );
            }

            List<string> list = _config.GetSection("AllowedEpicorEndpoints").Get<List<string>>();
            //vérifie si l'url de destination est white listé
            if (!list.Any(listItem => entryData.UrlEndPoint.Contains(listItem)))
            {
                return ErrorResult(
                    StatusCodes.Status403Forbidden,
                    "ENDPOINT_NOT_WHITELISTED",
                    "The requested endpoint is not allowed by the gateway.",
                    new { requestedEndpoint = entryData.UrlEndPoint }
                );
            }

            if (entryData.UrlEndPoint.Contains("BTMobileDataEntryMsSql"))
            {
               // _filewriter.WriteLogFile("SalesOrderSvc endpoint accessed " + entryData.UrlEndPoint + " " + DateTime.Now);
            }

            //envoyer la requête à l'interne et attendre le retour
            QualtechInternalHttpRequesterResponse retData = await _interpreter.QualtechInternalHttpRequester(entryData);

            //si retData null on ferme
            if (retData.IsSuccess == false )
            {
                return ErrorResult(
                    StatusCodes.Status502BadGateway,
                    "INTERNAL_GATEWAY_ERROR",
                    "The gateway did not receive a valid response from the internal service. : " +retData.ResponseData
                );
            }

            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("At " + DateTime.Now + ipDist + ":" + portDist + " http request ended");
            Console.WriteLine("-----------------------------------");
            Console.ResetColor();
            Console.WriteLine("");

            return Ok(retData.ResponseData);
        }

        /// <summary>
        /// Permet de tester temporairement l'api
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> Test()
        {
           /* if (!EventLog.SourceExists("ApiGatewayCustomLogs"))
            {
                EventLog.CreateEventSource("ApiGatewayCustomLogs", "Application");
            }
            EventLog.WriteEntry("ApiGatewayCustomLogs", "test", EventLogEntryType.Information);
            try {

                _filewriter.WriteLogFile("test " + DateTime.Now);
            }
            catch(Exception e)
            {
                if (!EventLog.SourceExists("ApiGatewayCustomLogs"))
                {
                    EventLog.CreateEventSource("ApiGatewayCustomLogs", "Application");
                }
                EventLog.WriteEntry("ApiGatewayCustomLogs", e.Message, EventLogEntryType.Information);
            }
            _filewriter.WriteLogFile("test " + DateTime.Now);

            //string ret = await _interpreter.GenerateEpicorApiBearer("erppilot");*/
            return Ok("test");
        }

        // Helper method for consistent error payloads + logging
        private IActionResult ErrorResult(int statusCode, string errorCode, string message, object? details = null)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string endpoint = HttpContext.Request.Path;
            string traceId = HttpContext.TraceIdentifier;

            // Build the structured error payload
            

            // Log to file
            _filewriter.WriteLogFile(
                $"[{DateTime.Now}] ERROR {statusCode} - {errorCode} - {message} | IP={clientIp} | Endpoint={endpoint} | TraceId={traceId}"
            );

            // Log to event logger
            _logger.LogWarning("GatewayError {@payload}", message);

            return StatusCode(statusCode, message);
        }


        // Helper for success result + logging
        private IActionResult SuccessResult(object result)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string endpoint = HttpContext.Request.Path;
            string traceId = HttpContext.TraceIdentifier;

            _filewriter.WriteLogFile(
                $"[{DateTime.Now}] SUCCESS 200 | IP={clientIp} | Endpoint={endpoint} | TraceId={traceId}"
            );

            _logger.LogInformation("GatewaySuccess {@result}", result);

            return Ok(result);
        }

    }
}
