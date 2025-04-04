using System;
using System.Data;
using System.Data.SqlClient;
// Add this using statement for IConfiguration
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Optional: For logging within DBConnect

// Ensure this namespace matches your project structure (e.g., Project3.Utilities)
namespace Project3.Utilities
{
    public class DBConnect
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly ILogger<DBConnect> _logger; // Optional: Inject logger

        // Constructor accepts IConfiguration and ILogger via Dependency Injection
        public DBConnect(IConfiguration configuration, ILogger<DBConnect> logger) // Added ILogger
        {
            _configuration = configuration;
            _logger = logger; // Store logger

            // Retrieve the connection string from appsettings.json/appsettings.Development.json
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Check if the connection string was found
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Database connection string 'DefaultConnection' not found in configuration.");
                // Throw an exception to prevent the application from starting incorrectly
                throw new InvalidOperationException("Database connection string 'DefaultConnection' not found in configuration.");
            }
            _logger.LogInformation("DBConnect initialized with DefaultConnection."); // Confirm initialization
        }

        // Helper method to get a new connection instance
        // Ensures that each operation gets a fresh connection from the pool
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Executes a SELECT statement (typically from a Stored Procedure) and returns a DataSet.
        /// Manages connection opening and closing.
        /// </summary>
        /// <param name="theCommand">The SqlCommand object (pre-configured with CommandText, CommandType, and Parameters).</param>
        /// <returns>A DataSet containing the results, or an empty DataSet if an error occurs.</returns>
        public DataSet GetDataSetUsingCmdObj(SqlCommand theCommand)
        {
            DataSet myDataSet = new DataSet();
            // Use 'using' block to ensure the connection is properly created, opened, closed, and disposed.
            using (SqlConnection con = GetConnection())
            {
                theCommand.Connection = con; // Assign the connection to the command
                try
                {
                    SqlDataAdapter myDataAdapter = new SqlDataAdapter(theCommand);
                    // The Fill method automatically opens and closes the connection if it's not already open.
                    myDataAdapter.Fill(myDataSet);
                    _logger.LogDebug("GetDataSetUsingCmdObj executed successfully for command: {CommandText}", theCommand.CommandText);
                }
                catch (SqlException sqlEx)
                {
                    // Log the detailed SQL exception
                    _logger.LogError(sqlEx, "SQL Error in GetDataSetUsingCmdObj for command {CommandText}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                                     theCommand.CommandText, sqlEx.Number, sqlEx.Message);
                    // Return empty DataSet on error, or re-throw depending on desired handling
                    // throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General Error in GetDataSetUsingCmdObj for command {CommandText}", theCommand.CommandText);
                    // Return empty DataSet on error, or re-throw
                    // throw;
                }
            } // Connection is automatically disposed here by the 'using' block
            return myDataSet;
        }

        /// <summary>
        /// Executes an INSERT, UPDATE, or DELETE statement (typically from a Stored Procedure)
        /// and returns the number of rows affected. Manages connection opening and closing.
        /// </summary>
        /// <param name="theCommand">The SqlCommand object (pre-configured with CommandText, CommandType, and Parameters).</param>
        /// <returns>The number of rows affected, or -1 if an error occurs.</returns>
        public int DoUpdateUsingCmdObj(SqlCommand theCommand)
        {
            int rowsAffected = -1; // Default to error/no rows affected
            // Use 'using' block for the connection
            using (SqlConnection con = GetConnection())
            {
                theCommand.Connection = con; // Assign the connection to the command
                try
                {
                    con.Open(); // Explicitly open the connection for ExecuteNonQuery
                    rowsAffected = theCommand.ExecuteNonQuery();
                    _logger.LogDebug("DoUpdateUsingCmdObj executed successfully for command: {CommandText}. Rows Affected: {RowsAffected}",
                                     theCommand.CommandText, rowsAffected);
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in DoUpdateUsingCmdObj for command {CommandText}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                                     theCommand.CommandText, sqlEx.Number, sqlEx.Message);
                    rowsAffected = -1; // Ensure error code is returned
                    // throw; // Optionally re-throw
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General Error in DoUpdateUsingCmdObj for command {CommandText}", theCommand.CommandText);
                    rowsAffected = -1; // Ensure error code is returned
                    // throw; // Optionally re-throw
                }
                // No explicit Close() needed - 'using' block handles closing/disposing
            }
            return rowsAffected;
        }

        /// <summary>
        /// Executes a command (typically a Stored Procedure for INSERT) that is expected
        /// to return a single scalar value (e.g., the new identity/ID).
        /// Manages connection opening and closing.
        /// </summary>
        /// <param name="theCommand">The SqlCommand object (pre-configured with CommandText, CommandType, and Parameters).</param>
        /// <returns>The scalar value returned by the command, or null if an error occurs or no value is returned.</returns>
        public async Task<object> ExecuteScalarUsingCmdObjAsync(SqlCommand theCommand)
        {
            object result = null;
            using (SqlConnection con = GetConnection())
            {
                theCommand.Connection = con;
                try
                {
                    await con.OpenAsync(); // Open asynchronously
                    result = await theCommand.ExecuteScalarAsync(); // Execute asynchronously
                    _logger.LogDebug("ExecuteScalarUsingCmdObjAsync executed successfully for command: {CommandText}. Result: {Result}",
                                     theCommand.CommandText, result ?? "NULL");
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in ExecuteScalarUsingCmdObjAsync for command {CommandText}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                                     theCommand.CommandText, sqlEx.Number, sqlEx.Message);
                    result = null; // Indicate error
                    // throw; // Optionally re-throw
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General Error in ExecuteScalarUsingCmdObjAsync for command {CommandText}", theCommand.CommandText);
                    result = null; // Indicate error
                    // throw; // Optionally re-throw
                }
            }
            return result;
        }

        // NOTE: Methods like GetRow, GetField, GetRows that relied on the class-level 'ds'
        // should be removed or refactored, as storing state like that is problematic.
        // Data should be processed directly from the DataSet returned by GetDataSetUsingCmdObj.

    } // end class
} // end namespace