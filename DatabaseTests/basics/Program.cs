using System;
using System.Data.SqlClient;

namespace basics
{
    class Program
    {


        static void Main()
        {
            try
            {
                SqlConnectionStringBuilder builder = new()
                {
                    DataSource = "<your_server.database.windows.net>",
                    UserID = "<your_username>",
                    Password = "<your_password>",
                    InitialCatalog = "<your_database>"
                };

                using SqlConnection connection = new(builder.ConnectionString);
                Console.WriteLine("\nQuery data example:");
                Console.WriteLine("=========================================\n");

                connection.Open();

                String sql = "SELECT name, collation_name FROM sys.databases";

                using SqlCommand command = new SqlCommand(sql, connection);
                using SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("\nDone. Press enter.");
            Console.ReadLine();
        }
    }
}



