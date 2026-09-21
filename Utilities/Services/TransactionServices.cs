using Formatter.Helper;
using ISCALE.MVVM.Model;
using Microsoft.Data.Sqlite;
using System.Windows.Media;

namespace ISCALE.Utilities.Services
{
    public class TransactionServices
    {
        private readonly SqliteConnection _connection;

        public TransactionServices(SqliteConnection connectin)
        {
            _connection = connectin;
            CreateTable();
        }

        private async Task CreateTable()
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        var command = _connection.CreateCommand();
                        command.Transaction = transaction;

                        // Transaction
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS tbl_transactions (
                        JobId TEXT PRIMARY KEY UNIQUE, 
                        Code INTEGER DEFAULT 0,
                        Sequence INTEGER DEFAULT 0, 
                        Section TEXT NOT NULL,
                        Weigher TEXT NOT NULL, 
                        ScaleNo TEXT NOT NULL,
                        MachineNo TEXT NOT NULL, 
                        OperatorNo TEXT NOT NULL, 
                        PackerNo TEXT NOT NULL, 
                        TotalSuccessWeight REAL NOT NULL DEFAULT 0,
                        TotalFailedWeight REAL NOT NULL DEFAULT 0,
                        AverageWeight REAL NOT NULL DEFAULT 0,
                        SuccessCount INTEGER NOT NULL DEFAULT 0,
                        FailedCount INTEGER NOT NULL DEFAULT 0,
                        DeletedCount INTEGER NOT NULL DEFAULT 0,
                        TotalCount INTEGER NOT NULL DEFAULT 0,
                        DeleteNote TEXT,
                        Status TEXT NOT NULL DEFAULT 'on_progress',
                        FinishedAt DATETIME,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);";
                        await command.ExecuteNonQueryAsync();


                        command.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS uq_code_sequence_progress ON tbl_transactions (Code,Sequence) WHERE Status = 'completed'";
                        await command.ExecuteNonQueryAsync();

                        // TransactionDetails
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS tbl_transaction_details (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        JobId TEXT NOT NULL, 
                        ItemId TEXT NOT NULL, 
                        ItemCode TEXT NOT NULL, 
                        InternalReference TEXT NOT NULL, 
                        ItemDescription TEXT NOT NULL, 
                        ItemUpperLimit REAL NOT NULL DEFAULT 0, 
                        ItemLowerLimit REAL NOT NULL DEFAULT 0, 
                        SupervisorId TEXT NOT NULL,
                        SupervisorName TEXT NOT NULL,
                        ScheduleShift TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);";
                        await command.ExecuteNonQueryAsync();

                        // TransactionItems
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS tbl_transaction_items (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        JobId TEXT NOT NULL, 
                        TempId TEXT NOT NULL UNIQUE,
                        Weight REAL NOT NULL DEFAULT 0, 
                        Status TEXT NOT NULL, 
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);";
                        await command.ExecuteNonQueryAsync();


                        // Migrations
                        command.Parameters.Clear();
                        command.CommandText = @"PRAGMA user_version;";
                        int user_version = Convert.ToInt32(await command.ExecuteScalarAsync());

                        // QA# added with default ''
                        if (user_version < 1)
                        {
                            command.Parameters.Clear();
                            command.CommandText = @"ALTER TABLE tbl_transactions ADD COLUMN QaNo TEXT DEFAULT ''";
                            await command.ExecuteNonQueryAsync();

                            command.Parameters.Clear();
                            command.CommandText = @"PRAGMA user_version=1;";
                            await command.ExecuteNonQueryAsync();
                            user_version = 1;
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }
        public async Task<ServiceResult> GetLatestTransactionId(string code)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"SELECT MAX(Sequence) FROM tbl_transactions WHERE Code = @code";
                    command.Parameters.AddWithValue("@code", code);
                    int max = int.TryParse(command.ExecuteScalar()?.ToString(), out int ct) ? ct + 1 : 1;

