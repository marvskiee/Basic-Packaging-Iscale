using DocumentFormat.OpenXml.Office.Word;
using ISCALE.MVVM.Model;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
namespace ISCALE.Utilities.Services
{
    public class ProductServices
    {
        private readonly SqliteConnection _connection;
        private readonly string TABLE_QUERY = @"
                        CREATE TABLE IF NOT EXISTS tbl_product_items (
                        Id TEXT PRIMARY KEY, 
                        Code TEXT NOT NULL, 
                        InternalReference TEXT,                         
                        Description TEXT, 
                        UpperLimit REAL NOT NULL DEFAULT 0, 
                        LowerLimit REAL NOT NULL DEFAULT 0, 
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        DeletedAt DATETIME NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);";
        private readonly string ACTIVE_CODE_INDEX_QUERY = @"CREATE UNIQUE INDEX IF NOT EXISTS active_code ON tbl_product_items (LOWER(Code)) WHERE IsDeleted = 0;";
        public ProductServices(SqliteConnection connection)
        {
            _connection = connection;
            CreateTable();
        }

        public async Task CreateTable()
        {
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = TABLE_QUERY;
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = ACTIVE_CODE_INDEX_QUERY;
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS tbl_product_metadata (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        EffectiveDate DATETIME NOT NULL, 
                        DocumentReferenceNo TEXT NOT NULL, 
                        RevisionNo TEXT NOT NULL, 
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);";
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
            return await ItemActions("LOAD");
        }
        public async Task<ServiceResult> DeleteItem(ProductItem oldProductItem)
        {
            return await ItemActions("DELETE", oldProductItem);
        }

        public async Task<ServiceResult> FindItem(ProductItem newProductItem)
        {
            return await ItemActions("FIND_ONE", newProductItem);
        }

