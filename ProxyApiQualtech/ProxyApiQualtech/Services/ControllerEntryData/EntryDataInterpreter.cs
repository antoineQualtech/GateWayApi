
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProxyApiQualtech.Model;
using ProxyApiQualtech.Services.FileWriter;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ProxyApiQualtech.Services.ControllerEntryData
{
    public struct QualtechInternalHttpRequesterResponse
    {
        public bool IsSuccess { get; set; }
        public string ResponseData { get; set; }
    }

    public struct EpicorErrorResponse
    {
        public int HttpStatus { get; set; }
        public string ReasonPhrase { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EntryDataInterpreter : IEntryDataInterpreter
    {
        //les credentials epicor
        private static string bearer;
        private readonly IConfiguration _config;
        private IFileWriter _filewriter;

        public EntryDataInterpreter(IConfiguration configuration, IFileWriter fileWriter)
        {
            _config = configuration;
            bearer = "";
            _config = configuration;
            _filewriter = fileWriter;
        }

        /// <summary>
        /// Construire l'appel d'api interne épicor
        /// </summary>
        /// <param name="entryData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<QualtechInternalHttpRequesterResponse> QualtechInternalHttpRequester(EntryDataModel entryData)
        {
            QualtechInternalHttpRequesterResponse qualtechInternalHttpRequesterResponse = new QualtechInternalHttpRequesterResponse();

            //return response data
            string responseRet = string.Empty;

            //pour l'instant les certificats sont invalides à l'interne donc enlevé la validation ssl
            HttpClientHandler clientHandler = CreateHandlerToRemoveCert();

            HttpClient httpClient = null;

            //message de réponse 
            HttpResponseMessage response = null;

            //si succes 
            bool successEpicorResponse = false;

            //si épicor endpoint générer un bearer
            if (entryData.IsEpicorApiEndPoint)
            {
                //générer bearer pour api epicor
                string apiBearer = await GenerateEpicorApiBearer(entryData.EpicorEnvironnement.ToString(), entryData);
                if (apiBearer == null)
                {
                    qualtechInternalHttpRequesterResponse.IsSuccess = false;
                    qualtechInternalHttpRequesterResponse.ResponseData = "Could not generate epicor API bearer token for : " + entryData.UrlEndPoint.ToString();

                    return qualtechInternalHttpRequesterResponse;
                } 

                //générer un client avec les bons headers
                HttpClient client = QualtechInternalHttpRequestHeadersBuilder(entryData, clientHandler, apiBearer);

                //le contenu de la requête
                var postDict = new Dictionary<string, string>();

                foreach ( KeyValuePair<string, object> entry in entryData.RequestBody)
                {
                    if( entry.Value != null)
                    {
                        postDict[entry.Key] = entry.Value.ToString();
                    }
                    else
                    {
                        postDict[entry.Key] = null;
                    }
                };

                // Serialize le json on indente le json interne en string
                string postData = JsonConvert.SerializeObject(postDict, Formatting.Indented);
                var content = new StringContent(postData, Encoding.UTF8, "application/json");

                if (entryData.RequestType == Constants.HttpRequestTypes.GET)
                {
                    response = await client.GetAsync(entryData.UrlEndPoint);
                    responseRet = await response.Content.ReadAsStringAsync();
                }
                else if (entryData.RequestType == Constants.HttpRequestTypes.PUT)
                {
                    response = await client.PutAsync(entryData.UrlEndPoint, content);
                    responseRet = await response.Content.ReadAsStringAsync();
                }
                else if (entryData.RequestType == Constants.HttpRequestTypes.POST)
                {
                    
                    response = await client.PostAsync(entryData.UrlEndPoint, content);
                    responseRet = await response.Content.ReadAsStringAsync();
                    
                }
                //pas utilisé pour l'instant
                else if (entryData.RequestType == Constants.HttpRequestTypes.PATCH)
                {
                    response = await client.PatchAsync(entryData.UrlEndPoint, content);
                    responseRet = await response.Content.ReadAsStringAsync();
                }
                else if (entryData.RequestType == Constants.HttpRequestTypes.DELETE)
                {
                    response = await client.DeleteAsync(entryData.UrlEndPoint);
                    responseRet = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    successEpicorResponse = false;
                }

                //valider si appel épicor réussi
                successEpicorResponse = await this.EpicorValidateIfSuccessfulResponse(responseRet);

            }
            else {
                
            }

            qualtechInternalHttpRequesterResponse.IsSuccess = successEpicorResponse;
            qualtechInternalHttpRequesterResponse.ResponseData = responseRet;

            return qualtechInternalHttpRequesterResponse;
        }

        /// <summary>
        /// Aller chercher le message d'erreur dans la réponse de l'API interne EPICOR.
        /// Si HttpStatus >= 400, ErrorMessage contiendra le message d'erreur.
        /// </summary>
        public async Task<bool> EpicorValidateIfSuccessfulResponse(string responseString)
        {
            bool isEpicorResponseSuccessful = true;

            EpicorErrorResponse epicorErrorResponse = new EpicorErrorResponse();

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                // HttpStatus (obligatoire pour savoir si erreur ou non)
                if (root.TryGetProperty("HttpStatus", out JsonElement httpStatusElement))
                {
                    epicorErrorResponse.HttpStatus = httpStatusElement.GetInt32();
                }

                if(epicorErrorResponse.HttpStatus >= 400)
                {
                    isEpicorResponseSuccessful = false;
                }

            }
            catch (Exception ex)
            {
                //si erreur probablement pas un format attendu
                isEpicorResponseSuccessful = false;
            }

            return isEpicorResponseSuccessful;
        }


        /// <summary>
        /// Permet d'éviter les certificats invalide ssl
        /// </summary>
        /// <returns></returns>
        public HttpClientHandler CreateHandlerToRemoveCert()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true
            };
            return handler;
        }


        /// <summary>
        /// Permet de créer la requête d'entrée dans qualtech interne
        /// </summary>
        /// <param name="entryData"></param>
        public HttpClient QualtechInternalHttpRequestHeadersBuilder(EntryDataModel entryData, HttpClientHandler? clientHandler, string? bearerToken)
        {
            //si pu de client handler
            HttpClient client = new HttpClient();
            if (clientHandler != null)
            {
                client = new HttpClient(clientHandler);
            }

            //création dynamique header
            Dictionary<string, string> headers = entryData.InternalRequestHeaders;
            foreach (KeyValuePair<string, string> header in headers)
            {
                if(header.Key == "Content-Type")
                {
                    //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(header.Value));
                }
                else
                {
                    if (header.Value != "" || header.Value != null)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
     
            }

            //bearer epicor
            if (bearerToken != null)
            {
                //bearer et apikey
                client.DefaultRequestHeaders.Add("Authorization", ("Bearer " + bearerToken));
                client.DefaultRequestHeaders.Add("X-API-Key", this._config["Epicor_X_API_KEY"].ToString());
            }

            return client;
        }


        public async Task<string> GenerateEpicorApiBearer(string epiEnv, EntryDataModel entryData)
        {

            //pour l'instant les certificats sont invalides à l'interne donc enlevé la validation ssl
            HttpClientHandler clientHandler = CreateHandlerToRemoveCert();

            string url = "";
            //si prod ou pas
            if (entryData.UrlEndPoint.Contains(this._config["EpicorServerPROD"].ToString()))
            {
                url = $"{this._config["Protocol"].ToString()}://{this._config["EpicorServerPROD"].ToString()}/{epiEnv}/TokenResource.svc";
            }
            else
            {
                url = $"{this._config["Protocol"].ToString()}://{this._config["EpicorServerDEV"].ToString()}/{epiEnv}/TokenResource.svc";
            }


            string bearerToken = string.Empty;
            string tokenType = string.Empty;

            using (var client = new HttpClient(clientHandler))
            {
                // Set request headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-Key", this._config["Epicor_X_API_KEY"].ToString());
                client.DefaultRequestHeaders.Add("Username", this._config["EpicorUsername"].ToString());
                client.DefaultRequestHeaders.Add("Password", this._config["EpicorPassword"].ToString());

                // Set the content for the request
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded");

                // Make the request
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    _filewriter.WriteLogFile("Could not generate epicor API bearer token for : " + url);
                    Console.WriteLine("Incapable de créer bearer token pour api Epicor. StatusCode:" + response.StatusCode + " " + DateTime.Now);
                    Console.ResetColor();
                    return null;
                }

                string returnString = await response.Content.ReadAsStringAsync();
                Dictionary<string, object> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(returnString);

                // Extract token
                if (data.TryGetValue("AccessToken", out var token))
                {
                    bearerToken = token.ToString();
                }
            }
            return bearerToken;
        }

     
    }
}

