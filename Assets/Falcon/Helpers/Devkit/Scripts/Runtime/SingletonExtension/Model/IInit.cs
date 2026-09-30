/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IInit : IMySingleton
    {
        Task Init(CancellationToken cancellationToken = default);
    }
}