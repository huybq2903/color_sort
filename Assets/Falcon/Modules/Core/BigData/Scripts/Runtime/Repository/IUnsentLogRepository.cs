/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public interface IUnsentLogRepository: ITerminal, IPioneer
    {
        void Enqueue(DataWrapper unsentData);
        void EnqueueAll(IEnumerable<DataWrapper> unsentData);
        
        bool Remove(DataWrapper unsentData);
        bool RemoveAll(IEnumerable<DataWrapper> unsentData);
        List<DataWrapper> DrainAll();
        List<DataWrapper> Drain(int size);
        List<DataWrapper> PeekAll();
    }
}