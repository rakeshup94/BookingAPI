using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TravillioXMLOutService.Transfer.Services.Interfaces
{
    internal interface IHBService
    {
        Task<XElement> GetSearchAsync(XElement _travyoReq);
        
    }
}
