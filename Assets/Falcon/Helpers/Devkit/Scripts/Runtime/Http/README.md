# Http Module for Unity

---

**Author**: leehuyyhoangg
**Email**: hoanglh@falcongames.com
**Company**: Falcon Games
**Date**: 2025-06-12

---

## 🚀 Overview

Module **Http** cung cấp một bộ công cụ mạnh mẽ và tiện lợi để thực hiện các yêu cầu HTTP trong dự án Unity của bạn. Nó được xây dựng trên `System.Net.Http.HttpClient`, đơn giản hóa việc gửi request, quản lý body, headers, tham số, và xử lý response một cách hiệu quả, đặc biệt hữu ích cho việc tương tác với các API dịch vụ web.

---

## 🎯 Core Concepts

Module này xoay quanh các khái niệm chính để định nghĩa và thực hiện một yêu cầu HTTP hoàn chỉnh:

### 1. `BodyType` (Enum)

Enum này định nghĩa các loại nội dung phổ biến cho phần thân (body) của yêu cầu HTTP. Mỗi loại có một **Media Type Name** tương ứng để thiết lập đúng `Content-Type` header:

* `Form`: `multipart/form-data`
* `XWwwForm`: `application/x-www-form-urlencoded`
* `Text`: `text/plain`
* `Json`: `application/json`
* `JavaScript`: `application/javascript`
* `Html`: `text/html`
* `XML`: `text/xml`
* `Binary`: `application/x-msdownload`

### 2. `Series` (Enum) và `HttpStatusExtensions`

`Series` phân loại các mã trạng thái HTTP (HTTP Status Code) thành các nhóm chính (1xx, 2xx, 3xx, 4xx, 5xx), giúp việc kiểm tra phản hồi trở nên dễ dàng hơn.

* **Informational (1xx)**
* **Successful (2xx)**
* **Redirection (3xx)**
* **ClientError (4xx)**
* **ServerError (5xx)**

`HttpStatusExtensions` cung cấp các phương thức mở rộng tiện lợi cho `HttpStatusCode` để kiểm tra nhanh nhóm trạng thái, ví dụ: `Is2xxSuccessful()`, `Is4xxClientError()`, `IsError()`.

---

## 📩 Xây dựng Yêu cầu (Request Building)

Module cung cấp một cấu trúc linh hoạt để xây dựng các yêu cầu HTTP:

### `IHttpBody` & `SimpleBody`

* **`IHttpBody`**: Interface định nghĩa cách nội dung body sẽ được chuyển đổi thành `HttpContent` để gửi đi trong request.
* **`SimpleBody`**: Triển khai cơ bản của `IHttpBody`, cho phép bạn dễ dàng tạo body dưới dạng **chuỗi văn bản** hoặc **JSON** (tự động serialize đối tượng).
    ```csharp
    // Tạo body JSON từ một đối tượng
    SimpleBody jsonBody = SimpleBody.Json(new { name = "Example", value = 123 });

    // Tạo body văn bản thuần túy
    SimpleBody textBody = SimpleBody.Plain("Hello, world!");
    ```

### `HttpRequest` (Abstract Class)

Đây là lớp cơ sở cho tất cả các yêu cầu HTTP. Nó trừu tượng hóa các chi tiết phổ biến và cung cấp các phương thức tiện ích:

* **`URL`**: Địa chỉ URL của yêu cầu.
* **`Headers`**: Một Dictionary để lưu trữ các HTTP headers.
* **`Params`**: Một Dictionary để lưu trữ các tham số query string (sẽ được thêm vào URL).
* **`Timeout`**: Thời gian chờ mặc định cho yêu cầu (mặc định 100 giây).
* **`Body`**: Đối tượng `IHttpBody` chứa nội dung của yêu cầu. Mặc định là `SimpleBody.Plain("")`.
* **Phương thức tiện ích**:
    * `AddParam()`, `AddParams()`: Thêm tham số vào URL.
    * `AddHeader()`, `AddHeaders()`: Thêm HTTP headers.
    * `AddAuthorization()`: Thêm header Authorization.
    * `SetBody()`, `SetJsonBody()`: Thiết lập nội dung body của request.
* **`HttpClient`**: Sử dụng một `HttpClient` tĩnh được cấu hình với `Connection: keep-alive` để tái sử dụng kết nối, cải thiện hiệu suất.
* **`Execute()`**: Phương thức chính bất đồng bộ để gửi yêu cầu. Nó hỗ trợ `CancellationToken` và tích hợp `Timeout` để kiểm soát tốt hơn.

### Các loại `HttpRequest` cụ thể

* **`GeneralHttpRequest`**:
    * Lớp triển khai `HttpRequest` tổng quát nhất.
    * Cho phép bạn chỉ định `HttpMethod` (GET, POST, PUT, DELETE, v.v.) trong constructor.
    * Tự động thêm tham số vào URL và nội dung body (trừ khi là GET request).

* **`PostRequest`**:
    * Lớp triển khai `HttpRequest` chuyên biệt cho yêu cầu **POST**.
    * Tự động thiết lập `HttpMethod` là `POST`.
    * Luôn đính kèm nội dung từ `Body.ToContent()` vào yêu cầu.

---

## ↩️ Xử lý Phản hồi (Response Handling)

Module cung cấp lớp `HttpResponse` để xử lý kết quả từ yêu cầu HTTP:

### `HttpResponse`

Lớp này bao bọc `HttpResponseMessage` từ `System.Net.Http` và cung cấp các phương thức dễ sử dụng để truy cập dữ liệu và trạng thái phản hồi:

