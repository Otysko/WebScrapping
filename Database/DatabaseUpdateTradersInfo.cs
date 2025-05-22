using MySqlConnector;

namespace WebScrappingTrades.Database
{
    internal static class DatabaseUpdateTradersInfo
    {
        /// <summary>
        /// Updates the trader statistics in the database for the specified client.
        /// </summary>
        /// <remarks>This method updates the trader statistics in the `traderStatistics` table of the
        /// `binbot` database. The `friendlyName` column is used to locate the record to update. Ensure that the
        /// provided <paramref name="values"/> array contains valid data and that the database connection string is
        /// properly configured.</remarks>
        /// <param name="values">An array of strings containing the updated values for the trader statistics. The array must contain exactly
        /// 8 elements in the following order: daily ROI, weekly ROI, monthly ROI, total ROI, daily PnL, weekly PnL,
        /// monthly PnL, and total PnL.</param>
        /// <param name="clientName">The name of the client whose statistics are being updated. This value is used to identify the corresponding
        /// record in the database.</param>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>
        internal static void UpdateValues(string[] values, string clientName, string connectionString)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            string sqlcomm = $"UPDATE `binbot`.`traderStatistics` SET `dailyRoi` = '{values[0]}', `weeklyRoi` = '{values[1]}', `monthlyRoi` = '{values[2]}', `totalRoi` = '{values[3]}', `dailyPnl` = '{values[4]}', `weeklyPnl` = '{values[5]}', `monthlyPnl` = '{values[6]}', `totalPnl` = '{values[7]}' WHERE (`friendlyName` = '{clientName}');";
            MySqlCommand cmd = new(sqlcomm, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}