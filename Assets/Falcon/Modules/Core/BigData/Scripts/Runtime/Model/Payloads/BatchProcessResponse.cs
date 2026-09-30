/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class BatchProcessResponse
    {
        public int successCount;
        public int failCount;
        public List<MessageProcessErrorInfo> errors= new();
    }
}