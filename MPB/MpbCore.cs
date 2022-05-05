using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AviaturCollectDataPrices
{
    public static class MpbCore
    {
        const string endPointMpb = @"http://mpb03.grupoaviatur.com:8080/Aviatur.MPB.Adapter/SessionService.ashx";
        const string endPointMpbQuery = @"http://mpb03.grupoaviatur.com:8080/Aviatur.MPB.Adapter/AirService.ashx";

        public readonly static DateTime departureDate = DateTime.Now.AddDays(2);
        public readonly static DateTime returningDate = DateTime.Now.AddDays(33);

        public static async Task<string> GetToken(string Corporation, string Password, string Office)
        {
            string soapString = File.ReadAllText(@".\MPB\RQ_MPB_TOKEN.xml").Replace("$Corporation$", Corporation).Replace("$Password$", Password).Replace("$Office$", Office);

            HttpResponseMessage response = await PostXmlRequest(endPointMpb, soapString);
            string xmlString = await response.Content.ReadAsStringAsync();

            var xDoc = XDocument.Parse(xmlString);

            return xDoc.Descendants().Where(m => m.Name.LocalName == "TransactionIdentifier").FirstOrDefault().Value;
        }

        public static async Task<Dictionary<int, decimal>> GetMpbResponse(string TransactionIdentifier, OriDesQuery queryTrip)
        {
            string soapString = (queryTrip.KindOfTrip == KINDOFTRIP.OneWay) ? 
                File.ReadAllText(@".\MPB\RQ_MPB_ONEWAY.xml").Replace("$TransactionIdentifier$", TransactionIdentifier)
                : File.ReadAllText(@".\MPB\RQ_MPB_ROUNDTRIP.xml").Replace("$TransactionIdentifier$", TransactionIdentifier);

            soapString = soapString.Replace("$Origin$", queryTrip.Origin).Replace("$Destination$", queryTrip.Destination)
                .Replace("$DateDeparture$", departureDate.ToString("yyyy-MM-dd"))
                .Replace("$DateReturn$", returningDate.ToString("yyyy-MM-dd"));
            
            HttpResponseMessage response = await PostXmlRequest(endPointMpbQuery, soapString);
            string xmlString = await response.Content.ReadAsStringAsync();

            var xDoc = XDocument.Parse(xmlString);
            var nodes = xDoc.Descendants().Where(m => m.Name.LocalName == "PricedItinerary");
#if DEBUG
            Directory.CreateDirectory(@".\xml");
            _ = File.WriteAllTextAsync(@".\xml\" + queryTrip.ToString() + "_" + Environment.TickCount.ToString() + ".xml", xmlString, Encoding.UTF8).ConfigureAwait(true);
#endif
            /*
            foreach (var item in nodes)
            {
                Console.WriteLine("TotalFare {0,-10:N0} \t Nota: {1}",
                    item.Descendants().Where(m => m.Name.LocalName == "TotalFare").FirstOrDefault().Attribute("Amount"),
                    item.Descendants().Where(m => m.Name.LocalName == "Notes").FirstOrDefault().Value);
            }
            */
            return GetMinimaFare(nodes);
        }

        private static Dictionary<int, decimal> GetMinimaFare(IEnumerable<XElement> nodes)
        {
            Dictionary<int, decimal> resultado = null;
            try
            {
                var proveedores = nodes.Select(x => new {
                    proveedor = x.Descendants().Where(m => m.Name.LocalName == "Notes").FirstOrDefault().Value,
                    fare = x.Descendants().Where(m => m.Name.LocalName == "TotalFare").FirstOrDefault().Attribute("Amount").Value
                });

                resultado = proveedores.Select(x => new
                {
                    proveedor = ObtenerProvider(x.proveedor),
                    fare = decimal.Parse(x.fare)
                })
                .GroupBy(o => o.proveedor)
                .ToDictionary(g => g.Key, g => g.Min(o => o.fare));
            }
            catch { }

            return resultado;
        }

        /// <summary>
        ///     <Notes>CorrelationID=BpflZvw3GEaybV-tj88KvQ;SequenceNmbr=0;ProviderId=45;</Notes>
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static int ObtenerProvider(string notes)
        {
            try
            {
                var split = notes.Split(';');
                var nodo = split.FirstOrDefault(x => x.StartsWith("ProviderId="));

                if (!string.IsNullOrWhiteSpace(nodo))
                {
                    if (int.TryParse(nodo.Replace("ProviderId=", ""), out int resultado))
                    {
                        return resultado;
                    }
                }
            }
            catch {}

            return 0;
        }

        private static async Task<HttpResponseMessage> PostXmlRequest(string baseUrl, string xmlString)
        {
            using (var httpClient = new HttpClient())
            {
                var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");
                //httpContent.Headers.Add("SOAPAction", "http://tempuri.org/HelloWorld");

                return await httpClient.PostAsync(baseUrl, httpContent);
            }
        }
    }
}
