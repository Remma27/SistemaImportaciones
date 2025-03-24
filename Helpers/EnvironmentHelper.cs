using System;

namespace Sistema_de_Gestion_de_Importaciones.Helpers
{
    public static class EnvironmentHelper
    {
        public static string GetDatabaseConnectionString()
        {
            return $"Server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                   $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                   $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                   $"User={Environment.GetEnvironmentVariable("DB_USER")};" +
                   $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                   "Convert Zero Datetime=True;ConnectionTimeout=30;";
        }

        public static string GetJwtKey()
        {
            return Environment.GetEnvironmentVariable("JWT_KEY") ??
                   throw new InvalidOperationException("JWT Key not found in environment variables.");
        }

        public static string GetJwtIssuer()
        {
            return Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "SistemaGestionImportaciones";
        }

        public static string GetJwtAudience()
        {
            return Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ApiClients";
        }

        public static int GetJwtExpiryMinutes()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out int result))
            {
                return result;
            }
            return 60; 
        }

        public static string GetApiBaseUrl()
        {
            return Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5079";
        }

        public static string[] GetApiKeys()
        {
            return new[]
            {
                Environment.GetEnvironmentVariable("API_KEY_1") ?? "default-key-1",
                Environment.GetEnvironmentVariable("API_KEY_2") ?? "default-key-2"
            };
        }

        public static string GetDataProtectionPath()
        {
            return Environment.GetEnvironmentVariable("DATA_PROTECTION_PATH") ?? @"C:\DataProtection-Keys";
        }
    }
}
