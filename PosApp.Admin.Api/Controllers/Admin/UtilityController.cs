using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Services.Hubs;
using PosApp.Admin.Api.Helpers;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class UtilityController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IUtilityService _utilityService;

        public UtilityController(
            IUtilityService utilityService,
            IOptions<AppSettings> appSettings)
        {
            _utilityService = utilityService;
            _appSettings = appSettings.Value;
        }

        [HttpGet("ClearCache")]
        public IActionResult ClearCache()
        {
            var result = _utilityService.ResetCache();
            StoreHelper.Caches.Clear();
            return Ok(result);
        }

        [HttpGet("Connections")]
        public IActionResult Connections()
        {
            var result = new
            {
                Users = NotifyHub.Users.Count,
                Connections = NotifyHub.Users.SelectMany(c => c.ConnectionIds).Count()
            };
            return Ok(result);
        }

        [HttpGet("Controllers")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Controllers()
        {
            var result = _utilityService.Controllers();
            return Ok(result);
        }

        [HttpGet("Actions")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Actions()
        {
            var result = _utilityService.Actions();
            return Ok(result);
        }

        [HttpGet("Actions/{by}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Actions([FromRoute] string by = default)
        {
            var result = _utilityService.Actions(by);
            return Ok(result);
        }

        [HttpGet("ConvertPdf")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult ConvertPdf()
        {
            //Hướng dẫnh 
            //Import file Spire.Doc.dll trong thư mục lib
            //cài nuget System.Drawing.Common 7.0
            //cài nuget System.Security.Cryptography.Xml 7.0.1
            //cài nuget System.Security.Permissions 7.0
            try
            {
                //string licenseKey = "Idd65VXxKpEAgBvZ1nVhUN+w7vpItcbvurq9YsmKuDda+JAEE9qF2G4YzR3o0s96HLaSfKKXq8fmv/VifgjLP/ZHrAKRewKyimE+b1l5tI82tdsWa+W3TgkLfepngT3Ui+LuaUc8pxXYEPd/bacNeg6yvWi7xVPzxDsE/m3D+OyD1ifz4S4lkOhjUS4pJ9gIKv6eIx0aXzRyczi4c+55+yRRBjUsB3AUS5C4sGq4LaSbeVLRq52visiCeMQxIkO6G38uTOyJl3mplKPrB3tpSTpmDc0j1WLuce1KIA9GbtKqOgh5vJwnXnwR3qeVgEBY2Lgrt6Gu0RModahYN6N5ODyj526SSOsz50jUQsrjfnk2JYKq3D3GA+lshknDJsSyHHkqYNxXfha7GQ4e11FhxALPu81LBXLXez4l73XCV9n6cdvHnyOerI18clWh/g6lgfEG+N+ugko2oxET/WEeIVKoIvpEw9YMv5bQrD6oWlN5GthgiXawtPQ6kM41r0MKW75+6ojDqRbOqvyVwC4HNRf2MXjni/Bdo0KBG3SD119bQfa+4zBREiEz6X26Mv7Tc0n8YzGTcK7VZcRGqI06bp4RDiFvAMrn4Y83gJaVRX6MbSJqwpKXKugSrmf0ck6XzLmhQcjsznnLkToXxvBS2jh6Vy3JZXvt4l8JUF8zE9CPix+kpDcGedXA1MmN/dju6Ps4sgGGAnjrfl1YLHvbQR8kii+h9tKrUrjTT88xvjjwz5IXmC4MX2A6HjSqabQwLVm8wfwNF22Pp1nMuX5DVP2pyNMMYMHIewGlJRSQz3j/7gVbw264aeBJPGyVpxrZCRO7byu/Z8cKTk02S+vZTazhIjV4jmn8zLOsxH0wsbcEpDLw1XnrH4tUiIRDQxRO+EBtpPklyFx9Q8AYkIv91osUiQZ14MXfysJ8oHG8gqHa7uidcd+YgFc3FRlFlVXYqqQlABFg5/ZvUHUklZdiRLenTb2yfl3RffnzA1aevJcLy2sBoWUrTxZlAFu0u8D2+swu0V3juiLM8pO9VDB4gHtQh3n/cnvShuv8hls2fi0TTZvpxLdfBw==";
                ////Load License
                //Spire.Doc.License.LicenseProvider.SetLicenseKey(licenseKey);
                //Spire.Doc.License.LicenseProvider.LoadLicense();

                ////Load Document
                //Document document = new Document();

                //var url = "https://calibre-ebook.com/downloads/demos/demo.docx";
                //using (MemoryStream ms = new())
                //{
                //    using (HttpClient client = new())
                //    {
                //        client.GetStreamAsync(url).Result.CopyTo(ms);
                //        document.LoadFromStream(ms, FileFormat.Docx);
                //    }
                //}

                //string contentType = "application/pdf";
                //using (MemoryStream stream = new())
                //{
                //    document.SaveToStream(stream, FileFormat.PDF);
                //    var content = stream.ToArray();
                //    return new FileContentResult(content, contentType);
                //}
                return Ok(0);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex);
            }
        }

        [HttpGet("CertInfo")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CertInfo([FromQuery] string serial)
        {
            string soapEndpoint = "http://192.168.1.200:8080/DigitalSignServerWs/SignServerWSService?wsdl";
            string soapRequest = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ws=""http://ws.viettel.com/"">
                                   <soapenv:Header/>
                                   <soapenv:Body>
                                      <ws:getCertInfo>
                                         <!--Optional:-->
                                         <arg0>benhvien</arg0>
                                         <!--Optional:-->
                                         <arg1>12345678aA@</arg1>
                                         <!--Optional:-->
                                         <arg2>{serial}</arg2>
                                      </ws:getCertInfo>
                                   </soapenv:Body>
                                </soapenv:Envelope>";
            using (var httpClient = new HttpClient())
            {
                // Set the content type and SOAPAction header
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "getCertInfo");

                // Make the SOAP request
                var response = await httpClient.PostAsync(soapEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    // Read and handle the SOAP response
                    string soapResponse = await response.Content.ReadAsStringAsync();
                    if (!soapResponse.IsStringNullOrEmpty())
                    {
                        //Convert XML to JSON
                        XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(soapResponse));
                        var xmlWithoutNs = xmlDocumentWithoutNs.ToString();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlWithoutNs);
                        var result = JsonConvert.SerializeXmlNode(doc.ChildNodes[0].ChildNodes[0].ChildNodes[0], Newtonsoft.Json.Formatting.None, true);
                        if (!result.IsStringNullOrEmpty())
                        {
                            return Ok(JObject.Parse(result));
                        }
                    }
                    return Ok(soapResponse);

                }
                else
                {
                    return Ok($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }

        private string CorrectFileName(string value)
        {
            if (value.IsStringNullOrEmpty())
                return string.Empty;
            value = value.ToNoSign().Replace("/", "-").Replace("#", "-").Replace("?", "-");
            value = Regex.Replace(value, "[^a-zA-Z0-9_./]+", "-", RegexOptions.Compiled);
            while (value.Contains("--")) value = value.Replace("--", "-");
            value = value.Trim(new[] { ' ', '-', '.' });
            value = value.ToLower();
            return value;
        }
    }
}
