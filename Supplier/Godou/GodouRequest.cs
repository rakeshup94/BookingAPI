using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Godou;

namespace TravillioXMLOutService.Supplier.Godou
{
    public class GodouRequest
    {
        
        public string GetServerResponse(string Request, string reqType, string parameter, string CustomerId)
        {
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(CustomerId, "31");
                string username = suppliercred.Descendants("Username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                string apiKey = suppliercred.Descendants("API_Key").FirstOrDefault().Value;
                string url = suppliercred.Descendants("url").FirstOrDefault().Value;
                url += reqType;
                if (parameter != string.Empty)
                {
                    url = url + "?" + parameter;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + svcCredentials);
                request.Headers.Add("WBE-Api-Key", apiKey);
                if (reqType.Equals("details"))
                    request.Method = "GET";
                else
                {
                    request.Method = "POST";
                    byte[] data = Encoding.ASCII.GetBytes(Request);
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    string supplierResponse = reader.ReadToEnd();
                    return supplierResponse;
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    string errorText = reader.ReadToEnd();
                }
                throw;
            }

        }
        public string ServiceResponses(string Request, string reqType, string parameter, string CustomerId)
        {
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(CustomerId, "31");
                string username = suppliercred.Descendants("Username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                string apiKey = suppliercred.Descendants("API_Key").FirstOrDefault().Value;
                string url = suppliercred.Descendants("url").FirstOrDefault().Value;
                url += reqType;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + svcCredentials);
                request.Headers.Add("WBE-Api-Key", apiKey);
                request.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(Request);
                request.ContentLength = data.Length;
                request.ContentType = "application/json";
                request.KeepAlive = true;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    string supplierResponse = reader.ReadToEnd();
                    return supplierResponse;
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    string errorText = reader.ReadToEnd();
                }
                throw;
            }

        }
    }
}