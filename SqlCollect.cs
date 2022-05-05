using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

namespace AviaturCollectDataPrices
{
    public class SqlCollect
    {
        private static SqlConnection connection = null;

        private static SqlConnection CreateSQLConn()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = "tcp:sabreconf.database.windows.net,1433";
                builder.UserID = "wreynaga";
                builder.Password = "D3v3l0p3r";
                builder.InitialCatalog = "aviatur_prod";

                return new SqlConnection(builder.ConnectionString);
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex);
            }
            return null;
        }

        internal static void Connect()
        {
            connection = CreateSQLConn();
            connection?.Open();
        }

        internal static void Disconnect()
        {
            connection?.Close();
            connection = null;
        }

        public static List<OriDesQuery> GetDataToQuery()
        {
            var result = new List<OriDesQuery>();
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("\nQuery data ...");
                    Console.WriteLine("=========================================\n");

                    String sql = @"SELECT [Origin],[Destination],[KindOfTrip] FROM [dbo].[cityPairCollectPrices] WHERE ([KindOfTrip] = 1 OR [KindOfTrip] = 2);";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new OriDesQuery(reader.GetString(0), reader.GetString(1), (KINDOFTRIP)reader.GetInt32(2)));
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            return result;
        }

        public static bool InsertDataIntoSQL(Dictionary<int, decimal> collection, OriDesQuery query)
        {
            string commandText = @"INSERT INTO [dbo].[reportFareCityPair] ([Origin],[Destination],[KindOfTrip],[Fare],[Provider])
                                   VALUES(@Origin, @Destination, @KindOfTrip, @Fare, @Provider); ";

            if (connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.Parameters.Add("@Origin", SqlDbType.NChar, 3);
                        command.Parameters.Add("@Destination", SqlDbType.NChar, 3);
                        command.Parameters.Add("@KindOfTrip", SqlDbType.Int);
                        command.Parameters.Add("@Fare", SqlDbType.Money);
                        command.Parameters.Add("@Provider", SqlDbType.Int);

                        foreach (var item in collection)
                        {
                            command.Parameters["@Origin"].Value = query.Origin;
                            command.Parameters["@Destination"].Value = query.Destination;
                            command.Parameters["@KindOfTrip"].Value = query.KindOfTrip;
                            command.Parameters["@Fare"].Value = item.Value;
                            command.Parameters["@Provider"].Value = item.Key;

                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return false;
        }
    }
}