        public async Task<ServiceResult> UploadCSV(ProductMetadata newProductMetadata)
        {
            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // drop first
                        using (var dropCommand = _connection.CreateCommand())
                        {
                            dropCommand.Transaction = transaction;
                            dropCommand.CommandText = @"DROP TABLE IF EXISTS tbl_product_items";
                            await dropCommand.ExecuteNonQueryAsync();
                        }
                        // create table
                        using (var createCommand = _connection.CreateCommand())
                        {
                            createCommand.Transaction = transaction;
                            createCommand.CommandText = TABLE_QUERY;
                            await createCommand.ExecuteNonQueryAsync();
                        }
                        // active code index 
                        using (var activeIndexCommand = _connection.CreateCommand())
                        {
                            activeIndexCommand.Transaction = transaction;
                            activeIndexCommand.CommandText = ACTIVE_CODE_INDEX_QUERY;
                            await activeIndexCommand.ExecuteNonQueryAsync();
                        }

                        using (var insertCommand = _connection.CreateCommand())
                        {
                            insertCommand.Transaction = transaction;

                            insertCommand.CommandText = @"
                                 INSERT INTO tbl_product_items
                                 (Id, Code, InternalReference, Description, UpperLimit, LowerLimit)
                                 VALUES (@id, @code, @internalReference, @description, @upperLimit, @lowerLimit)";

                            var idParams = insertCommand.Parameters.Add("@id", SqliteType.Text);
                            var codeParams = insertCommand.Parameters.Add("@code", SqliteType.Text);
                            var internalReferenceParams = insertCommand.Parameters.Add("@internalReference", SqliteType.Text);
                            var descriptionParams = insertCommand.Parameters.Add("@description", SqliteType.Text);
                            var upperParams = insertCommand.Parameters.Add("@upperLimit", SqliteType.Real);
                            var lowerParams = insertCommand.Parameters.Add("@lowerLimit", SqliteType.Real);

                            foreach (ProductItem item in newProductMetadata.ProductItem)
                            {
                                idParams.Value = item.Id;
                                codeParams.Value = item.Code;
                                internalReferenceParams.Value = item.InternalReference;
                                descriptionParams.Value = item.Description;
                                upperParams.Value = item.UpperLimit;
                                lowerParams.Value = item.LowerLimit;

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }

                        using (var metadataCommand = _connection.CreateCommand())
                        {
                            metadataCommand.Transaction = transaction;
                            metadataCommand.CommandText = @"
                            INSERT INTO tbl_product_metadata
                            (EffectiveDate, DocumentReferenceNo, RevisionNo)
                            VALUES (@effectiveDate, @documentReferenceNo, @revisionNo);";
                            metadataCommand.Parameters.AddWithValue("@effectiveDate", newProductMetadata.EffectiveDate);
                            metadataCommand.Parameters.AddWithValue("@documentReferenceNo", newProductMetadata.DocumentReferenceNo);
                            metadataCommand.Parameters.AddWithValue("@revisionNo", newProductMetadata.RevisionNo);
                            await metadataCommand.ExecuteNonQueryAsync();
                        }



                        await transaction.CommitAsync();
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

        private async Task<ServiceResult> ItemActions(string action, ProductItem currentProductItem = null)
        {
            try
            {
                switch (action)
                {
                    case "DELETE":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    UPDATE tbl_product_items SET 
                                    IsDeleted = $isDeleted,
                                    DeletedAt = $deletedAt
                                    WHERE Id = $id";
                            command.Parameters.AddWithValue("$isDeleted", 1);
                            command.Parameters.AddWithValue("$deletedAt", DateTime.UtcNow);
                            command.Parameters.AddWithValue("$id", currentProductItem.Id);

                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "LOAD":
                        using (var command = _connection.CreateCommand())
                        {
                            // load metadata
                            command.CommandText = @"
                                    SELECT * FROM tbl_product_metadata
                                    ORDER BY CreatedAt DESC LIMIT 1";

                            await command.ExecuteNonQueryAsync();

                            ProductMetadata productMetadata = new();

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    productMetadata = new ProductMetadata
                                    {
                                        Id = int.Parse(reader["Id"].ToString()),
                                        EffectiveDate = DateTime.TryParse(reader["EffectiveDate"].ToString(), out DateTime ed) ? ed : DateTime.UtcNow,
                                        DocumentReferenceNo = reader["DocumentReferenceNo"].ToString(),
                                        RevisionNo = reader["RevisionNo"].ToString(),
                                        CreatedAt = DateTime.TryParse(reader["CreatedAt"].ToString(), out DateTime dt) ? dt : DateTime.UtcNow,
                                    };
                                }
                            }

                            // load items
                            command.CommandText = @"
                                    SELECT * FROM tbl_product_items
                                    WHERE IsDeleted = 0";

                            await command.ExecuteNonQueryAsync();

                            List<ProductItem> items = new();

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    items.Add(new ProductItem
                                    {
                                        Id = (reader["Id"].ToString()),
                                        Code = reader["Code"].ToString(),
                                        InternalReference = reader["InternalReference"].ToString(),
                                        Description = reader["Description"].ToString(),
                                        UpperLimit = decimal.TryParse(reader["UpperLimit"].ToString(), out decimal upper) ? upper : 0,
                                        LowerLimit = decimal.TryParse(reader["LowerLimit"].ToString(), out decimal lower) ? lower : 0,
                                    });
                                }
                            }
                            productMetadata.ProductItem = new ObservableCollection<ProductItem>(items);

                            return new ServiceResult
                            {
                                Success = true,
                                Data = productMetadata
                            };
                        }
                    case "FIND_ONE":
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                                    SELECT * FROM tbl_product_items
                                    WHERE IsDeleted = 0 AND LOWER(Code) = @code LIMIT 1";

                            command.Parameters.AddWithValue("@code", currentProductItem.Code.ToLowerInvariant());
                            await command.ExecuteNonQueryAsync();

                            using (var reader = command.ExecuteReader())
                            {
                                List<ProductItem> items = new List<ProductItem>();
                                while (reader.Read())
                                {
                                    items.Add(new ProductItem
                                    {
                                        Id = (reader["Id"].ToString()),
                                        Code = reader["Code"].ToString(),
                                        InternalReference = reader["InternalReference"].ToString(),
                                        Description = reader["Description"].ToString(),
                                        UpperLimit = decimal.TryParse(reader["UpperLimit"].ToString(), out decimal upper) ? upper : 0,
                                        LowerLimit = decimal.TryParse(reader["LowerLimit"].ToString(), out decimal lower) ? lower : 0,
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
