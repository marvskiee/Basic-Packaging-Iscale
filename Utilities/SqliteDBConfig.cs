using Microsoft.Data.Sqlite;
using System.IO;

namespace ISCALE.Utilities
{
    public static class SqliteDBConfig
    {
        private static readonly string roaming_folder_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string app_folder_path = Path.Combine(roaming_folder_path, $"Iscale");
        private static readonly string db_path = Path.Combine(app_folder_path, "iscale.db");

        private static readonly string _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = db_path,
            Password = "Iscale2025!",
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        public static SqliteConnection GetConnection()
        {
            if(!Directory.Exists(app_folder_path))
                Directory.CreateDirectory(app_folder_path);

            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
