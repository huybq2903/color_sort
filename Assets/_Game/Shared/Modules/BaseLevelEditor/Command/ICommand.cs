/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-19
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.BaseLevelEditor
{
    public interface ICommand
    {
        // true = thực hiện thành công (ghi vào undo history); false = bị từ chối (không ghi).
        bool Execute();
        void Undo();
    }
}