- Module này để kiểm tra xem các module khác đã theo chuẩn quy định chưa

- Ngoài ra còn vẽ đồ thị phụ thuộc của các module với nhau, phân lớp theo chiều dọc
- Nếu muốn tạo một Verifier riêng, cần kế thừa IModuleVerify và thiết lập Attribute, ví dụ:
```csharp
    [FAModuleVerify("Verify CS, SC")]
    public class CSSCVerify : IModuleVerify
    {
        public bool VerifyModules(FModule module)
        {
            // Kiểm tra module CS, SC
            return true;
        }
    }
```
