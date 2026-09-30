# Encrypts Module for Unity

---

**Author**: leehuyyhoangg
**Email**: hoanglh@falcongames.com
**Company**: Falcon Games
**Date**: 2025-06-12

---

## 🚀 Overview

Module **Encrypts** cung cấp một khung sườn linh hoạt để thực hiện các thao tác mã hóa và giải mã trong dự án Unity của bạn. Nó cho phép bạn xử lý dữ liệu dưới dạng các khối mã hóa, cung cấp một interface chung cho các thuật toán mã hóa khác nhau (hiện tại là AES), và tích hợp các phương thức tiện ích để quản lý khóa và chuyển đổi dữ liệu.

---

## 🎯 Core Concepts

Module này xoay quanh các khái niệm chính để thực hiện các tác vụ mã hóa/giải mã:

### 1. `ICryptographer` (Interface)

Đây là hợp đồng cốt lõi cho mọi công cụ mã hóa/giải mã trong module. Nó định nghĩa các phương thức để mã hóa và giải mã cả một khối dữ liệu (`CryptoBlock`) hoặc một tập hợp các khối dữ liệu (`CryptoBlocks`).

### 2. `CryptoBlock` (Class)

`CryptoBlock` là một đối tượng dữ liệu cơ bản, đại diện cho một khối dữ liệu (byte array) được sử dụng trong các thao tác mã hóa. Nó cung cấp sự linh hoạt trong việc khởi tạo và chuyển đổi dữ liệu:

* **Lưu trữ**: Chứa dữ liệu dưới dạng `byte[] Bytes`.
* **Khởi tạo linh hoạt**: Có thể tạo từ `byte[]` trực tiếp, từ chuỗi Base64 (`string shortStr`), hoặc từ chuỗi văn bản (`string shortStr`) với một `Encoding` cụ thể.
* **Chuyển đổi tiện lợi**: Cung cấp các phương thức `AsStr()` (chuyển đổi sang Base64 string) và `AsStr(Encoding encoding)` (chuyển đổi sang string với encoding cụ thể).

### 3. `CryptoBlocks` (Class)

`CryptoBlocks` là một lớp tiện ích kế thừa từ `List<CryptoBlock>`, được thiết kế để quản lý và thao tác với nhiều `CryptoBlock` cùng lúc. Điều này đặc biệt hữu ích khi bạn cần chia nhỏ dữ liệu lớn để xử lý hoặc khi làm việc với các hệ thống mã hóa theo khối:

* **Khởi tạo đa dạng**: Có thể tạo từ các `CryptoBlock` có sẵn, hoặc từ một chuỗi dài (`string longStr`) tự động chia thành các khối dựa trên kích thước (`chunkSize`) và `Encoding`.
* **Hợp nhất và chuyển đổi**: Cung cấp các phương thức để hợp nhất các khối thành một chuỗi duy nhất (`AsStr()`), một danh sách chuỗi (`AsStringList()`), hoặc một mảng byte tổng hợp (`AsBytes()`).

---

## 🔐 Dịch vụ Mã hóa (Cryptography Services)

Module cung cấp các triển khai cụ thể của `ICryptographer` và các dịch vụ nền tảng cho mã hóa:

### `NoCrypto`

* **Mục đích**: Là một triển khai của `ICryptographer` không thực hiện bất kỳ hoạt động mã hóa hay giải mã nào. Nó đơn giản trả về dữ liệu đầu vào nguyên vẹn.
* **Sử dụng**: Hữu ích cho mục đích phát triển, kiểm thử hoặc khi bạn muốn tạm thời vô hiệu hóa chức năng mã hóa mà không cần thay đổi cấu trúc mã nguồn. Nó là một **singleton** (`NoCrypto.Instance`).

### `AesParser`

* **Mục đích**: Một triển khai cụ thể của `ICryptographer` sử dụng thuật toán mã hóa **Advanced Encryption Standard (AES)**.
* **Khóa AES**: Được khởi tạo với một khóa AES (dưới dạng `byte[]` hoặc chuỗi Base64).
* **Hoạt động**: Sử dụng `AesService` để thực hiện các phép mã hóa và giải mã thực tế, xử lý cả `CryptoBlock` và `CryptoBlocks` bằng cách lặp qua từng khối.

### `AesService` (Static Class)

`AesService` là lớp tĩnh chứa logic lõi cho các hoạt động mã hóa/giải mã AES. Nó tuân thủ các thực tiễn bảo mật tốt nhất:

* **Tham số mặc định**: Sử dụng `CipherMode.CBC` (Cipher Block Chaining) và `PaddingMode.PKCS7` làm chế độ mặc định, là các lựa chọn phổ biến và an toàn.
* **Tạo khóa an toàn**:
    * `GenerateAesKey()`: Tạo một khóa AES mới ngẫu nhiên dưới dạng `byte[]`.
    * `GenerateAesKeyStr()`: Tạo khóa và chuyển đổi thành chuỗi Base64.
* **Mã hóa (`Encrypt`)**:
    * Nhận khóa AES và nội dung plaintext (`byte[]`).
    * **Tạo IV ngẫu nhiên**: Điều cực kỳ quan trọng là nó **tự động tạo một Initialization Vector (IV) mới, ngẫu nhiên cho MỖI LẦN mã hóa**. Việc này giúp ngăn chặn các cuộc tấn công tái sử dụng IV và tăng cường bảo mật.
    * Trả về một đối tượng `AesEncrypted` (chứa dữ liệu đã mã hóa và IV được sử dụng).
