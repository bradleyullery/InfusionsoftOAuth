using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using CookComputing.XmlRpc;
using InfusionsoftOAuth.Models;

namespace InfusionsoftOAuth.Controllers
{
    public class HomeController : Controller
    {
        private string DeveloperAppKey = "";
        private string DeveloperAppSecret = "";

        private string CallbackUrl = "http://localhost:2412/home/callback";
                
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Authorize()
        {
            // Hard coded "scope=full" and "responseType=code" since they are the only supported options
            string authorizeUrlFormat = "https://signin.infusionsoft.com/app/oauth/authorize?scope=full&redirect_uri={0}&response_type=code&client_id={1}";

            // Url encode CallbackUrl
            return Redirect(string.Format(authorizeUrlFormat, Server.UrlEncode(CallbackUrl), DeveloperAppKey));
        }
        
        public ActionResult Callback(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                string tokenUrl = "https://api.infusionsoft.com/token";

                // Hard coded "grant_type=authorization_code" since it is the only supported option
                string tokenDataFormat = "code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code";

                HttpWebRequest request = HttpWebRequest.Create(tokenUrl) as HttpWebRequest;
                request.Method = "POST";
                request.KeepAlive = true;
                request.ContentType = "application/x-www-form-urlencoded";

                // Url encode CallbackUrl
                string dataString = string.Format(tokenDataFormat, code, DeveloperAppKey, DeveloperAppSecret, Server.UrlEncode(CallbackUrl));
                var dataBytes = Encoding.UTF8.GetBytes(dataString);
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(dataBytes, 0, dataBytes.Length);
                }

                string resultJSON = string.Empty;
                using (WebResponse response = request.GetResponse())
                {
                    var sr = new StreamReader(response.GetResponseStream());
                    resultJSON = sr.ReadToEnd();
                    sr.Close();
                }

                var jsonSerializer = new JavaScriptSerializer();

                var tokenData = jsonSerializer.Deserialize<TokenData>(resultJSON);

                ViewData.Add("AccessToken", tokenData.Access_Token);
            }

            return View();
        }

        public ActionResult FindContact()
        {

            var accessToken = Request.QueryString["AccessToken"];
            var email = Request.QueryString["Email"];

            var viewData = new List<Contact>();

            if (!string.IsNullOrEmpty(accessToken))
            {
                var contactService = XmlRpcProxyGen.Create<IInfusionsoftAPI>();
                contactService.Url = "https://api.infusionsoft.com/crm/xmlrpc/v1?access_token=" + accessToken;

                var contacts = contactService.LookupByEmail(DeveloperAppKey, email, new[] { "Id", "FirstName", "LastName" });

                foreach (var contact in contacts)
                {
                    viewData.Add(new Contact { Id = (int)contact["Id"], FirstName = (string)contact["FirstName"], LastName = (string)contact["LastName"]});
                }

            }

            return View(viewData);
        }


        //Infusionsoft API
        public interface IInfusionsoftAPI : IXmlRpcProxy
        {
            [XmlRpcMethod("ContactService.findByEmail")]
            XmlRpcStruct[] LookupByEmail(string key, string email, string[] selectedFields);
        }

    }
}
