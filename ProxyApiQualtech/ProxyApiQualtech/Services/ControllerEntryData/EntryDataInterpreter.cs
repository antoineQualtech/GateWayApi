
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using ProxyApiQualtech.Model;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProxyApiQualtech.Services.ControllerEntryData
{
    public class EntryDataInterpreter : IEntryDataInterpreter
    {
        //les credentials epicor
        private string epicorApiKey;
        private string taskUserName;
        private string taskPassword;
        private static string bearer;

        public EntryDataInterpreter()
        {
            epicorApiKey = "kEcTYTqXn4sKi20uctfbqsTEETbrqCyEUfmXiYbA8RPbJ";
            taskUserName = "TASK";
            taskPassword = "LuK^d6swSwj4";
            bearer = "";
        }

        /// <summary>
        /// Construire l'appel d'api interne épicor
        /// </summary>
        /// <param name="entryData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> QualtechInternalHttpRequester(EntryDataModel entryData)
        {

            //return response data
            string responseRet = string.Empty;

            //pour l'instant les certificats sont invalides à l'interne donc enlevé la validation ssl
            HttpClientHandler clientHandler = CreateHandlerToRemoveCert();

            HttpClient httpClient = null;

            //message de réponse 
            HttpResponseMessage response = null;

            //si épicor endpoint générer un bearer
            if (entryData.IsEpicorApiEndPoint)
            {
                //générer bearer pour api epicor


                string apiBearer = await GenerateEpicorApiBearer(entryData.EpicorEnvironnement.ToString());
                if (apiBearer == null) 
                    return null;
                

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
                    return null;
                }

            } else {
                HttpClient client = QualtechInternalHttpRequestHeadersBuilder(entryData, clientHandler, null);
                //le contenu de la requête

                var jsonContent = JsonConvert.SerializeObject(entryData.RequestBody.ToString());
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

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
                    return null;
                }
            }

            return responseRet;
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
                client.DefaultRequestHeaders.Add("X-API-Key", this.epicorApiKey);
            }

            return client;
        }


        public async Task<string> GenerateEpicorApiBearer(string epiEnv)
        {

            //pour l'instant les certificats sont invalides à l'interne donc enlevé la validation ssl
            HttpClientHandler clientHandler = CreateHandlerToRemoveCert();

            string url = $"https://qbcdeverpapp.qualtech.int/{epiEnv}/TokenResource.svc/";
            string bearerToken = string.Empty;
            string tokenType = string.Empty;

            using (var client = new HttpClient(clientHandler))
            {
                // Set request headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-Key", this.epicorApiKey);
                client.DefaultRequestHeaders.Add("Username", this.taskUserName);
                client.DefaultRequestHeaders.Add("Password", this.taskPassword);

                // Set the content for the request
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded");

                // Make the request
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
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

