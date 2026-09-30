/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IThreadPool
    {
        int Size { get; set; }

        void Add(IMyAction action);

        bool Remove(IMyAction action);
    }
}