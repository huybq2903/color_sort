using System;
using System.Collections;
using Falcon.Helpers.Singleton;
using UnityEngine;

public class PhoneUtil : PersistentSingleton<PhoneUtil>
{
    /// <summary>
    /// Chụp màn hình và trả về ảnh PNG (byte[]) qua callback.
    /// </summary>
    /// <param name="onCaptured">Callback nhận byte[] ảnh PNG</param>
    public void CaptureScreenshotAsync(Action<byte[]> onCaptured)
    {
        StartCoroutine(CaptureCoroutine(onCaptured));
    }

    private IEnumerator CaptureCoroutine(Action<byte[]> onCaptured)
    {
        // Chờ đến cuối frame để đảm bảo render xong
        yield return new WaitForEndOfFrame();

        // Tạo texture từ màn hình
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Đọc pixel từ màn hình
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Chuyển sang byte[] PNG
        byte[] imageBytes = tex.EncodeToPNG();

        // Dọn dẹp
        Destroy(tex);

        // Gọi callback
        onCaptured?.Invoke(imageBytes);
    }
}
