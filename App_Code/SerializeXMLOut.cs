using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TravillioXMLOutService.App_Code
{
    public class SerializeXMLOut
    {
        public string Serialize<T>(T response)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringWriter outStream = new StringWriter();
            serializer.Serialize(outStream, response);
            return outStream.ToString();
        }
    }
}