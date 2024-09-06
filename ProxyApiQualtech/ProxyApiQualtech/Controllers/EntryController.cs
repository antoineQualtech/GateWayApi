using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using ProxyApiQualtech.Model;
using ProxyApiQualtech.Services.ControllerEntryData;
using ProxyApiQualtech.Services.FileWriter;
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

        public EntryController(IEntryDataInterpreter interpreter, IConfiguration configuration,IFileWriter fileWriter)
        {
            _interpreter = interpreter;
            _config = configuration;
            _filewriter = fileWriter;
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
            Console.WriteLine("");
            Console.WriteLine("At " + DateTime.Now + " accessed entry point on: " + iplocal + ":" + portlocal + " from: " + ipDist + ":" + portDist);
            Console.WriteLine("At " + DateTime.Now + ipDist + ":" + portDist + " accessed api point " + entryData.UrlEndPoint);
            Console.ResetColor();

            //ip null on ferme
            if (ipDist == null)
            {
                Console.WriteLine("Ip invalid close connexion " + DateTime.Now );
                _filewriter.WriteLogFile("Ip invalid close connexion " + DateTime.Now);
                return Unauthorized("Ip invalid");
            }

            //if api key est null on ferme
            if (apikey == null)
            {
                Console.WriteLine("Apikey invalid close connexion " + DateTime.Now);
                _filewriter.WriteLogFile("Apikey invalid close connexion " + DateTime.Now);
                return Unauthorized("Apikey invalid");
            }

            //validation api key
            if (apikey != _config["API_KEY"].ToString())
            {
                Console.WriteLine("Apikey invalid close connexion " + DateTime.Now);
                _filewriter.WriteLogFile("Apikey invalid close connexion " + DateTime.Now);
                return Unauthorized("Apikey invalid");
            }

            //si entry data null on ferme
            if (entryData == null) {
                Console.WriteLine("Entry Data invalid close connexion " + DateTime.Now);
                _filewriter.WriteLogFile("Entry Data invalid close connexion " + DateTime.Now);
                return BadRequest("EntryData invalid");
               
            }

            List<string> list = _config.GetSection("AllowedEpicorEndpoints").Get<List<string>>();
            //vérifie si l'url de destination est white listé
            if (!list.Any(listItem=> entryData.UrlEndPoint.Contains(listItem)))
            {
                Console.WriteLine("Endpoint not whitelisted close connexion   "+ entryData.UrlEndPoint+ " " + DateTime.Now);
                _filewriter.WriteLogFile("Endpoint not whitelisted close connexion  " + entryData.UrlEndPoint+" " + DateTime.Now);
                return Unauthorized("Endpoint not whitelisted");
            }

            //envoyer la requête à l'interne et attendre le retour
            var retData = await _interpreter.QualtechInternalHttpRequester(entryData);

            //si retData null on ferme
            if (retData == null)
            {
                Console.WriteLine("Something went wrong handling de request close connexion " + DateTime.Now);
                _filewriter.WriteLogFile("Something went wrong handling de request close connexion " + DateTime.Now);
                return BadRequest("EntryData invalid");
            }

            //si un erreur http 400 ou plus grand
            try
            {
                var jsonDoc = JsonDocument.Parse(retData);
                var root = jsonDoc.RootElement;

                // Extract the HttpStatus, ReasonPhrase, and ErrorMessage
    
                if (root.TryGetProperty("HttpStatus", out JsonElement httpStatusElement))
                {
                    int httpStatus = httpStatusElement.GetInt32();

                    if (httpStatus >= 400)
                    {
                        if (root.TryGetProperty("ReasonPhrase", out JsonElement reasonPhraseElement))
                        {
                            string reasonPhrase = reasonPhraseElement.GetString();
                            return BadRequest(reasonPhrase);
                        }
                        else
                        {
                            // Handle case where ReasonPhrase is missing
                            return BadRequest("Error occurred but no reason provided.");
                        }
                    }
                }
               
            }
            catch (Exception ex) {
                Console.WriteLine("Something went wrong handling de request close connexion " + DateTime.Now);
                _filewriter.WriteLogFile("Something went wrong handling de request close connexion " + DateTime.Now);
                return BadRequest("EntryData invalid");
            }

            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("At " + DateTime.Now + ipDist + ":" + portDist + " http request ended");
            Console.WriteLine("-----------------------------------");
            Console.ResetColor();
            Console.WriteLine("");

            return Ok(retData);
        }

        /// <summary>
        /// Permet de tester temporairement l'api
        /// </summary>
        /// <returns></returns>
       /* [HttpGet("[action]")]
        public async Task<IActionResult> Test()
        {
            string ret = await _interpreter.GenerateEpicorApiBearer("erppilot");
            return Ok(ret);
        }*/
    }
}
