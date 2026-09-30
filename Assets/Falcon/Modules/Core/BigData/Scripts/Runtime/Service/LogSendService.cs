/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class LogSendService : MySingleton<LogSendService>
    {
        private const string SINGLE_URL = "https://dwhapi-v2.data4game.com/event-log-v2";
        private const string BATCH_URL = "https://dwhapi-v2.data4game.com/batch/event-log-v2";
        private readonly AnalyticConfigService _configService;

        public LogSendService(AnalyticConfigService configService)
        {
            _configService = configService;
        }

        public async Task Send(DataWrapper wrapper) 
        {
            if(_configService.ShouldNotSendLogToServer) return;

            // Per DWH-EventLog-API-Spec §6.1: endpoint đơn trả plain text, success = đúng chuỗi này (server có thể kèm whitespace cuối).
            var response = await (await new PostRequest(SINGLE_URL).SetJsonBody(wrapper).Execute()).SuccessStrBody();
            if (string.Equals(response?.TrimEnd(), "Request processed successfully.", StringComparison.Ordinal))
            {
                AnalyticLogger.Instance.Info(wrapper.@event + " has been sent successfully");
            }
            else
                Debug.LogError(wrapper.@event +
                               " has been sent failed with the response of: " + response);

            if (_configService.Testing)
            {
                (await new PostRequest(_configService.TestingSingleUrl).SetJsonBody(wrapper).Execute()).Close();
            }
        }

        public async Task SendBatch(ICollection<DataWrapper> wrappers)
        {
            if(!wrappers.Any() || _configService.ShouldNotSendLogToServer) return;
            if (wrappers.Count == 1)
            {
                await Send(wrappers.First());
                return;
            }
            var batchResponse = await (await new PostRequest(BATCH_URL).SetJsonBody(wrappers.Select(wrapper => wrapper.ToJson())).Execute()).SuccessObj<BatchProcessResponse>();
            if (batchResponse.errors.Count > 0)
            {
                foreach (var messageProcessErrorInfo in batchResponse.errors)
                {
                    Debug.LogError("Message has been sent failed with the response of: " +
                                   messageProcessErrorInfo.exception);
                }
            }
            else
            {
                AnalyticLogger.Instance.Info(wrappers.Count + " logs has been sent successfully");
            }
            
            if (_configService.Testing)
            {
                (await new PostRequest(_configService.TestingBatchUrl).SetJsonBody(wrappers).Execute()).Close();
            }
        }
    }
}