                    return new ServiceResult
                    {
                        Success = true,
                        Data = code + max.ToString("D3")
                    };
                }
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
        public async Task<ServiceResult> AddTransaction(Transaction newTransaction)
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        using (var transactionCommand = _connection.CreateCommand())
                        {
                            transactionCommand.Transaction = transaction;
                            // insert transaction
                            transactionCommand.CommandText = @"
                                    INSERT INTO tbl_transactions 
                                    (JobId, Code, Weigher, ScaleNo, MachineNo, QaNo, OperatorNo, PackerNo, Section, CreatedAt)
                                    VALUES (@jobId, @code, @weigher, @scaleNo, @machineNo, @qaNo, @operatorNo, @packerNo, @section, @createdAt);";

                            transactionCommand.Parameters.AddWithValue("@jobId", newTransaction.JobId);
                            transactionCommand.Parameters.AddWithValue("@code", newTransaction.Code);
                            transactionCommand.Parameters.AddWithValue("@weigher", newTransaction.Weigher);
                            transactionCommand.Parameters.AddWithValue("@scaleNo", newTransaction.ScaleNo);
                            transactionCommand.Parameters.AddWithValue("@machineNo", newTransaction.MachineNo);
                            transactionCommand.Parameters.AddWithValue("@qaNo", newTransaction.QaNo);
                            transactionCommand.Parameters.AddWithValue("@operatorNo", newTransaction.OperatorNo);
                            transactionCommand.Parameters.AddWithValue("@section", newTransaction.Section);
                            transactionCommand.Parameters.AddWithValue("@packerNo", newTransaction.PackerNo);
                            transactionCommand.Parameters.AddWithValue("@createdAt", DateTimeHelper.DateLocalToUtc(newTransaction.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"));


                            await transactionCommand.ExecuteNonQueryAsync();
                        }
                        using (var transactionDetailsCommand = _connection.CreateCommand())
                        {
                            transactionDetailsCommand.Transaction = transaction;
                            // insert transactiondetails
                            transactionDetailsCommand.CommandText = @"
                                    INSERT INTO tbl_transaction_details 
                                    (JobId, ItemId, ItemCode, InternalReference, ItemDescription, ItemUpperLimit, ItemLowerLimit, SupervisorId, SupervisorName,ScheduleShift )
                                    VALUES (@jobId, @itemId, @itemCode, @internalReference, @itemDescription, @itemUpperLimit, @itemLowerLimit, @supervisorId, @supervisorName, @scheduleShift);";

                            transactionDetailsCommand.Parameters.AddWithValue("@jobId", newTransaction.JobId);
                            transactionDetailsCommand.Parameters.AddWithValue("@itemId", newTransaction.Item.Id);
                            transactionDetailsCommand.Parameters.AddWithValue("@itemCode", newTransaction.Item.Code);
                            transactionDetailsCommand.Parameters.AddWithValue("@internalReference", newTransaction.Item.InternalReference);
                            transactionDetailsCommand.Parameters.AddWithValue("@itemDescription", newTransaction.Item.Description);
                            transactionDetailsCommand.Parameters.AddWithValue("@itemUpperLimit", newTransaction.Item.UpperLimit);
                            transactionDetailsCommand.Parameters.AddWithValue("@itemLowerLimit", newTransaction.Item.LowerLimit);
                            transactionDetailsCommand.Parameters.AddWithValue("@supervisorId", newTransaction.Supervisor.Id);
                            transactionDetailsCommand.Parameters.AddWithValue("@supervisorName", newTransaction.Supervisor.Username);
                            transactionDetailsCommand.Parameters.AddWithValue("@scheduleShift", newTransaction.ScheduleShift);


                            await transactionDetailsCommand.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return new ServiceResult
                        {
                            Success = true,
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

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
        public async Task<ServiceResult> UpdateTransaction(Transaction newTransaction)
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {

                        using (var updateCommand = _connection.CreateCommand())
                        {
                            // Responsible for number of transaction today date
                            updateCommand.Transaction = transaction;
                            updateCommand.CommandText = @"SELECT MAX(Sequence) FROM tbl_transactions WHERE Code = @code";
                            updateCommand.Parameters.AddWithValue("@code", newTransaction.Code);
                            var newSequnce = await updateCommand.ExecuteScalarAsync();
                            int newSequence = int.TryParse(newSequnce?.ToString(), out int ct) ? ct + 1 : 1;
                            await UpdateTransactionPartial(transaction, newTransaction, "completed", newSequence);
                        }
                        transaction.Commit();
                        return new ServiceResult
                        {
                            Success = true,
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
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
        public async Task<ServiceResult> AddTransactionItem(TransactionItem transactionItem, Transaction newTransaction)
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        using (var itemCommand = _connection.CreateCommand())
                        {
                            itemCommand.Transaction = transaction;

                            itemCommand.CommandText = @"
                                 INSERT INTO tbl_transaction_items 
                                 (JobId, TempId, Weight, IsDeleted, Status, CreatedAt)
                                 VALUES (@jobId, @tempId, @weight, @isDeleted, @status, @createdAt);";

                            itemCommand.Parameters.Clear();
                            itemCommand.Parameters.AddWithValue("@jobId", newTransaction.JobId);
                            itemCommand.Parameters.AddWithValue("@tempId", transactionItem.TempId);
                            itemCommand.Parameters.AddWithValue("@weight", transactionItem.Weight);
                            itemCommand.Parameters.AddWithValue("@isDeleted", transactionItem.IsDeleted);
                            itemCommand.Parameters.AddWithValue("@status", transactionItem.Status);
                            itemCommand.Parameters.AddWithValue("@createdAt", transactionItem.DateCreated);

                            await itemCommand.ExecuteNonQueryAsync();

                            await UpdateTransactionPartial(transaction, newTransaction, "on_progress");
                        }
                        transaction.Commit();

                        return new ServiceResult
                        {
                            Success = true,
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

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
        public async Task<ServiceResult> DeleteTransactionItem(TransactionItem item, Transaction newTransaction)
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        using (var itemCommand = _connection.CreateCommand())
                        {
                            itemCommand.Transaction = transaction;
                            itemCommand.CommandText = @"
                                 UPDATE tbl_transaction_items SET IsDeleted=@newIsDeleted WHERE JobId=@jobId AND TempId=@tempId;";

                            itemCommand.Parameters.Clear();
                            itemCommand.Parameters.AddWithValue("@jobId", newTransaction.JobId);
                            itemCommand.Parameters.AddWithValue("@tempId", item.TempId);
                            itemCommand.Parameters.AddWithValue("@newIsDeleted", item.IsDeleted);
                            await itemCommand.ExecuteNonQueryAsync();
                        }
                        await UpdateTransactionPartial(transaction, newTransaction, "on_progress");
                        transaction.Commit();
                        return new ServiceResult
                        {
                            Success = true,
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }

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
        public async Task<ServiceResult> SearchTransactionReport(TransactionSearchPayload payload)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    var filters = new List<string>
                    {
                        "tt.CreatedAt BETWEEN @startDate AND @endDate AND Status = 'completed'",
                    };

                    if (!string.IsNullOrEmpty(payload.Weigher))
                    {
                        if (payload.IsWeigherSpecific)
                            filters.Add("tt.Weigher = @weigher COLLATE NOCASE");
                        else
                            filters.Add("tt.Weigher LIKE @weigher");
                    }

                    command.CommandText = @"
                                    SELECT tt.JobId, tt.Weigher, tt.ScaleNo, tt.MachineNo, tt.QaNo, tt.OperatorNo, tt.PackerNo, tt.CreatedAt as TransactionCreatedAt, tt.FinishedAt as TransactionFinishedAt,
                                    tt.Code, tt.Sequence, tt.Section, tt.TotalSuccessWeight, tt.TotalFailedWeight, tt.AverageWeight, tt.SuccessCount, tt.FailedCount, tt.DeletedCount, tt.TotalCount, tt.DeleteNote,
                                    td.ItemId, td.ItemCode, td.InternalReference, td.ItemDescription, td.ItemUpperLimit, td.ItemLowerLimit, td.SupervisorId,td.ScheduleShift, td.SupervisorName, td.CreatedAt as DetailsCreatedAt
                                    FROM tbl_transactions as tt INNER JOIN tbl_transaction_details as td ON tt.JobId = td.JobId
                                    WHERE " + string.Join(" AND ", filters);
                    command.Parameters.AddWithValue("@startDate", payload.StartDate);
                    command.Parameters.AddWithValue("@endDate", payload.EndDate);

                    if (!string.IsNullOrEmpty(payload.Weigher))
                        command.Parameters.AddWithValue("@weigher", payload.Weigher + (!payload.IsWeigherSpecific ? "%" : string.Empty));

                    await command.ExecuteNonQueryAsync();

                    using (var reader = command.ExecuteReader())
                    {
                        List<Transaction> transactions = new List<Transaction>();
                        while (reader.Read())
                        {
                            string transactionId = $"{reader["Code"].ToString()}{(int.TryParse(reader["Sequence"].ToString(), out int sq) ? sq : 0).ToString("D3")}";

                            transactions.Add(new Transaction
                            {
                                JobId = (reader["JobId"].ToString()),
                                Weigher = reader["Weigher"].ToString(),
                                ScaleNo = reader["ScaleNo"].ToString(),
                                PackerNo = reader["PackerNo"].ToString(),
                                TransactionId = transactionId,
                                MachineNo = reader["MachineNo"].ToString(),
                                QaNo = reader["QaNo"].ToString(),
                                OperatorNo = reader["OperatorNo"].ToString(),
                                ScheduleShift = reader["ScheduleShift"].ToString(),
                                Section = reader["Section"].ToString(),
                                TotalSuccessWeight = decimal.TryParse(reader["TotalSuccessWeight"].ToString(), out decimal tsw) ? tsw : 0,
                                TotalFailedWeight = decimal.TryParse(reader["TotalFailedWeight"].ToString(), out decimal tfw) ? tfw : 0,
                                AverageWeight = decimal.TryParse(reader["AverageWeight"].ToString(), out decimal aw) ? aw : 0,
                                SuccessCount = int.TryParse(reader["SuccessCount"].ToString(), out int sc) ? sc : 0,
                                FailedCount = int.TryParse(reader["FailedCount"].ToString(), out int fc) ? fc : 0,
                                DeletedCount = int.TryParse(reader["DeletedCount"].ToString(), out int dc) ? dc : 0,
                                TotalCount = int.TryParse(reader["TotalCount"].ToString(), out int tc) ? tc : 0,
                                DeleteNote = reader["DeleteNote"].ToString(),
                                Supervisor = new User()
                                {
                                    Id = reader["SupervisorId"].ToString(),
                                    Username = reader["SupervisorName"].ToString()
                                },
                                Item = new ProductItem()
                                {
                                    Code = (reader["ItemCode"].ToString()),
                                    Description = (reader["ItemDescription"].ToString()),
                                    InternalReference = (reader["InternalReference"].ToString()),
                                    UpperLimit = decimal.TryParse(reader["ItemUpperLimit"].ToString(), out decimal upper) ? upper : 0,
                                    LowerLimit = decimal.TryParse(reader["ItemLowerLimit"].ToString(), out decimal lower) ? lower : 0,
                                },
                                CreatedAt = DateTime.TryParse(reader["TransactionCreatedAt"].ToString(), out DateTime tca) ? tca : new DateTime(),
                                FinishedAt = DateTime.TryParse(reader["TransactionFinishedAt"].ToString(), out DateTime tfa) ? tfa : new DateTime(),
                            });
                        }
                        return new ServiceResult
                        {
                            Success = true,
                            Data = transactions
                        };
                    }
                }
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
        public async Task<ServiceResult> SearchTransactionReportItem(string Id, bool isDeleteIncluded = false)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    var filters = new List<string>
                    {
                        "JobId = @jobId",
                    };
                    if (!isDeleteIncluded)
                    {
                        filters.Add("IsDeleted = @isDeleted");
                    }
                    command.CommandText = @"
                                    SELECT * FROM tbl_transaction_items
                                    WHERE "+string.Join(" AND ", filters) +" ORDER BY CreatedAt";
                    command.Parameters.AddWithValue("@jobId", Id);
                    if (!isDeleteIncluded)
                        command.Parameters.AddWithValue("@isDeleted", 0);

                    await command.ExecuteNonQueryAsync();

                    using (var reader = command.ExecuteReader())
                    {
                        List<TransactionItem> items = new List<TransactionItem>();
                        int counter = 1;
                        while (reader.Read())
                        {
                            string status = reader["Status"].ToString() ?? string.Empty;
                            items.Add(new TransactionItem
                            {
                                Id = counter,
                                TempId = reader["TempId"].ToString() ?? string.Empty,
                                Status = reader["Status"].ToString() ?? string.Empty,
                                Weight = decimal.TryParse(reader["Weight"].ToString(), out decimal weight) ? weight : 0,
                                IsDeleted = Convert.ToInt32(reader["IsDeleted"]) == 1,
                                DateCreated = DateTime.TryParse(reader["CreatedAt"].ToString(), out DateTime created) ? created : DateTime.Now.ToLocalTime(),
                                StatusForeground = status == "Success" ? Brushes.LimeGreen : Brushes.Red,
                            });

                            counter++;
                        }
                        return new ServiceResult
                        {
                            Success = true,
                            Data = items
                        };
                    }
                }
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
        private async Task UpdateTransactionPartial(SqliteTransaction transaction, Transaction newTransaction, string status, int newSequence = 0)
        {
            using (var updateCommand = _connection.CreateCommand())
            {
                string sequence = newSequence == 0 ? string.Empty : newSequence.ToString();
                List<string> update_fields = new List<string>
                {
                    "TotalSuccessWeight = @totalSuccessWeight",
                    "TotalFailedWeight = @totalFailedWeight",
                    "AverageWeight = @averageWeight",
                    "SuccessCount = @successCount",
                    "FailedCount = @failedCount",
                    "Status = @status",
                    "DeletedCount = @deletedCount",
                    "TotalCount = @totalCount",
                };

                if (status == "completed")
                {
                    update_fields.Add("FinishedAt = @finishedAt");
                    update_fields.Add("DeleteNote = @deleteNote");
                }

                if (!string.IsNullOrWhiteSpace(sequence))
                    update_fields.Add("Sequence = @sequence");

                updateCommand.Transaction = transaction;
                updateCommand.CommandText = $@"UPDATE tbl_transactions SET {string.Join(',', update_fields)} WHERE JobId = @jobId";

                updateCommand.Parameters.Clear();
                if (!string.IsNullOrWhiteSpace(sequence))
                    updateCommand.Parameters.AddWithValue("@sequence", newSequence);

                if (status == "completed")
                {
                    updateCommand.Parameters.AddWithValue("@finishedAt", newTransaction.FinishedAt);
                    updateCommand.Parameters.AddWithValue("@deleteNote", newTransaction.DeleteNote ?? string.Empty);
                }

                updateCommand.Parameters.AddWithValue("@totalSuccessWeight", newTransaction.TotalSuccessWeight);
                updateCommand.Parameters.AddWithValue("@totalFailedWeight", newTransaction.TotalFailedWeight);
                updateCommand.Parameters.AddWithValue("@averageWeight", newTransaction.AverageWeight);
                updateCommand.Parameters.AddWithValue("@successCount", newTransaction.SuccessCount);
                updateCommand.Parameters.AddWithValue("@failedCount", newTransaction.FailedCount);
                updateCommand.Parameters.AddWithValue("@totalCount", newTransaction.TotalCount);
                updateCommand.Parameters.AddWithValue("@deletedCount", newTransaction.DeletedCount);
                updateCommand.Parameters.AddWithValue("@status", status);

                updateCommand.Parameters.AddWithValue("@jobId", newTransaction.JobId);

                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        public async Task<ServiceResult> SearchTransactionRecover(TransactionRecoverPayload payload)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                                    SELECT tt.JobId, tt.Weigher, tt.ScaleNo, tt.MachineNo, tt.QaNo, tt.OperatorNo, tt.PackerNo, tt.CreatedAt as TransactionCreatedAt, tt.FinishedAt as TransactionFinishedAt,
                                    tt.Code, tt.Sequence, tt.Section, tt.TotalSuccessWeight, tt.TotalFailedWeight, tt.AverageWeight, tt.SuccessCount, tt.FailedCount, tt.DeletedCount, tt.TotalCount, tt.DeleteNote,
                                    td.ItemId, td.ItemCode, td.InternalReference, td.ItemDescription, td.ItemUpperLimit, td.ItemLowerLimit, td.SupervisorId,td.ScheduleShift, td.SupervisorName, td.CreatedAt as DetailsCreatedAt
                                    FROM tbl_transactions as tt INNER JOIN tbl_transaction_details as td ON tt.JobId = td.JobId
                                    WHERE tt.Status = 'on_progress' AND tt.TotalCount > 0 ORDER BY tt.CreatedAt DESC LIMIT 20 ";

                    //tt.Weigher = @weigher COLLATE NOCASE AND

                    await command.ExecuteNonQueryAsync();

                    using (var reader = command.ExecuteReader())
                    {
                        List<Transaction> transactions = new List<Transaction>();
                        while (reader.Read())
                        {
                            string transactionId = $"{reader["Code"].ToString()}{(int.TryParse(reader["Sequence"].ToString(), out int sq) ? sq : 0).ToString("D3")}";

                            transactions.Add(new Transaction
                            {
                                JobId = reader["JobId"].ToString() ?? "",
                                Weigher = reader["Weigher"].ToString() ?? "",
                                ScaleNo = reader["ScaleNo"].ToString() ?? "",
                                PackerNo = reader["PackerNo"].ToString() ?? "",
                                TransactionId = transactionId,
                                MachineNo = reader["MachineNo"].ToString() ?? "",
                                QaNo = reader["QaNo"].ToString() ?? "",
                                OperatorNo = reader["OperatorNo"].ToString() ?? "",
                                ScheduleShift = reader["ScheduleShift"].ToString() ?? "",
                                Section = reader["Section"].ToString() ?? "",
                                TotalSuccessWeight = decimal.TryParse(reader["TotalSuccessWeight"].ToString(), out decimal tsw) ? tsw : 0,
                                TotalFailedWeight = decimal.TryParse(reader["TotalFailedWeight"].ToString(), out decimal tfw) ? tfw : 0,
                                AverageWeight = decimal.TryParse(reader["AverageWeight"].ToString(), out decimal aw) ? aw : 0,
                                SuccessCount = int.TryParse(reader["SuccessCount"].ToString(), out int sc) ? sc : 0,
                                FailedCount = int.TryParse(reader["FailedCount"].ToString(), out int fc) ? fc : 0,
                                DeletedCount = int.TryParse(reader["DeletedCount"].ToString(), out int dc) ? dc : 0,
                                TotalCount = int.TryParse(reader["TotalCount"].ToString(), out int tc) ? tc : 0,
                                DeleteNote = reader["DeleteNote"].ToString() ?? "",
                                Supervisor = new User()
                                {
                                    Id = reader["SupervisorId"].ToString() ?? "",
                                    Username = reader["SupervisorName"].ToString() ?? ""
                                },
                                Item = new ProductItem()
                                {
                                    Code = reader["ItemCode"].ToString() ?? "",
                                    Description = reader["ItemDescription"].ToString() ?? "",
                                    InternalReference = reader["InternalReference"].ToString() ?? "",
                                    UpperLimit = Decimal.TryParse(reader["ItemUpperLimit"].ToString(), out decimal upper) ? upper : 0,
                                    LowerLimit = Decimal.TryParse(reader["ItemLowerLimit"].ToString(), out decimal lower) ? lower : 0,
                                },
                                CreatedAt = DateTime.TryParse(reader["TransactionCreatedAt"].ToString(), out DateTime tca) ? tca : new DateTime(),
                                FinishedAt = DateTime.TryParse(reader["TransactionFinishedAt"].ToString(), out DateTime tfa) ? tfa : new DateTime(),
                            });
                        }
                        return new ServiceResult
                        {
                            Success = true,
                            Data = transactions
                        };
                    }
                }
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
