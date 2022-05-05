using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace AviaturCollectDataPrices
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlCollect.Connect();
            Console.WriteLine("Conectando a base de datos ...");
            List<OriDesQuery> QUERY_CITYPAIRS = SqlCollect.GetDataToQuery();

            Console.WriteLine("\nCityPairs encontradas: {0}" , QUERY_CITYPAIRS.Count);
            Console.WriteLine("Departure Day: {0}", MpbCore.departureDate.ToString("yyyy-MM-dd"));
            Console.WriteLine("Returning Day: {0}", MpbCore.returningDate.ToString("yyyy-MM-dd"));

            string mpbToken = MpbCore.GetToken(
                ConfigurationManager.AppSettings.Get("Corporation"),
                ConfigurationManager.AppSettings.Get("Password"),
                ConfigurationManager.AppSettings.Get("Office")).Result;

            Console.WriteLine("\nMBP Token: {0}", mpbToken);

            Console.WriteLine("\nIniciando consultas ...");

            foreach (OriDesQuery item in QUERY_CITYPAIRS)
            {
                Dictionary<int, decimal> response = MpbCore.GetMpbResponse(mpbToken, item).Result;
                SqlCollect.InsertDataIntoSQL(response, item);

                Console.WriteLine("\nFrom: {0} To: {1} KindOfTrip {2} \t Providers: {3}", item.Origin, item.Destination, item.KindOfTrip, response.Count);
                foreach (var kvp in response.OrderBy( x => x.Value))
                {
                    Console.WriteLine("\tProviderId: {0} \t Fare: {1:C0}", kvp.Key, kvp.Value);
                }
            }

            Console.WriteLine("Fin.");
            SqlCollect.Disconnect();

            Console.ReadLine();
        }
    }
}
