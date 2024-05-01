using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_HtlDetail
    {
       


        public XElement Responce(XElement Responce)
        {
            
            //foreach (var item in xele.Descendants("HotelInformation"))
            //{
            //    XElement htlinfotag = xele.Element("HotelInformation");
            //   XElement Respon = new XElement("hoteldescResponse",
            // new XElement("Hotels",
            //     new XElement("Hotel",
            //         new XElement("HotelID", item.Element("ItemCode").Value),
            //         new XElement("Description", xele.Element("Description") != null ? xele.Element("Description").Value : string.Empty),
            //         new XElement("Images", BindImg(xele.Descendants("ImageFile"))),
            //                                            new XElement("ContactDetails",
            //                                                new XElement("Phone", xele.Element("PhoneNo") != null ? xele.Element("PhoneNo").Value : string.Empty),
            //                                                new XElement("Fax", xele.Element("FaxNo") != null ? xele.Element("FaxNo").Value : string.Empty)))));
            //   return htlinfotag;
            //}

            return null;
        }


        XElement BindImg(IEnumerable<XElement> imglst)
        {
            return null;
        }

    }

   
}