* **`StatusCode`**: Mã trạng thái HTTP dưới dạng số nguyên.
* **`Status`**: Mã trạng thái HTTP dưới dạng enum `HttpStatusCode`.
* **`IsSuccess`**: `true` nếu mã trạng thái là 2xx (thành công).
* **Đọc Body**:
    * `StrBody()`: Đọc nội dung body dưới dạng chuỗi.
    * `BytesBody()`: Đọc nội dung body dưới dạng mảng byte.
    * `StreamBody()`: Đọc nội dung body dưới dạng `Stream`.
    * **Lưu ý quan trọng**: Tất cả các phương thức đọc body này đều gọi `Close()` trong khối `finally` để đảm bảo tài nguyên underlying `HttpResponseMessage` được giải phóng ngay sau khi nội dung được đọc, tránh rò rỉ bộ nhớ.
* **Phương thức "Success"**:
    * `SuccessStrBody()`, `SuccessBytesBody()`, `SuccessStreamBody()`: Đọc body chỉ khi yêu cầu thành công.
    * `SuccessObj<T>()`: Đọc body dưới dạng chuỗi, sau đó chuyển đổi (deserialize) thành đối tượng `T` (thường dùng cho JSON).
* **`EnsureSuccess()`**: Phương thức nội bộ được gọi bởi các phương thức "Success" để kiểm tra xem phản hồi có thành công hay không. Nếu không, nó sẽ ném `InvalidOperationException` kèm theo mã trạng thái và nội dung body lỗi (nếu có) để hỗ trợ debug.
* **`Close()`**: Giải phóng tài nguyên của `HttpResponseMessage` underlying.

---

## 🚀 Cách sử dụng (Ví dụ)

Dưới đây là một số ví dụ minh họa cách sử dụng module Http:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine; // Giả sử sử dụng trong Unity
using Falcon.Helpers.Devkit.Http.Scripts.Runtime.Requests;
using Falcon.Helpers.Devkit.Http.Scripts.Runtime.Requests.Bodies;

public class HttpExample : MonoBehaviour
{
    private async void Start()
    {
        // Ví dụ 1: Gửi GET Request đơn giản
        await GetExample();

        // Ví dụ 2: Gửi POST Request với JSON Body
        await PostJsonExample();

        // Ví dụ 3: Gửi GET Request với Params và Headers
        await GetWithParamsAndHeadersExample();

        // Ví dụ 4: Xử lý phản hồi lỗi
        await ErrorHandlingExample();
    }

    private async Task GetExample()
    {
        Debug.Log("--- GET Example ---");
        var request = new GeneralHttpRequest("https://jsonplaceholder.typicode.com/todos/1", HttpMethod.Get);

        try
        {
            var response = await request.Execute();
            if (response.IsSuccess)
            {
                string body = await response.StrBody();
                Debug.Log($"GET Success! Status: {response.Status}\nBody: {body}");
            }
            else
            {
                string errorBody = await response.StrBody();
                Debug.LogError($"GET Failed! Status: {response.Status}\nError Body: {errorBody}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GET Exception: {e.Message}");
        }
    }

    private async Task PostJsonExample()
    {
        Debug.Log("--- POST JSON Example ---");
        var request = new PostRequest("https://jsonplaceholder.typicode.com/posts");

        var postData = new {
            title = "foo",
            body = "bar",
            userId = 1
        };
        request.SetJsonBody(postData);

        try
        {
            var response = await request.Execute();
            // Sử dụng SuccessObj để tự động kiểm tra thành công và deserialize
            var result = await response.SuccessObj<Dictionary<string, object>>();
            Debug.Log($"POST JSON Success! Status: {response.Status}\nCreated Post ID: {result["id"]}");
        }
        catch (InvalidOperationException ioe) // Bắt lỗi từ EnsureSuccess()
        {
            Debug.LogError($"POST JSON Failed (Status Error): {ioe.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"POST JSON Exception: {e.Message}");
        }
    }

    private async Task GetWithParamsAndHeadersExample()
    {
        Debug.Log("--- GET with Params & Headers Example ---");
        var request = new GeneralHttpRequest("https://httpbin.org/get", HttpMethod.Get);
        
        request.AddParam("param1", "value1")
               .AddParam("param2", "value2");

        request.AddHeader("X-Custom-Header", "MyValue");
        request.AddAuthorization("Bearer your_token_here"); // Ví dụ thêm token

        try
        {
            var response = await request.Execute();
            string body = await response.SuccessStrBody(); // Chỉ đọc nếu thành công
            Debug.Log($"GET With Params & Headers Success! Status: {response.Status}\nBody: {body}");
        }
        catch (Exception e)
        {
LogError($"GET With Params & Headers Exception: {e.Message}");
        }
    }

    private async Task ErrorHandlingExample()
    {
        Debug.Log("--- Error Handling Example (404 Not Found) ---");
        var request = new GeneralHttpRequest("https://jsonplaceholder.typicode.com/nonexistent-path", HttpMethod.Get);

        try
        {
            var response = await request.Execute();
            // Nếu không dùng SuccessStrBody, cần kiểm tra IsSuccess
            if (!response.IsSuccess)
            {
                string errorBody = await response.StrBody();
                Debug.LogError($"Expected Error Handled! Status: {response.Status}\nError Body: {errorBody}");
            }
            else
            {
                Debug.LogWarning("Unexpected success for error path!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected Exception during error handling: {e.Message}");
        }
    }
}