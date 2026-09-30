using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Sổ tên cột hợp đồng — cửa chặn game đặt key trùng qua Common.Set / Label.* (§H1).
    /// Ca sinh ra nó: game xây feature streak rồi đặt "win_streak", trùng cột thống kê của SDK.
    /// </summary>
    public class FWireReservedKeysTests
    {
        [Test]
        public void PopularColumnNames_AreReserved_InAnySpelling()
        {
            // So theo dạng chuẩn hoá: snake/camel/CONST đều phải bắt được
            Assert.IsTrue(FWireReservedKeys.IsReserved("winStreak"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("win_streak"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("WIN_STREAK"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("loseStreak"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("play_turn_id"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("currentLevel"));
            Assert.IsTrue(FWireReservedKeys.IsReserved("score"));
        }

        [Test]
        public void GameOwnNames_PassFreely()
        {
            Assert.IsFalse(FWireReservedKeys.IsReserved("streak_status"), "Tên được gợi ý thay thế phải đi lọt");
            Assert.IsFalse(FWireReservedKeys.IsReserved("guild_id"));
            Assert.IsFalse(FWireReservedKeys.IsReserved("vip_tier"));
        }

        [Test]
        public void SdkOwnLabelKeys_AreNotSelfBlocked()
        {
            // SDK tự gửi nhãn elo (SetUserElo) và clicked (bản tin bổ sung offer) — hai key này
            // không được nằm trong sổ, không thì SDK tự bắn vào chân
            Assert.IsFalse(FWireReservedKeys.IsReserved(FUserLabelKey.ELO));
            Assert.IsFalse(FWireReservedKeys.IsReserved("clicked"));
        }

        [Test]
        public void BlankKey_IsNotReserved()
        {
            Assert.IsFalse(FWireReservedKeys.IsReserved(null));
            Assert.IsFalse(FWireReservedKeys.IsReserved(string.Empty));
        }
    }
}
