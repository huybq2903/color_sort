// Polyfill cho C# 9 `init` accessor. Unity (target .NET Standard 2.1) chưa ship
// System.Runtime.CompilerServices.IsExternalInit → CS0518 khi compile property `{ get; init; }`.
// Khai báo `internal` để chỉ scope trong asmdef này, tránh duplicate-type khi asmdef khác cũng polyfill.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
