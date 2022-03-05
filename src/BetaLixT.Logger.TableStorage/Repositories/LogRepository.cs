using BetaLixT.Logger.TableStorage.Entities;
using BetaLixT.Logger.Entities;
using BetaLixT.Logger.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaLixT.Logger.TableStorage.Repositories
{

    public class LogRepository : BaseRepository<LogEntity>, ILogRepository
    {
        public LogRepository(IOptions<TableStorageLoggerOptions> options)
            : base(
                options,
                options.Value.TableName,
                "-1")
        {
        }

        public async Task AddMessagesAsync(IList<LogMessage> messages)
        {
            try
            {
                var messageEntities = messages.Select(x => new LogEntity(x)).ToList();
                await this.InsertOrReplaceBatchAsync(messageEntities);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e);
            }
            
        }
    }
}
