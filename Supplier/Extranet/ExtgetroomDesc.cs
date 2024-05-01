using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtgetroomDesc : IDisposable
    {
        XElement reqTravayoo;
        #region Room's Description (XML OUT for Travayoo)
        public XElement getroomDesc(XElement req)
        {
            try
            {
                XElement roomResponse = null;
                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                #region Extranet Request/Response                
                string requestxml = string.Empty;
                #region Credentials
                string exAgentID = string.Empty;
                string exUserName = string.Empty;
                string exPassword = string.Empty;
                string exServiceType = string.Empty;
                string exServiceVersion = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "3");
                exAgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
                exUserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
                exPassword = suppliercred.Descendants("Password").FirstOrDefault().Value;
                exServiceType = suppliercred.Descendants("ServiceType").FirstOrDefault().Value;
                exServiceVersion = suppliercred.Descendants("ServiceVersion").FirstOrDefault().Value;
                #endregion
                requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                              "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                               "<Authentication>" +
                                 "<AgentID>" + exAgentID + "</AgentID>" +
                                  "<UserName>" + exUserName + "</UserName>" +
                                  "<Password>" + exPassword + "</Password>" +
                                  "<ServiceType>" + exServiceType + "</ServiceType>" +
                                  "<ServiceVersion>" + exServiceVersion + "</ServiceVersion>" +
                                "</Authentication>" +
                              "</soapenv:Header>" +
                              "<soapenv:Body>" +
                                "<HotelDetailRequest>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<HotelNo>" + req.Descendants("HotelID").FirstOrDefault().Value + "</HotelNo>" +
                                  "<RoomNo>" + req.Descendants("RoomID").FirstOrDefault().Value + "</RoomNo>" +
                                "</HotelDetailRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                #endregion
                object result = extclient.HotelDetailResponse(requestxml);
                if (result != null)
                {

                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomDesc";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "getroomDesc";
                        ex1.PageName = "ExtgetroomDesc";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    XElement doc = XElement.Parse(result.ToString());
                    List<XElement> hotelavailabilityres = doc.Descendants("Hotel").ToList();
                    XElement supresponse = hotelavailabilityres.FirstOrDefault();
                    #region Get Room Response
                    IEnumerable<XElement> request = req.Descendants("roomDescRequest").ToList();
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                    string supplierid = req.Descendants("SupplierID").Single().Value;                   
                    #region XML OUT
                        roomResponse = new XElement(
                          new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement(soapenv + "Header",
                                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                     new XElement("Authentication",
                                         new XElement("AgentID", AgentID),
                                         new XElement("UserName", username),
                                         new XElement("Password", password),
                                         new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))),
                                            new XElement(soapenv + "Body",
                                                new XElement(request.Single()),
                                                   new XElement("roomDescResponse",
                                                       new XElement("Hotels",
                                                           new XElement("Hotel",
                                                               new XElement("Rooms",
                                                                   new XElement("Room",
                                                                       new XAttribute("ID", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Attribute("RoomTypeId").Value)),
                                                                       new XAttribute("RoomType", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Attribute("RoomTypeName").Value)),
                                                                       new XAttribute("MaxOccupancy", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Attribute("MaxOccupency").Value)),
                                                                       new XAttribute("Size", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Attribute("Size").Value)),
                                                                       new XElement("Desc", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Descendants("Desc").FirstOrDefault().Value)),
                                                                       new XElement("OtherDesc", Convert.ToString(supresponse.Descendants("Room").FirstOrDefault().Descendants("Additional").FirstOrDefault().Value)),
                                                                       new XElement("images", hotelimagesExtranet(supresponse.Descendants("Room").FirstOrDefault().Descendants("Image").ToList())),
                                                                       new XElement("Amenities", amentyExtranet(supresponse.Descendants("Room").FirstOrDefault().Descendants("Amenity").ToList()))                                                                       
                                                                       )))                                                           
                                          )))));                    
                    #endregion
                    #endregion
                }
                else
                {
                    #region No Result
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomDesc";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "getroomDesc";
                        ex1.PageName = "ExtgetroomDesc";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    #endregion
                }
                return roomResponse;
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getroomDesc";
                ex1.PageName = "ExtgetroomDesc";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                IEnumerable<XElement> request = req.Descendants("roomDescRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement searchdoc = new XElement(
                  new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication",
                                 new XElement("AgentID", AgentID),
                                 new XElement("UserName", username),
                                 new XElement("Password", password),
                                 new XElement("ServiceType", ServiceType),
                                 new XElement("ServiceVersion", ServiceVersion))),
                             new XElement(soapenv + "Body",
                                 new XElement(request.Single()),
                       new XElement("roomDescResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
        }
        #endregion
        #region Room Images Extranet
        public IEnumerable<XElement> hotelimagesExtranet(List<XElement> images)
        {
            Int32 length = images.Count();
            List<XElement> image = new List<XElement>();
            if (length == 0)
            {
                return null;
            }
            else
            {
                for(int i=0;i<length;i++)
                {
                    image.Add(new XElement("image",
                        new XAttribute("path", Convert.ToString(images[i].Value)),
                      new XAttribute("caption", Convert.ToString(""))));
                }
            }
            return image;
        }
        #endregion
        #region Room Amenities
        public IEnumerable<XElement> amentyExtranet(List<XElement> amenities)
        {
            Int32 length = amenities.Count();
            List<XElement> amenity = new List<XElement>();
            if (length == 0)
            {
                return null;
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    amenity.Add(new XElement("Amenity", Convert.ToString(amenities[i].Value)));
                }
            }
            return amenity;
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}