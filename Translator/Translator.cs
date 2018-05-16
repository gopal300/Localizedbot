using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using LuisBot.Models;
using LuisBot.Utilities;

namespace LuisBot.Translator
{
    public class Translator
    {
        internal string Bearer;

        internal Translator()
        {
            Bearer = Task.Run(GetBearerTokenForTranslator).Result;
        }

        public string Translate(string inputText, string inputLocale, string outputLocale)
        {
            try
            {
                string uri = "https://api.microsofttranslator.com/v2/Http.svc/"+
                    $"Translate?text={HttpUtility.UrlEncode(inputText)}&from={inputLocale}&to={outputLocale}";

                WebRequest translationWebRequest = WebRequest.Create(uri);
                translationWebRequest.Headers.Add("Authorization", Bearer);

                WebResponse response = null;
                response = translationWebRequest.GetResponse();
                Stream stream = response.GetResponseStream();
                Encoding encode = Encoding.GetEncoding("utf-8");

                StreamReader translatedStream = new StreamReader(stream, encode);
                XmlDocument xTranslation = new XmlDocument();
                xTranslation.LoadXml(translatedStream.ReadToEnd());

                return xTranslation.InnerText;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal string Detect(string input)
        {
            try
            {
                string uri = "https://api.microsofttranslator.com/v2/Http.svc/Detect?text=" + HttpUtility.UrlEncode(input);
                WebRequest translationWebRequest = WebRequest.Create(uri);
                translationWebRequest.Headers.Add("Authorization", Bearer);

                WebResponse response = null;
                response = translationWebRequest.GetResponse();
                Stream stream = response.GetResponseStream();
                Encoding encode = Encoding.GetEncoding("utf-8");

                StreamReader translatedStream = new StreamReader(stream, encode);
                XmlDocument xTranslation = new XmlDocument();
                xTranslation.LoadXml(translatedStream.ReadToEnd());

                return xTranslation.InnerText;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        internal async Task<string> GetBearerTokenForTranslator()
        {
            var azureSubscriptionKey = Settings.GetSubscriptionKey();
            var azureAuthToken = new AzureAuthToken(azureSubscriptionKey);
            var token = await azureAuthToken.GetAccessTokenAsync();
            GlobalVars.Bearer = token;
            return GlobalVars.Bearer;



        }
    }
}