using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DumpVW
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
                string outputFolder = $@"C:\{databaseName}\ViewsExport"; // Output folder path

                // Ensure output folder exists
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // SQL connection string (Windows Authentication)
                string connectionString = $"Server={serverName};Database={databaseName};Integrated Security=True;";
                var sqlViewInfos = new List<SqlViewInfo>();
                var sqlViewReader = new SqlViewReader();
                sqlViewInfos = sqlViewReader.GetAllViewDefinitions(connectionString);
                foreach (var sqlViewInfo in sqlViewInfos)
                {
                    string fileName = $"{sqlViewInfo.SchemaName}.{sqlViewInfo.ViewName}.sql";
                    string filePath = Path.Combine(outputFolder, fileName);
                    string header = @"SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
";
                    string fullScript = $"{header}\r\n{sqlViewInfo.ViewDefinition}\r\nGO";
                    File.WriteAllText(filePath, fullScript);
                }

                Console.WriteLine($"\nAll views scripted to: {outputFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    public class SqlViewInfo
    {
        public string SchemaName { get; set; }
        public string ViewName { get; set; }
        public string ViewDefinition { get; set; }
    }

    public class SqlViewReader
    {
        public List<SqlViewInfo> GetAllViewDefinitions(string connectionString)
        {
            var views = new List<SqlViewInfo>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
SELECT
    s.name AS SchemaName,
    v.name AS ViewName,
    m.definition AS ViewDefinition
FROM sys.views v
INNER JOIN sys.sql_modules m ON v.object_id = m.object_id
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
ORDER BY s.name, v.name;
";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var view = new SqlViewInfo
                            {
                                SchemaName = reader["SchemaName"].ToString(),
                                ViewName = reader["ViewName"].ToString(),
                                ViewDefinition = reader["ViewDefinition"].ToString()
                            };
                            views.Add(view);
                        }
                    }
                }
            }

            return views;
        }
    }
}