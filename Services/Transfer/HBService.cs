using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.ModelBinding;
using System.Xml.Linq;
using TravillioXMLOutService.Models.DotW;
using TravillioXMLOutService.Models.Transfer.HB;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Transfer.Helper;

namespace TravillioXMLOutService.Services.Transfer
{
    public class HBService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        //private readonly JsonSerializerOptions _options;

        public HBService()
        {
            XElement _credentials = CommonHelper.ReadCredential("", "");
            _httpClient.BaseAddress = new Uri(_credentials.Element("BaseAddress").Value);
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Api-key", _credentials.Element("Key").Value);
            _httpClient.DefaultRequestHeaders.Add("X-Signature", _credentials.Element("Secret").Value);

            //_options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }



        public async Task<SearchResponseModel> GetSearchAsync(SearchModel model)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(model.BaseAddress);
                var data = $"transfer-api/1.0/availability/{model.language}/from/{model.ftype}/" +
                    $"{model.fcode}/to/{model.ttype}/{model.tcode}/{model.departing}/" +
                $"{model.adults}/{model.children}/{model.infants}";
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), data))
                {
                    //request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    //request.Headers.TryAddWithoutValidation("Api-key", model.Key);
                    //request.Headers.TryAddWithoutValidation("X-Signature", model.Secret);
                    var response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var stringResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<SearchResponseModel>(stringResponse);
                        return result;
                    }
                    else
                    {
                        throw new HttpRequestException(response.ReasonPhrase);
                    }
                }

            }



        }
    }
}