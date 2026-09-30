/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

namespace Falcon.Shared.Audio
{
    // Tuỳ chọn cho một lượt phát: null = giữ mặc định. Truyền vào thay vì trả AudioSource ra ngoài,
    // nên pool chỉ phải reset đúng những property khai báo ở đây.
    // AudioManager.PlaySFX("Click", new SfxOptions { Pitch = 1.2f });
    public struct SfxOptions
    {
        public float? Volume;
        public float? Pitch;
        public float? Pan;
    }
}
