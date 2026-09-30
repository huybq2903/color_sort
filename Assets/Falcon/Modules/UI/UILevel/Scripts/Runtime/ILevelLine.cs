/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

namespace Falcon.Modules.UI.Level.Runtime
{
    public interface ILevelLine
    {
        /// <summary>
        /// Trả về số lượng level muốn tạo trên dây
        /// </summary>
        /// <returns></returns>
        int GetNumberOfLevels();
        
        /// <summary>
        /// Trả về level hiện tại
        /// </summary>
        /// <returns></returns>
        int GetCurrentLevel();
        
        /// <summary>
        /// Trả về win streak nếu có. Nếu không có thì trả về 0
        /// </summary>
        /// <returns></returns>
        int GetWinStreak();
        
        /// <summary>
        /// Trả về độ khó theo level
        /// </summary>
        /// <param name="level">Level cần lấy độ khó</param>
        /// <returns></returns>
        int GetDifficultyAction(int level);
        
        /// <summary>
        /// Kiểm tra xem level có reward hay không. Với game không làm reward cho level thì return false
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        bool HasRewardsAction(int level);
        
        /// <summary>
        /// Sự kiện khi click vào 1 level node. Có thể để trống nếu không cho click vào level node
        /// </summary>
        /// <param name="model"></param>
        void OnClickLevelNode(LevelNodeModel model);

        /// <summary>
        /// Sự kiện khi click button play
        /// </summary>
        void OnClickBtnPlay();
    }
}