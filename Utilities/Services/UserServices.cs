using ISCALE.Core.Interface;
using ISCALE.MVVM.Model;
using Microsoft.Data.Sqlite;
using Formatter.Helper;
namespace ISCALE.Utilities.Services
{
    public class UserServices : IService<User>
    {
        private readonly SqliteConnection _connection;
        private FileHelper fileHelper;
        public UserServices(SqliteConnection connection)
        {
            _connection = connection;
            fileHelper = new FileHelper();
            CreateTable();
        }
        public async Task CreateTable()
        {
            try
            {

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS tbl_users (
                    Id TEXT PRIMARY KEY, 
                    Username TEXT NOT NULL, 
                    Password TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    IsDeleted INTEGER NOT NULL DEFAULT 0,
                    DeletedAt DATETIME NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP);";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS active_username ON tbl_users (Username) WHERE IsDeleted = 0";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }

        }

        public async Task<ServiceResult> GetAllItem()
        {
            return await UserActions("LOAD");
        }
        public async Task<ServiceResult> AddItem(User newUser)
        {
            return await UserActions("ADD", newUser);
        }

        public async Task<ServiceResult> UpdateItem(User newUser)
        {
            return await UserActions("UPDATE", newUser);
        }
        public async Task<ServiceResult> UpdatePassword(User newUser, string oldPassword)
        {
            return await UserActions("UDPATE_PASSWORD", oldPassword, newUser);
        }
        public async Task<ServiceResult> DeleteItem(User oldUser)
        {
            return await UserActions("DELETE", oldUser);
        }
        private async Task<ServiceResult> UserActions(string action, string oldPassword, User newUser = null)
        {
            try
            {
                switch (action)
                {
                    case "UDPATE_PASSWORD":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"SELECT * FROM tbl_users WHERE Id = $id";
                            command.Parameters.AddWithValue("$id", newUser.Id);

                            await command.ExecuteNonQueryAsync();

                            User user = new User();
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    user.Password = reader["Password"].ToString();
                                }
                            }
                            ;
                            bool isPasswordMatch = CryptographyHelper.Decrypt(user.Password, Constants.PASSWORD_HASH) == oldPassword;

                            if (!isPasswordMatch)
                                return new ServiceResult
                                {
                                    Success = false,
                                    ErrorMessage = "Wrong Password!",
                                    ErrorType = Constants.WRONG_PASSWORD
                                };

                            command.Parameters.Clear();
                            command.CommandText = @"
                                    UPDATE tbl_users SET 
                                    Password = $password
                                    WHERE Id = $id";
                            command.Parameters.AddWithValue("$id", newUser.Id);
                            command.Parameters.AddWithValue("$password", newUser.Password);

                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorType = Constants.CONNECTION_ERROR_TYPE
                };
            }
            return new ServiceResult
            {
                Success = true
            };
        }
        private async Task<ServiceResult> UserActions(string action, User newUser = null)
        {
            try
            {
                switch (action)
                {
                    case "UPDATE":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    UPDATE tbl_users SET 
                                    Username = $username, Password = $password, Role = $role
                                    WHERE Id = $id";
                            command.Parameters.AddWithValue("$id", newUser.Id);
                            command.Parameters.AddWithValue("$username", newUser.Username);
                            command.Parameters.AddWithValue("$role", newUser.Role);
                            command.Parameters.AddWithValue("$password", newUser.Password);


                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "ADD":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    INSERT INTO tbl_users 
                                    (Id,Username,Password,Role)
                                    VALUES ($id,$username, $password, $role);";

                            command.Parameters.AddWithValue("$id", newUser.Id);
                            command.Parameters.AddWithValue("$username", newUser.Username);
                            command.Parameters.AddWithValue("$password", newUser.Password);
                            command.Parameters.AddWithValue("$role", newUser.Role);

                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "DELETE":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    UPDATE tbl_users SET 
                                    IsDeleted = $isDeleted,
                                    DeletedAt = $deletedAt
                                    WHERE Id = $id";
                            command.Parameters.AddWithValue("$isDeleted", 1);
                            command.Parameters.AddWithValue("$deletedAt", DateTime.UtcNow);
                            command.Parameters.AddWithValue("$id", newUser.Id);

                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "LOAD":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    SELECT * FROM tbl_users
                                    WHERE IsDeleted = 0 AND Role != 'Admin'";

                            await command.ExecuteNonQueryAsync();

                            using (var reader = command.ExecuteReader())
                            {
                                List<User> items = new List<User>();
                                while (reader.Read())
                                {
                                    items.Add(new User
                                    {
                                        Id = reader["Id"].ToString(),
                                        Username = reader["Username"].ToString(),
                                        Password = reader["Password"].ToString(),
                                        Role = reader["Role"].ToString()
                                    });
                                }
                                return new ServiceResult
                                {
                                    Success = true,
                                    Data = items
                                };
                            }
                        }
                    default:
                        throw new Exception("INVALID ACTION");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ErrorType = Constants.DUPLICATE_ERROR_TYPE
                    };
                }
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorType = Constants.CONNECTION_ERROR_TYPE
                };
            }

            return new ServiceResult
            {
                Success = true
            };
        }
    }
}