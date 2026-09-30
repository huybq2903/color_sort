# Module CDN

## Tổng quan
- **Tên:** `CDN`
- **Công dụng**: Module **CDN** cung cấp cách quản lý và tải file từ CDN về game/app Unity.  
  Nó hỗ trợ preload, caching, và đọc dữ liệu dưới nhiều dạng (text, JSON, texture, sprite, audio, asset bundle).
## Quick Start

1. **Tạo và cấu hình `SOCdnSettings`**

   Vào menu:  
```

Falcon / Modules / Cdn / Settings

````
để tạo và chỉnh `SOCdnSettings`.

- **cdnKey**: khóa được tạo từ tool CMS, dùng để cấp quyền truy cập CDN cho ứng dụng.  
- **syncAllFilesAutomatically**:  
  - Bật (true): tất cả file sẽ được đồng bộ tự động với server.  
  - Tắt (false): bạn cần chủ động gọi các hàm `Prepare...` để đồng bộ thủ công.  
- **retryAttempts**: số lần thử lại khi request từ server thất bại.

2. **Nếu `syncAllFilesAutomatically = true`**

Bạn chỉ cần gọi:

```csharp
var file = await CdnController.Instance.LoadFile("example.png");
var sprite = await file.GetSprite();
````

⚠️ Nên bọc lệnh load trong `try/catch`, vì nếu file chưa từng load trong các phiên chơi trước và server không trả về được thì sẽ throw exception.

3. **Nếu `syncAllFilesAutomatically = false`**

   Bạn cần tự khởi tạo dữ liệu trước bằng các hàm `Prepare...`:

   ```csharp
   await CdnController.Instance.PrepareAll();
   // hoặc
   await CdnController.Instance.PrepareFolderExact("Textures");
   ```

   Sau đó mới có thể gọi `LoadFile(...)`.

---

## Sử dụng

### Load một file

```csharp
// Load file PNG thành sprite
var cdnFile = await CdnController.Instance.LoadFile("character.png", "Textures");
var sprite = await cdnFile.GetSprite();
```

### Đọc dữ liệu dạng text / JSON

```csharp
var jsonFile = await CdnController.Instance.LoadFile("config.json", "Configs");
var config = await jsonFile.GetJson<GameConfig>();
```

### Load AudioClip

```csharp
var audioFile = await CdnController.Instance.LoadFile("bgm.mp3", "Audio");
var clip = await audioFile.GetAudioClip(AudioType.MPEG);
```

### Load AssetBundle và lấy asset

```csharp
var bundleFile = await CdnController.Instance.LoadFile("ui_bundle", "Bundles");
var buttonPrefab = await bundleFile.GetAssetFromBundle<GameObject>("UIButton");
```

---

## Sự kiện khởi tạo

Bạn có thể lắng nghe khi service CDN hoàn tất init:

```csharp
CdnController.Instance.OnInitComplete += () =>
{
    Debug.Log("CDN đã khởi tạo xong!");
};
```

---

## Tóm tắt API chính

* **CdnController**

  * `LoadFile(...)` → tải file từ CDN.
  * `PrepareAll()` → preload toàn bộ file.
  * `PrepareFolderExact(...)` → preload folder chính xác.
  * `PrepareFolderPrefix(...)` → preload folder theo prefix.
  * `OnInitComplete` → event khi init hoàn tất.

* **CdnFile**

  * `GetStream()` / `GetBytes()` / `GetString()` / `GetJson<T>()`
  * `GetTexture()` / `GetSprite()`
  * `GetAudioClip()`
  * `GetAssetBundle()` / `GetAssetFromBundle<T>()`

---

## Ghi chú

* Nên luôn xử lý exception khi gọi `LoadFile`.
* Các API là **async** (`Task`), cần `await`.
* Module này phụ thuộc Unity (`Texture2D`, `Sprite`, `AudioClip`, `AssetBundle`).

