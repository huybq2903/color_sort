using System.IO;
using Falcon.InGame.Core;
using UnityEditor;
using UnityEngine;

/// <summary>Bake hai texture kính (seed cố định) thành PNG trong Resources để PictureView nạp thay vì tính lại mỗi phiên.</summary>
public static class BakeGlassTextures
{
    private const string Dir = "Assets/_Game/InGame/Core/Picture/Resources/PictureGlass";

    [MenuItem("Falcon/Picture/Bake Glass Textures")]
    public static void Bake()
    {
        Directory.CreateDirectory(Dir);
        BakeOne(GlassTexture.Create(), "glass");
        BakeOne(GlassTexture.CreateFacets(), "facets");
        AssetDatabase.Refresh();
        foreach (var n in new[] { "glass", "facets" }) ApplyImportSettings($"{Dir}/{n}.png");
    }

    private static void BakeOne(Texture2D tex, string name)
    {
        var png = tex.EncodeToPNG();
        File.WriteAllBytes($"{Dir}/{name}.png", png);

        var back = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        back.LoadImage(png);
        var a = tex.GetPixels32();
        var b = back.GetPixels32();
        var same = a.Length == b.Length;
        for (var i = 0; same && i < a.Length; i++)
            same = a[i].r == b[i].r && a[i].g == b[i].g && a[i].b == b[i].b && a[i].a == b[i].a;
        Debug.Log($"BAKE {name} {tex.width}x{tex.height} png={png.Length}B identical={same}");
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(back);
    }

    private static void ApplyImportSettings(string path)
    {
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
        ti.textureType = TextureImporterType.Default;
        ti.sRGBTexture = true; // giống Texture2D tạo bằng code (không linear)
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = false;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.isReadable = false;
        ti.SaveAndReimport();
    }
}