* **Giải mã (`Decrypt`)**:
    * Nhận khóa AES và đối tượng `AesEncrypted` làm đầu vào.
    * **Sử dụng IV chính xác**: Nó phải sử dụng chính IV đã được tạo và dùng trong quá trình mã hóa để giải mã thành công.
    * Bao gồm xử lý lỗi (`CryptographicException`) để thông báo khi quá trình giải mã thất bại (ví dụ: dữ liệu hỏng, khóa/IV không đúng).

---

## 🚀 Cách sử dụng (Ví dụ)

```csharp
using System;
using System.Text;
using UnityEngine;
using Falcon.Helpers.Devkit.Encrypts.Scripts.Runtime;
using Falcon.Helpers.Devkit.Encrypts.Scripts.Runtime.Requests;
using Falcon.Helpers.Devkit.Encrypts.Scripts.Runtime.Aes;

public class EncryptsExample : MonoBehaviour
{
    void Start()
    {
        // 1. Tạo một khóa AES mới
        string aesKeyStr = AesService.GenerateAesKeyStr();
        Debug.Log(<span class="math-inline">"Generated AES Key \(Base64\)\: \{aesKeyStr\}"\);
// 2\. Khởi tạo AesParser với khóa
ICryptographer aesCryptographer \= new AesParser\(aesKeyStr\);
string originalData \= "Đây là một chuỗi dữ liệu rất nhạy cảm cần được mã hóa\!";
Debug\.Log\(</span>"Original Data: {originalData}");

        // 3. Chuẩn bị CryptoBlock từ dữ liệu gốc
        CryptoBlock originalBlock = new CryptoBlock(originalData, Encoding.UTF8);

        // 4. Mã hóa CryptoBlock
        CryptoBlock encryptedBlock = aesCryptographer.Encrypt(originalBlock);
        string encryptedStr = encryptedBlock.AsStr(); // Lấy dạng Base64 của dữ liệu đã mã hóa
        Debug.Log(<span class="math-inline">"Encrypted Data \(Base64\)\: \{encryptedStr\}"\);
// 5\. Giải mã CryptoBlock
CryptoBlock decryptedBlock \= aesCryptographer\.Decrypt\(encryptedBlock\);
string decryptedData \= decryptedBlock\.AsStr\(Encoding\.UTF8\);
Debug\.Log\(</span>"Decrypted Data: {decryptedData}");

        // Kiểm tra xem dữ liệu có khớp không
        Debug.Log(<span class="math-inline">"Data integrity check\: \{originalData \=\= decryptedData\}"\);
Debug\.Log\("\\n\-\-\- Working with CryptoBlocks \(Chunking\) \-\-\-"\);
string longOriginalData \= "Dữ liệu này rất dài và cần được chia nhỏ thành nhiều khối để xử lý mã hóa hiệu quả hơn\. Module Encrypts hỗ trợ tự động chia nhỏ và hợp nhất các khối dữ liệu để đơn giản hóa quá trình này\.";
Debug\.Log\(</span>"Long Original Data: {longOriginalData}");

        // 6. Chia nhỏ dữ liệu dài thành CryptoBlocks
        // Ví dụ chia thành các khối 32 byte (kích thước khối AES là 16 byte, đây là kích thước chunk cho đầu vào)
        CryptoBlocks originalBlocks = new CryptoBlocks(longOriginalData, Encoding.UTF8, 32);
        Debug.Log(<span class="math-inline">"Original Blocks Count\: \{originalBlocks\.Count\}"\);
// 7\. Mã hóa CryptoBlocks
CryptoBlocks encryptedBlocks \= aesCryptographer\.Encrypt\(originalBlocks\);
Debug\.Log\(</span>"Encrypted Blocks Count: {encryptedBlocks.Count}");

        // 8. Giải mã CryptoBlocks
        CryptoBlocks decryptedBlocks = aesCryptographer.Decrypt(encryptedBlocks);
        Debug.Log(<span class="math-inline">"Decrypted Blocks Count\: \{decryptedBlocks\.Count\}"\);
// 9\. Hợp nhất các khối đã giải mã trở lại thành chuỗi ban đầu
string longDecryptedData \= decryptedBlocks\.AsStr\(Encoding\.UTF8\);
Debug\.Log\(</span>"Long Decrypted Data: {longDecryptedData}");

        // Kiểm tra tính toàn vẹn dữ liệu dài
        Debug.Log(<span class="math-inline">"Long Data integrity check\: \{longOriginalData \=\= longDecryptedData\}"\);
Debug\.Log\("\\n\-\-\- Using NoCrypto \(No encryption\) \-\-\-"\);
ICryptographer noCryptographer \= NoCrypto\.Instance;
CryptoBlock testBlock \= new CryptoBlock\("Dữ liệu không mã hóa", Encoding\.UTF8\);
CryptoBlock noEncrypted \= noCryptographer\.Encrypt\(testBlock\);
CryptoBlock noDecrypted \= noCryptographer\.Decrypt\(noEncrypted\);
Debug\.Log\(</span>"NoCrypto Original: {testBlock.AsStr(Encoding.UTF8)}");
        Debug.Log(<span class="math-inline">"NoCrypto Encrypted\: \{noEncrypted\.AsStr\(Encoding\.UTF8\)\}"\);
Debug\.Log\(</span>"NoCrypto Decrypted: {noDecrypted.AsStr(Encoding.UTF8)}");
        Debug.Log($"NoCrypto integrity check: {testBlock.AsStr(Encoding.UTF8) == noDecrypted.AsStr(Encoding.UTF8)}");
    }
}