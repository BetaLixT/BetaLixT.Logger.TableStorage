using System;
using System.Collections.Generic;
using System.Text;

namespace BetaLixT.Logger.TableStorage
{
    public class TableStorageLoggerOptions
    {
        public const string OptionsKey = "TableStorageLoggerOptions";
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
    }
}
