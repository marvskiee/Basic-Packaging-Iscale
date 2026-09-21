using ISCALE.MVVM.Model;
using ISCALE.MVVM.Model.ServiceModel;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Windows;

namespace ISCALE.Utilities.Services
{
    public class ScheduleServices : ViewModelBase
    {
        private readonly SqliteConnection _connection;

        public ScheduleServices(SqliteConnection connection)
        {
            _connection = connection;
            CreateTable();
        }

        private void CreateTable()
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS tbl_schedule (
                            Id TEXT PRIMARY KEY,
                            DayShiftStart TEXT NOT NULL,
                            DayShiftEnd TEXT NOT NULL,
                            NightShiftStart TEXT NOT NULL,
                            NightShiftEnd TEXT NOT NULL,
                            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP)";
                    command.ExecuteNonQuery();
                };
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }

        public async Task Add()
        {
            try
            {
                // initial schedule
                Schedule newSchedule = new Schedule()
                {
                    DayShiftStart = new TimeOnly(7, 0).ToString("HH:mm"),
                    DayShiftEnd = new TimeOnly(14, 0).ToString("HH:mm"),
                    NightShiftStart = new TimeOnly(18, 0).ToString("HH:mm"),
                    NightShiftEnd = new TimeOnly(2, 0).ToString("HH:mm"),
                };
                using (var checkCommand = _connection.CreateCommand())
                {
                    checkCommand.CommandText = @"SELECT COUNT(*) FROM tbl_schedule WHERE Id=1";
                    int schedule_ct = int.TryParse(checkCommand.ExecuteScalar()?.ToString(), out int ct) ? ct : 0;

                    if (schedule_ct == 0)
                    {
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    INSERT INTO tbl_schedule (Id, DayShiftStart, DayShiftEnd, NightShiftStart, NightShiftEnd)
                                    VALUES (@id, @dayShiftStart, @dayShiftEnd, @nightShiftStart, @nightShiftEnd)";
                            command.Parameters.AddWithValue("@id", 1);
                            command.Parameters.AddWithValue("@dayShiftStart", newSchedule.DayShiftStart);
                            command.Parameters.AddWithValue("@dayShiftEnd", newSchedule.DayShiftEnd);
                            command.Parameters.AddWithValue("@nightShiftStart", newSchedule.NightShiftStart);
                            command.Parameters.AddWithValue("@nightShiftEnd", newSchedule.NightShiftEnd);
                            command.ExecuteNonQuery();
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }

        public async Task<ServiceResult> Update(Schedule newSchedule)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                                    UPDATE tbl_schedule SET 
                                    DayShiftStart=@dayShiftStart, DayShiftEnd=@dayShiftEnd, 
                                    NightShiftStart=@nightShiftStart, NightShiftEnd=@nightShiftEnd 
                                    WHERE Id = 1";
                    command.Parameters.AddWithValue("@dayShiftStart", newSchedule.DayShiftStart);
                    command.Parameters.AddWithValue("@dayShiftEnd", newSchedule.DayShiftEnd);
                    command.Parameters.AddWithValue("@nightShiftStart", newSchedule.NightShiftStart);
                    command.Parameters.AddWithValue("@nightShiftEnd", newSchedule.NightShiftEnd);
                    command.ExecuteNonQuery();
                };
                return new ServiceResult
                {
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
                return new ServiceResult
                {
                    Success = false,
                };
            }
        }

        public async Task<ServiceResult> Load()
        {
            try
            {
                await Add();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                                   SELECT * FROM tbl_schedule ORDER BY CreatedAt ASC LIMIT 1";
                    command.ExecuteNonQuery();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Schedule schedule = new Schedule()
                            {
                                DayShiftStart = reader["DayShiftStart"].ToString(),
                                DayShiftEnd = reader["DayShiftEnd"].ToString(),
                                NightShiftStart = reader["NightShiftStart"].ToString(),
                                NightShiftEnd = reader["NightShiftEnd"].ToString(), 
                            };
                            return new ServiceResult
                            {
                                Success = true,
                                Data = schedule
                            };
                        }
                    }
                    throw new Exception("Something went wrong");
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorType = Constants.CONNECTION_ERROR_TYPE,
                    ErrorMessage = ex.Message
                };

            }
        }
    }
}
