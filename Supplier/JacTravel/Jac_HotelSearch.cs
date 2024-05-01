
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_HotelSearch
    {
        int totalroom = 1;
       


        public IEnumerable<XElement> SearchFun(string value, XElement searchre, XElement Htl_static, XElement mealtype, int duration, XElement Facility,int CountryId)
        {

            string ClientTxt = string.Empty;
            RequestClass requobj = new RequestClass();
            XElement suppliercred = supplier_Cred.getsupplier_credentials(searchre.Descendants("CustomerID").FirstOrDefault().Value, "8");
            string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
            string SearchResponse = requobj.HttpPostRequest(url, searchre, value, "CXLPolicy",8,3);         
          
            XElement doc = XElement.Parse(SearchResponse);          

           
            Jac_HotelAvail obj = new Jac_HotelAvail();
            return obj.GetSerachResponce(doc, duration, Htl_static, mealtype, totalroom, Facility,0,null,"JacTravels",8,"");

        }
        
   
    }
}
