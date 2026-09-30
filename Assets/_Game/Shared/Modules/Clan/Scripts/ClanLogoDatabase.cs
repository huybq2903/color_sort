using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Bảng tra sprite logo clan theo id, nạp từ Resources.</summary>
    [CreateAssetMenu(fileName = "SO_ClanLogoDatabase", menuName = "Falcon/Clan/Logo Database")]
    public class ClanLogoDatabase : ScriptableObject
    {
        [SerializeField] private Sprite[] _logos;

        private static ClanLogoDatabase _instance;

        public static ClanLogoDatabase Instance =>
            _instance ??= Resources.Load<ClanLogoDatabase>("SO_ClanLogoDatabase");

        public int Count => _logos?.Length ?? 0;

        /// <summary>id ngoài khoảng thì trả null, gọi bên ngoài tự lo.</summary>
        public Sprite GetById(int id) =>
            _logos != null && id >= 0 && id < _logos.Length ? _logos[id] : null;
    }
}
