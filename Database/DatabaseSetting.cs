using MySqlConnector;

namespace WebScrappingTrades.Database
{
    internal static class DatabaseSetting
    {
        /// <summary>
        /// Retrieves user credentials from the database based on the specified ID and updates the provided <see
        /// cref="Startup"/> instance.
        /// </summary>
        /// <remarks>This method queries the database for user credentials associated with the specified
        /// ID and updates the <c>userCredentials</c> property of the provided <see cref="Startup"/> instance. If no
        /// credentials are found, the <c>userCredentials</c> property remains unchanged.</remarks>
        /// <param name="value">The ID of the user whose credentials are to be retrieved.</param>
        /// <param name="startup">The <see cref="Startup"/> instance whose <c>userCredentials</c> property will be updated with the retrieved
        /// data.</param>
        /// <param name="_connectionString">The connection string used to connect to the database.</param>
        /// <returns></returns>
        internal static async Task GetSettingFromDbAsync(int value, Startup startup, string _connectionString)
        {
            string query = "SELECT email, password, apiId, apiSecret, apiRefresh FROM scrapperLogin WHERE id = @id";
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", value);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (startup.userCredentials != null)
                {
                    startup.userCredentials.email = reader["email"].ToString();
                    startup.userCredentials.password = reader["password"].ToString();
                    startup.userCredentials.clientId = reader["apiId"].ToString();
                    startup.userCredentials.clientSecret = reader["apiSecret"].ToString();
                    startup.userCredentials.refreshToken = reader["apiRefresh"].ToString();
                    Console.WriteLine($"Index: {value} -> selected user: {startup.userCredentials.email}");
                }
            }
        }
    }
}