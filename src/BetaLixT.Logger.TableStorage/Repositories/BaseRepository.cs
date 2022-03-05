using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetaLixT.Logger.TableStorage.Repositories
{
    public class BaseRepository<T> where T : TableEntity, new()
    {
        public readonly string defaultPartitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="tableName">The name of the table in Azure Table Storage.</param>
        /// <param name="defaultPartitionKey">Default partition key value.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public BaseRepository(
            IOptions<TableStorageLoggerOptions> options,
            string tableName,
            string defaultPartitionKey)
        {
            var storageAccountConnectionString = options.Value.ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.Table = tableClient.GetTableReference(tableName);

            this.defaultPartitionKey = defaultPartitionKey;
        }

        /// <summary>
        /// Gets cloud table instance.
        /// </summary>
        public CloudTable Table { get; }

        /// <summary>
        /// Create or update an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be created or updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task InsertOrReplaceBatchAsync(IList<T> entities)
        {
            var operations = new TableBatchOperation();
            foreach (var entity in entities)
            {
                operations.InsertOrReplace(entity);
            }

            await this.Table.ExecuteBatchAsync(operations);
        }


        /// <summary>
        /// Delete an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be deleted.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task DeleteAsync(T entity)
        {
            var operation = TableOperation.Delete(entity);

            await this.Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// Get an entity by the keys in the table storage.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key fo the entity.</param>
        /// <returns>The entity matching the keys.</returns>
        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = await this.Table.ExecuteAsync(operation);

            return result.Result as T;
        }


        /// <summary>
        /// Get entities from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <param name="partition">Partition key value.</param>
        /// <returns>All data entities.</returns>
        public async Task<List<T>> GetWithSelectAsync(List<string> select, string partition = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var query = new TableQuery<T>().Where(partitionKeyFilter);
            query.Select(select);

            var entities = await this.ExecuteQueryAsync(query);

            return entities;
        }

        /// <summary>
        /// Get entities from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <param name="partition">Partition key value.</param>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetWithFilterAsync(string filter, string partition = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var combinedFilter = this.CombineFilters(filter, partitionKeyFilter);

            var query = new TableQuery<T>().Where(combinedFilter);

            var entities = await this.ExecuteQueryAsync(query);

            return entities;
        }


        /// <summary>
        /// Get entities with selected properties from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="select">List of names of properties to select.</param>
        /// <param name="filter">Filter to the result.</param>
        /// <param name="partition">Partition key value.</param>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetWithSelectFilterAsync(List<string> select, string filter, string partition = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var combinedFilter = this.CombineFilters(filter, partitionKeyFilter);

            var query = new TableQuery<T>().Where(combinedFilter);
            query.Select(select);

            var entities = await this.ExecuteQueryAsync(query);

            return entities;
        }

        /// <summary>
        /// Get all data entities from the table storage in a partition.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetAllAsync(string partition = null, int? count = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var query = new TableQuery<T>().Where(partitionKeyFilter);

            var entities = await this.ExecuteQueryAsync(query, count);

            return entities;
        }

        private string CombineFilters(string filter1, string filter2)
        {
            if (string.IsNullOrWhiteSpace(filter1) && string.IsNullOrWhiteSpace(filter2))
            {
                return string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(filter1))
            {
                return filter2;
            }
            else if (string.IsNullOrWhiteSpace(filter2))
            {
                return filter1;
            }

            return TableQuery.CombineFilters(filter1, TableOperators.And, filter2);
        }

        private string GetPartitionKeyFilter(string partition)
        {
            var filter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                string.IsNullOrWhiteSpace(partition) ? this.defaultPartitionKey : partition);
            return filter;
        }

        private async Task<List<T>> ExecuteQueryAsync(
            TableQuery<T> query,
            int? count = null,
            CancellationToken ct = default)
        {
            query.TakeCount = count;

            try
            {
                var result = new List<T>();
                TableContinuationToken token = null;

                do
                {
                    TableQuerySegment<T> seg = await this.Table.ExecuteQuerySegmentedAsync<T>(query, token);
                    token = seg.ContinuationToken;
                    result.AddRange(seg);
                }
                while (token != null
                    && !ct.IsCancellationRequested
                    && (count == null || result.Count < count.Value));

                return result;
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
