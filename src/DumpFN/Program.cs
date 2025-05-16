using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DumpFN
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // === CONFIGURATION ===
                string serverName = @"MANH"; // Your SQL Server instance
                string databaseName = "mssql"; // Your database name
                string outputFolder = $@"C:\{databaseName}\FunctionsExport"; // Output folder path

                // Ensure output folder exists
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // SQL connection string (Windows Authentication)
                string connectionString = $"Server={serverName};Database={databaseName};Integrated Security=True;";
                var sqlFunctionInfos = new List<SqlFunctionInfo>();
                var sqlFunctionReader = new SqlFunctionReader();
                sqlFunctionInfos = sqlFunctionReader.GetAllFunctionBodies(connectionString);
                foreach (var sqlFunctionInfo in sqlFunctionInfos)
                {
                    string fileName = $"{sqlFunctionInfo.SchemaName}.{sqlFunctionInfo.FunctionName}.sql";
                    string filePath = Path.Combine(outputFolder, fileName);
                    string header = @"SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
";
                    string fullScript = $"{header}\r\n{sqlFunctionInfo.FunctionDefinition}\r\nGO";
                    File.WriteAllText(filePath, fullScript);
                }

                Console.WriteLine($"\nAll functions scripted to: {outputFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    public class SqlFunctionInfo
    {
        public string SchemaName { get; set; }
        public string FunctionName { get; set; }
        public string FunctionDefinition { get; set; }
    }

    public class SqlFunctionReader
    {
        public List<SqlFunctionInfo> GetAllFunctionBodies(string connectionString)
        {
            var functions = new List<SqlFunctionInfo>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
SELECT
    s.name AS SchemaName,
    o.name AS FunctionName,
    m.definition AS FunctionDefinition
FROM sys.sql_modules m
INNER JOIN sys.objects o ON m.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type IN ('FN', 'IF', 'TF')  -- Scalar, Inline, Table-Valued Functions
ORDER BY s.name, o.name;
";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var info = new SqlFunctionInfo
                            {
                                SchemaName = reader["SchemaName"].ToString(),
                                FunctionName = reader["FunctionName"].ToString(),
                                FunctionDefinition = reader["FunctionDefinition"].ToString()
                            };
                            functions.Add(info);
                        }
                    }
                }
            }

            return functions;
        }
    }
}