# Falcon Addressable Helper

`Falcon Addressable Helper` là module wrapper cho **Unity Addressables**
giúp:

-   Load asset an toàn và đơn giản hơn
-   Hỗ trợ **preload** để giảm delay runtime
-   Quản lý **reference counting**
-   Tránh **duplicate load**
-   Retry khi load lỗi
-   Hỗ trợ **Prefab instantiation**

Module này cung cấp API cho cả:

-   Address key dạng **string** (cache key theo *address*)
-   **AssetReference** (cache key theo *GUID*)

> ⚠️ **Hai namespace cache TÁCH BIỆT (by-design).** Đừng load CÙNG một asset bằng
> cả hai API (sẽ tạo 2 entry + 2 bản resident). Mỗi asset hãy chọn một cách nhất quán.
> Không gộp được vì runtime Addressables thiếu canonicalize *address↔GUID* đồng bộ.

# Key Features

-   **Reference counting**\
    Asset chỉ được unload khi không còn ai sử dụng.

-   **Shared loading task**\
    Nếu nhiều nơi load cùng asset cùng lúc, chỉ có **1 request
    Addressables**.

-   **Retry mechanism**\
    Tự retry khi load fail.

-   **Prefab instantiation helper**\
    Load + Instantiate + GetComponent trong một API.

-   **Preload support**\
    Load asset trước khi cần dùng để giảm hitch.

-----------------------------------------------------------------------

# Basic Usage

## Load Asset

Dùng khi cần **asset gốc** (không instantiate).

``` csharp
var sprite = await AddressableHelper.Load<Sprite>("UI/IconCoin");
var prefab = await AddressableHelper.Load<GameObject>("UIPopup_Shop");
```

------------------------------------------------------------------------

## Preload Asset

Load asset trước khi cần dùng để giảm delay runtime.

``` csharp
AddressableHelper.PreloadPrefab("UIPopup_Shop");
AddressableHelper.PreloadScriptableObject<GameConfig>("Configs/GameConfig");
```

Preload **không trả asset về**.

------------------------------------------------------------------------

## Instantiate Prefab

Load prefab và tạo instance.

``` csharp
var popup = await AddressableHelper.LoadPrefabInstance<ShopPopupView>(
    "UIPopup_Shop",
    uiRoot
);

popup.Show();
```

API sẽ:

1.  Load prefab
2.  Instantiate object
3.  Trả về component `T`

------------------------------------------------------------------------

## Release Asset

Giảm reference count của asset.

``` csharp
AddressableHelper.Release("UIPopup_Shop");
```

Asset chỉ bị unload khi **refCount = 0**.

------------------------------------------------------------------------

# AssetReference Usage

Nếu asset được gán trong Inspector:

``` csharp
[SerializeField]
private AssetReference popupReference;
```

Load:

``` csharp
var popup = await AddressableHelper
    .LoadPrefabByReference<ShopPopupView>(popupReference, uiRoot);
```

Preload:

``` csharp
AddressableHelper.PreloadPrefabByReference(popupReference);
```

Release:

``` csharp
AddressableHelper.ReleaseByReference(popupReference);
```

## Release

``` csharp
void Release<T>(string path)
void Release(string path)

void ReleaseByReference<T>(AssetReference reference)
void ReleaseByReference(AssetReference reference)
```

Giảm reference count của asset.

------------------------------------------------------------------------

# Lifecycle Rules

## 1. Load tăng refCount

Mỗi lần `Load` thành công → `refCount++`.

------------------------------------------------------------------------

## 2. Release giảm refCount

Khi `refCount == 0` → asset được `Addressables.Release`.

------------------------------------------------------------------------

## 3. Instantiate ≠ Load

`LoadPrefab` sẽ:

-   Load prefab asset
-   Instantiate object

Nhưng:

-   `Release()` **không destroy instance**
-   `Destroy()` **không release asset**

Bạn thường cần cả hai:

``` csharp
Destroy(instance.gameObject);
AddressableHelper.Release("UIPopup_Shop");
```

------------------------------------------------------------------------
# Common Mistakes

  Popup mở chậm ->                      Chưa preload.

  Spawn object nhưng chỉ có 1 instance ->     `reuseCachedInstance = true`.

  Asset bị unload sớm ->                Release quá nhiều.

  Instance còn trong scene nhưng asset bị release ->     Destroy và Release không đúng thứ tự.

  Asset load 2 lần / không dedup ->     Load cùng asset bằng cả path lẫn AssetReference (2 namespace khác nhau).

------------------------------------------------------------------------

# Editor Utilities

Thêm asset vào Addressables group.

``` csharp
MakeAssetAddressable(Object asset, string groupName, bool simplifyName)
```

Kiểm tra asset đã nằm trong Addressables chưa.
``` csharp
IsInAddressables(Object asset)
```
