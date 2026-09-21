using Formatter.Helper;
using ISCALE.MVVM.Model;
using Microsoft.Data.Sqlite;

namespace ISCALE.Utilities.Services
{
    public class AuthServices
    {
        private readonly SqliteConnection _connection;

        public AuthServices(SqliteConnection connection)
        {
            _connection = connection;
            CreateTable();
            CheckAdmin();
        }
        private async Task CreateTable()
        {
            UserServices userServices = new UserServices(_connection);
            await userServices.CreateTable();
        }
        private async Task<ServiceResult> CheckAdmin()
        {
            return await AuthActions("CHECK_ADMIN", Constants.ADMIN_CREDENTIALS);
        }

        public async Task<ServiceResult> Login(string username, string password)
        {
            return await AuthActions("LOGIN",new User { Username = username, Password = password});
        }

        private async Task<ServiceResult> AuthActions(string action, User user)
        {
            try
            {
                switch (action)
                {
                    case "CHECK_ADMIN":
                        using(var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"SELECT COUNT(*) FROM tbl_users WHERE Role = 'Admin' LIMIT 1";
                            int admin_ct = int.TryParse(command.ExecuteScalar()?.ToString(), out int ct) ? ct : 0;
                            if (admin_ct == 0)
                            {
                                UserServices userServices = new UserServices(_connection);
                                ServiceResult result = await userServices.AddItem(user);
                                if (!result.Success)
                                    throw new Exception(result.ErrorMessage);
                            }
                        }
                        break;
                    case "LOGIN":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"SELECT * FROM tbl_users WHERE Username = @username AND IsDeleted = 0";
                            command.Parameters.AddWithValue("@username", user.Username);

                            await command.ExecuteNonQueryAsync();
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {

                                    if (CryptographyHelper.Decrypt(reader["Password"].ToString(), Constants.PASSWORD_HASH) == user.Password)
                                        return new ServiceResult
                                        {
                                            Success = true,
                                            Data = new User
                                            {
                                                Id = reader["Id"].ToString(),
                                                Username = reader["Username"].ToString(),
                                                Role = reader["Role"].ToString()
                                            }
                                        };
                                }
                            }
                            return new ServiceResult
                            {
                                Success = false,
                                ErrorType = "USER_NOT_EXISTED"
                            };
                        }
                    default:
                        throw new Exception("INVALID ACTION");
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
    }
}