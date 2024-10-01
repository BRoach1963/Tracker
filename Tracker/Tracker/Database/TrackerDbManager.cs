using System.Data;
using System.Security.Principal;
using System.Windows;
using ABI.Windows.System.RemoteSystems;
using Microsoft.Data.SqlClient;
using Tracker.Common;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Views.Dialogs;

namespace Tracker.Database
{
    public class TrackerDbManager
    {
        #region Fields

        private static TrackerDbManager? _instance;
        private static readonly object SyncRoot = new();
        protected volatile bool _isInitialized;

        private LoggingManager.Logger _logger = new(nameof(TrackerDbManager), "DatabaseLog");

        #endregion

        #region Public Instances

        public static TrackerDbManager? Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new TrackerDbManager();
                    }
                }

                return _instance;
            }
        }

        public void Reset()
        {
            _isInitialized = false;

        }

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
        }

        public void Shutdown()
        {
            _isInitialized = false;
        }

        #endregion

        #region Public Properties

        public bool IsCloud { get; set; }

        public bool IsLocal { get; set; }

        public bool IsServer { get; set; }

        public string LocalServerName { get; set; } = "Primary_Alien";

        public string LocalDatabaseName { get; set; } = "TrackerDb";

        public string LocalUserName { get; set; }

        public string LocalPassword { get; set; }

        public string CloudServerName { get; set; }

        public string CloudDatabaseName { get; set; }

        public string CloudUserName { get; set; }

        public string CloudPassword { get; set; }

        #endregion

        #region Public Methods

        public async Task<List<TeamMember>> GetTeamMembers()
        {
            var teamMembers = new List<TeamMember>();

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand("GetTeamMembers", connection);
                command.CommandType = CommandType.StoredProcedure;

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var teamMember = new TeamMember
                    {
                        Id = reader.GetInt32(reader.GetOrdinal(TrackerConstants.IdField)),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        NickName = reader.IsDBNull(reader.GetOrdinal("NickName")) ? string.Empty : reader.GetString(reader.GetOrdinal("NickName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        CellPhone = reader.GetString(reader.GetOrdinal("Cell")),
                        JobTitle = reader.GetString(reader.GetOrdinal("JobTitle")),
                        BirthDay = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Birthday")),
                        HireDate = reader.IsDBNull(reader.GetOrdinal("HireDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("HireDate")),
                        TerminationDate = reader.IsDBNull(reader.GetOrdinal("TerminationDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("TerminationDate")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? false : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                        ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage")) ? Array.Empty<byte>() : (byte[])reader["ProfileImage"],
                        LinkedInProfile = reader.IsDBNull(reader.GetOrdinal("LinkedInProfile")) ? string.Empty : reader.GetString(reader.GetOrdinal("LinkedInProfile")),
                        FacebookProfile = reader.IsDBNull(reader.GetOrdinal("FacebookProfile")) ? string.Empty : reader.GetString(reader.GetOrdinal("FacebookProfile")),
                        InstagramProfile = reader.IsDBNull(reader.GetOrdinal("InstaProfile")) ? string.Empty : reader.GetString(reader.GetOrdinal("InstaProfile")),
                        XProfile = reader.IsDBNull(reader.GetOrdinal("XProfile")) ? string.Empty : reader.GetString(reader.GetOrdinal("XProfile")),
                        Specialty = reader.IsDBNull(reader.GetOrdinal("Speciality")) ? (EngineeringSpecialtyEnum)0 : (EngineeringSpecialtyEnum)reader.GetInt32(reader.GetOrdinal("Speciality")),
                        SkillLevel = reader.IsDBNull(reader.GetOrdinal("Skill")) ? (SkillLevelEnum)0 : (SkillLevelEnum)reader.GetInt32(reader.GetOrdinal("Skill")),
                        Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? (RoleEnum)0 : (RoleEnum)reader.GetInt32(reader.GetOrdinal("Role"))
                    };

                    teamMembers.Add(teamMember);
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                _logger.Exception(ex, "Error retrieving Team Members collection from database.");
            }
            _logger.Debug("{0}: Team members successfully returned from database.", nameof(GetTeamMembers));
            var sortedTeamMembers = teamMembers
                .OrderBy(tm => tm.Role)
                .ThenBy(tm => tm.LastName)
                .ThenBy(tm => tm.FirstName)
                .ToList();

            return sortedTeamMembers;
        }

        public async Task<bool> AddTeamMember(string? firstName = null,
            string? lastName = null,
            string? nickName = null,
            string? email = null,
            string? cell = null,
            string? jobTitle = null,
            DateTime? birthday = null,
            DateTime? hireDate = null,
            DateTime? terminationDate = null,
            bool? isActive = null,
            int? managerId = null,
            byte[]? profileImage = null,
            string? linkedInProfile = null,
            string? facebookProfile = null,
            string? instagramProfile = null,
            string? xProfile = null,
            int? specialty = null,
            int? skill = null,
            int? role = null)
        {
            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(TrackerConstants.AddTeamMember, connection);
                command.CommandType = CommandType.StoredProcedure;

                AddSqlParameter(command, TrackerConstants.TeamMemberFirstName, firstName);
                AddSqlParameter(command, TrackerConstants.TeamMemberLastName, lastName);
                AddSqlParameter(command, TrackerConstants.TeamMemberNickname, nickName);
                AddSqlParameter(command, TrackerConstants.TeamMemberEmail, email);
                AddSqlParameter(command, TrackerConstants.TeamMemberCell, cell);
                AddSqlParameter(command, TrackerConstants.TeamMemberJobTitle, jobTitle);
                AddSqlParameter(command, TrackerConstants.TeamMemberBirthday, birthday);
                AddSqlParameter(command, TrackerConstants.TeamMemberHireDate, hireDate);
                AddSqlParameter(command, TrackerConstants.TeamMemberTerminationDate, terminationDate);
                AddSqlParameter(command, TrackerConstants.TeamMemberIsActive, isActive);
                AddSqlParameter(command, TrackerConstants.TeamMemberManagerId, managerId);
                AddSqlParameter(command, TrackerConstants.TeamMemberProfileImage, profileImage);
                AddSqlParameter(command, TrackerConstants.TeamMemberLinkedInProfile, linkedInProfile);
                AddSqlParameter(command, TrackerConstants.TeamMemberFacebookProfile, facebookProfile);
                AddSqlParameter(command, TrackerConstants.TeamMemberInstaProfile, instagramProfile);
                AddSqlParameter(command, TrackerConstants.TeamMemberXProfile, xProfile);
                AddSqlParameter(command, TrackerConstants.TeamMemberSpeciality, specialty);
                AddSqlParameter(command, TrackerConstants.TeamMemberSkill, skill);
                AddSqlParameter(command, TrackerConstants.TeamMemberRole, role);

                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    if (reader.FieldCount == 1 && reader.GetName(0) == "NewTeamMemberId")
                    {
                        return true;
                    }

                    if (reader.FieldCount == 2 && reader.GetName(0) == "ErrorNumber" && reader.GetName(1) == "ErrorMessage")
                    {
                        // Handle SQL errors returned from the CATCH block
                        int errorNumber = Convert.ToInt32(reader["ErrorNumber"]);
                        string errorMessage = reader["ErrorMessage"].ToString();
                        _logger.Error("SQL Error : {0}: {1}", errorNumber, errorMessage);
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task UpdateTeamMemberValues(int id, Dictionary<string, object> values)
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(TrackerConstants.UpdateTeamMember, connection);
            command.CommandType = CommandType.StoredProcedure;

            // Required field
            command.Parameters.AddWithValue(TrackerConstants.TeamMemberId, id);


            // Optional fields
            foreach (var value in values)
            {
                AddSqlParameter(command, value.Key, value.Value);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeleteTeamMember(int id)
        {
            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(TrackerConstants.DeleteTeamMember, connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue(TrackerConstants.TeamMemberId, id);

                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception e)
            {
                _logger.Exception(e, "{0}: Error deleting contact with id {1}", nameof(DeleteTeamMember), id);
                return false;
            }
        }

        public async Task CheckUserAsync()
        {
            try
            {
                // Try opening a connection with the current user's credentials
                await using var connection = new SqlConnection(BuildConnectionString());
                await connection.OpenAsync();
                LocalUserName = WindowsIdentity.GetCurrent().Name;
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    NotificationManager.Instance.SendNativeToast(ToastNotificationAction.SqlLoginSuccess);
                });
            }
            catch (SqlException ex) when (ex.Number == 18456) // Login failed
            {
                _logger.Exception(ex, "Error attempting to access SQL Database: Login Failed.  Attempting to add User");
                await AddUserToDatabaseAsync();
            }
            catch (Exception e)
            {
                _logger.Exception(e, "Error attempting to access SQL Database");
            }
        }

        #endregion

        #region Private Properties


        #endregion

        #region Private Methods

        private string BuildConnectionString()
        {
            if (IsCloud)
            {
                // Azure SQL Server connection string
                return $"Server=tcp:{CloudServerName}.database.windows.net,1433;" +
                                   $"Initial Catalog={CloudDatabaseName};" +
                                   $"Persist Security Info=False;" +
                                   $"User ID={CloudUserName};" +
                                   $"Password={CloudPassword};" +
                                   "MultipleActiveResultSets=False;" +
                                   "Encrypt=True;" +
                                   "TrustServerCertificate=False;" +
                                   "Connection Timeout=30;";
            }
            else if (IsLocal)
            {
                return string.Empty;
            }
            else
            {
                // SQL Server connection string
                return $"Server={LocalServerName};" +
                                   $"Database={LocalDatabaseName};" +
                                   "Integrated Security=True;" +
                                   "MultipleActiveResultSets=True;" +
                                   "TrustServerCertificate=True;" +
                                   "Connection Timeout=30;";
            }
        }

        private async Task<SqlConnection> OpenConnectionAsync()
        {
            if (string.IsNullOrEmpty(BuildConnectionString()))
            {
                BuildConnectionString();
            }

            var connection = new SqlConnection(BuildConnectionString());
            await connection.OpenAsync();
            return connection;
        }

        private void CloseConnection(SqlConnection? connection)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        private async Task AddUserToDatabaseAsync()
        {
            // Get the current Windows user name
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            string userName = currentUser.Name; // e.g., "MACHINE_NAME\UserName"

            // Create a connection to the SQL Server using an admin account
            var adminConnectionString = @"Data Source=YourSqlServerInstance;Initial Catalog=master;User Id=adminUsername;Password=adminPassword;";

            await using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            // SQL command to add the user as a login and then add them to the database
            string addUserCommandText = $@"
                IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = '{userName}')
                BEGIN
                    CREATE LOGIN [{userName}] FROM WINDOWS;
                END
                IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '{userName}')
                BEGIN
                    USE TrackerDatabase;
                    CREATE USER [{userName}] FOR LOGIN [{userName}];
                    ALTER ROLE db_datareader ADD MEMBER [{userName}];
                    ALTER ROLE db_datawriter ADD MEMBER [{userName}];
                END";

            await using (var command = new SqlCommand(addUserCommandText, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            _logger.Debug("{0}: added {1} to database", nameof(AddUserToDatabaseAsync), userName);
            // After adding the user, show a toast notification to inform them
            NotificationManager.Instance.SendTrackerToast("Did this work?", "I am me.");
        }

        private void AddSqlParameter<T>(SqlCommand command, string paramName, T value)
        {
            if (value == null || value.Equals(default(T)))
            {
                command.Parameters.AddWithValue(paramName, DBNull.Value);
            }
            else
            {
                command.Parameters.AddWithValue(paramName, value);
            }
        }

        #endregion
    }
}
