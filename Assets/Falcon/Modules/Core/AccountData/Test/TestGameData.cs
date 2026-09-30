/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-14


using System.Collections;
using BayatGames.SaveGamePro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Falcon.Modules.Core.AccountData
{
    [FGameDataType("game_data_1", true)] //có thể đặt tên bất kỳ
    public class GameData1 : FGameData<GameData1>
    {
        public int int1 { get; set; }
        public string string1 { get; set; }
        
        public override void OnUpdateFromServer()
        {
            Debug.Log("GameData1 updated from server");
            // Thực hiện các hành động cần thiết khi dữ liệu được cập nhật từ server
        }
    }
    
    public class TestGameData
    {
#if UNITY_EDITOR
        [UnityTest]
#endif
        public IEnumerator SaveGameData()
        {

            AccountManager.Instance.OnLoginEvent += (success) =>
            {         
                GameData1.Instance.int1 = 10000;
                GameData1.Instance.string1 = "test10000";
                GameData1.Instance.Save();
                GameData1.Instance.UpdateToServer();
            };

            AccountManager.Instance.Init();

            yield return new WaitForSeconds(300);
            yield break;
        }
        
#if UNITY_EDITOR
        [UnityTest]
#endif
        public IEnumerator LoadGameData()
        {

            SaveGame.Clear();

            AccountManager.Instance.Init();

            yield return new WaitForSeconds(300);
            yield break;
        }
    }
}