using System;
using System.Data.SqlClient;
using System.IO;

namespace GenerateSPBody
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // === CONFIGURATION ===
            string serverName = @"MANH"; // Your SQL Server instance
            string databaseName = "mssql"; // Your database name
            string outputFolder = @"C:\StoredProceduresExport"; // Output folder path

            // Ensure output folder exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // SQL connection string (Windows Authentication)
            string connectionString = $"Server={serverName};Database={databaseName};Integrated Security=True;";

            // SQL query to get stored procedure definitions
            string sqlQuery = @"
SELECT
    SCHEMA_NAME(p.schema_id) AS SchemaName,
    p.name AS ProcedureName,
    m.definition AS ProcedureDefinition
FROM sys.procedures p
JOIN sys.sql_modules m ON p.object_id = m.object_id
ORDER BY SchemaName, ProcedureName;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine($"Connected to server '{serverName}', database '{databaseName}'");

                    using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string schemaName = reader.GetString(0);
                            string procName = reader.GetString(1);
                            string definition = reader.GetString(2);

                            string fileName = $"{schemaName}.{procName}.sql";
                            string filePath = Path.Combine(outputFolder, fileName);

                            string header = @"SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
";

                            string fullScript = $"{header}\r\n{definition}\r\nGO";

                            File.WriteAllText(filePath, fullScript);

                            Console.WriteLine($"Scripted: {schemaName}.{procName} -> {fileName}");
                        }
                    }

                    Console.WriteLine($"\nAll stored procedures scripted to: {outputFolder}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
