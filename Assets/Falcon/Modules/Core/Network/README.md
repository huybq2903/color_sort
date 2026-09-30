# Module Falcon Network
## Tổng quan
- **Tên:** `Falcon Network`
- **Giới thiệu:** Kết nối và truyền nhận bản tin giữa Client và Server thông qua WebSocket hoặc UDP. Hỗ trợ tự động kết nối lại khi mất kết nối, nén bản tin, gửi bản tin có kích thước lớn, v.v.

## Quick Start
1. ### Cấu hình
Trên thanh Menu của Unity, chọn Falcon -> Modules -> Network Settings, điền các thông số Server Ip, Context, Port tương ứng
2. ### Khởi tạo Network
Trong code của bạn, gọi hàm khởi tạo Network như sau:
```csharp
var serverIp = NetworkSettings.GetServerIp();
var serverPort = NetworkSettings.GetServerPort();
var serverContext = NetworkSettings.GetServerContext();
FNetManager.Instance.Start($"http://{serverIp}:{serverPort}/{serverContext}/");
```
3. ### Lắng nghe các sự kiện kết nối 
- Kiểm tra có kết nối hay không bằng biến `FNetManager.Instance.Connected` (true là có kết nối, false là không có kết nối)
- Tạo một Class để kế thừa ISessionListener, sẽ tự động gọi vào các hàm, ví dụ:
```csharp
    public class ConnectionListener: ISessionListener
    {
        public void OnSessionReset()
        {
            //To-do 
        }
        public void OnFirstSession()
        {
            //To-do 
        }
        
        public void OnChannelDisconnected(FChannel channel)
        {
            //To-do
        }
    }
 ```
4. ### Gửi bản tin lên Server
```csharp
    [FAMessage("cs_chat")]
    public class CSChat : CSMessage
    {
        public string text;

        public CSChat() { }

        public CSChat(string text)
        {
            this.text = text;
        }
    }
    
    new CSChat("Hello").Send();
```

5. ### Nhận bản tin từ Server trả về

Kế thừa class SCMessage, rồi xử lý trong hàm OnData, lưu ý các thuộc tính phải để public và có constructor không tham số, Ví dụ:
```csharp
    [FAMessage("sc_chat")]
    public class SCChat : SCMessage
    {
        public string text; // text

        public SCChat()
        {
        }

        public SCChat(string text)
        {
            this.text = text;
        }

        public override void OnData()
        {
            //To-do here 
        }
    }
```

6. ### Gửi CS và đợi SC trả về
Thêm AddSCListener trước khi gọi hàm Send của CS. Ví dụ
```
new CSChat("Hello").AddSCListener<SCChat>(((message, timeout, success) =>
 {
    if (timeout)
       //To-do timeout 
    else
       //To-do success 
}), 5).Send();
//5 trong ví dụ là thời gian timeout, tức là sau 5s nếu không có bản tin SC trả về thì timeout = true, có thể đặt số khác 

new CSChat("Hello").AddSCListenerExt<SCChat>(((csMessage, scMessage, timeout, success) =>
 {
    if (timeout)
       //To-do timeout 
    else
       //To-do success 
}), 5).Send();
//5 trong ví dụ là thời gian timeout, tức là sau 5s nếu không có bản tin SC trả về thì timeout = true, có thể đặt số khác 
//csMessage là bản tin CS gốc gửi đi
//scMessage là bản tin SC trả về tương ứng với CS gốc gửi đi

```

7. ### Gửi bản tin UDP lên Server
Vẫn kế thừa CSMessage như bình thường, nhưng khi gọi hàm Send thì truyền tham số TransportType.UDP vào, ví dụ:
```csharp
new CSChat("Hello").Send(TransportType.UDP);
```

8. ### Nén bản tin và gửi
Với bản tin có kích thước lớn, có thể nén nội dung và gửi bằng cách đơn giản sau, ví dụ**

```
new CSChat("Hello").Compress().Send();
```

9. ### Tối ưu kích thước bản tin CS 
Việc gửi bản tin CS mặc định sẽ chuyển object thành json để gửi đi, nhưng việc chuyển đổi thành json tốn CPU và tốn 
dung lượng băng thông, do đó, từ phiên bản mới, moudle Network hỗ trợ việc tự mã hoá bản tin CS thành binary rồi gửi đi.
Ưu điểm của việc tự mã hoá thành binary là nó rất nhanh, nhẹ, phù hợp với các ứng dụng đòi hỏi realtime, tối ưu performance.

```csharp
    [FAMessage("c_test")]
    public class CSTest : CSMessage, ICSBinary
    {
        public string name;
        public int age;
        public float score;

        public CSTest()
        {
            name = "test";
            age = 10;
            score = 100.5f;
        }

        public byte[] ToBytes()
        {
            FBinaryWriter writer = initWriter(1024);
            
            writer.WriteString(name);
            writer.WriteInt(age);
            writer.WriteFloat(score);

            return writer.ToArray();
        }
    }
    
    //Cách dùng, khi này bản tin CSTest sẽ được gửi đi bằng binary rất nhẹ
    new CSTest().Send();
    
```

```csharp
    [FAMessage("s_test")]
    public class SCTest : SCMessage, ISCBinary
    {
        public string name;
        public int age;
        public float score;
        
        public override void OnData()
        {
            Debug.Log("YYY: " + name);
        }

        public void FromBytes(byte[] bytes)
        {
            FBinaryReader reader = initReader(bytes);
            
            name = reader.ReadString();
            age = reader.ReadInt();
            score = reader.ReadFloat();
        }
    }
```
10. ### Lắng nghe sự kiện bản tin SC trả về từ Server 
Ngoài việc kế thừa SCMessage và xử lý trong hàm OnData, bạn có thể lắng nghe sự kiện bản tin SC trả về từ Server bằng cách đăng ký listener, ví dụ:
```csharp
    public class SCLoginListener : SCMessageListener<SCLogin>
    {
        public override void OnMessage(SCLogin message)
        {
            Debug.Log("SCLoginListener");
        }
    }
```

## Lưu ý
1. Các bản tin phải có chữ cái đầu là CS, SC viết hoa
2. Các thuộc tính của CS, SC phải để public 
3. Không nên khai báo constructor trong SC


## Lỗi thường gặp
- Cấu hình Network không khớp với cấu hình trên Server, ví dụ: sai IP, sai Context, sai Port.
- Các thuộc tính của CS hoặc SC không được để public
- Class SC KHÔNG có constructor không tham số, mà lại có constructor có tham số
- Gửi bản tin UDP mà phía Server chưa cấu hình hỗ trợ UDP