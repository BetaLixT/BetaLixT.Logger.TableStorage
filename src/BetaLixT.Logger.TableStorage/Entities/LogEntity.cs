using BetaLixT.Logger.Entities;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace BetaLixT.Logger.TableStorage.Entities
{
    public class LogEntity : TableEntity
    {
        private static Random _rnd = new Random();

        public string NodeName { get => this.PartitionKey; set => this.PartitionKey = value; }
        public int LogLevel { get; set; }
        public string LogName { get; set; }
        public int EventId { get; set; }
        public string Message { get; set; }
        public string LogLevelString { get; set; }
        public string Data { get; set; }
        public string Exception { get; set; }
        public string RequestId { get; set; }
        public string CorrelationId { get; set; }

        public LogEntity() {}
        public LogEntity(LogMessage log)
        {
            this.RowKey = log.EventTime.ToUnixTimeMilliseconds().ToString() + _rnd.Next(0, 1000).ToString("0000");
            this.Timestamp = log.EventTime;


            var serializationOptions = new Newtonsoft.Json.JsonSerializerSettings{
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore 
            };
            this.LogName = log.LogName;
            this.NodeName = log.NodeName;
            this.LogLevel = log.LogLevel;
            this.EventId = log.EventId;
            this.Message = log.Message;
            this.LogLevelString = log.LogLevelString;
            if (log.Exception != null)
            {
                this.Exception =  JsonConvert.SerializeObject(
                    log.Exception,
                    serializationOptions
                );
            }
            var data = new Dictionary<string, string>();
            var iter = 0;
            
            foreach(var scope in log.Scopes)
            {
                if (scope is IEnumerable<KeyValuePair<string, object>>)
                {
                    var s = (IEnumerable<KeyValuePair<string, object>>)scope;
                    foreach(var param in s)
                    {
                        if (param.Key != "{OriginalFormat}")
                        {
                            data[param.Key] = param.Value.ToString();
                            if (param.Key == "RequestId")
                            {
                                this.RequestId = param.Value.ToString();
                            }
                            else if (param.Key == "CorrelationId")
                            {
                                this.CorrelationId = param.Value.ToString();
                            }
                        }
                    }
                }
                else if(scope is KeyValuePair<string, object>)
                {
                    data[((KeyValuePair<string, object>)scope).Key] = ((KeyValuePair<string, object>)scope).Value.ToString();
                    if (((KeyValuePair<string, object>)scope).Key == "RequestId")
                    {
                        this.RequestId = ((KeyValuePair<string, object>)scope).Value.ToString();
                    }
                    else if (((KeyValuePair<string, object>)scope).Key == "CorrelationId")
                    {
                        this.CorrelationId = ((KeyValuePair<string, object>)scope).Value.ToString();
                    }
                }
                else
                {
                    data[$"scope:{iter}"] = scope.ToString();
                    iter++;
                }
            }
            this.Data = JsonConvert.SerializeObject(
                data,
                serializationOptions
            );
        }
    }
}
