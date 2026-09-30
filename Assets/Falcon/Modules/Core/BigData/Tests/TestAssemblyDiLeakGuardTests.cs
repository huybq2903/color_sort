using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.Devkit;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Tripwire cho cái mìn DI-leak đã ghi sổ: FReflection quét CẢ test asmdef khi editor Play
    /// game thật — một class test lỡ kế thừa IMySingleton (trực tiếp, qua interface như IDataPool,
    /// hay qua subclass service thật) mà thiếu [NoAutoCreate] là nó chui vào container DI của game
    /// thay bản thật, lỗi chỉ nổ lúc Play chứ suite không thấy. Luật trước đây chỉ nằm trong
    /// comment/memory — test này biến nó thành máy: quét MỌI assembly test đang nạp, khỏi phải
    /// rà tay mỗi lần thêm fake/mock mới.
    /// </summary>
    public class TestAssemblyDiLeakGuardTests
    {
        [Test]
        public void MySingletonImplementorsInTestAssemblies_AreAllMarkedNoAutoCreate()
        {
            var offenders = new List<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                // Chỉ soi asmdef test của Falcon — third-party không biết IMySingleton là gì,
                // còn UnityEngine.TestRunner các kiểu quét cũng vô hại nhưng tốn công.
                if (!name.Contains("Tests") || !name.StartsWith("Falcon")) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                offenders.AddRange(types
                    .Where(t => typeof(IMySingleton).IsAssignableFrom(t))
                    .Where(t => !t.IsInterface && !t.IsAbstract)
                    // inherit:false — attribute phải nằm trên CHÍNH class test; thừa kế từ lớp cha
                    // không tính (lớp cha là service thật, đời nào có [NoAutoCreate])
                    .Where(t => !t.IsDefined(typeof(NoAutoCreateAttribute), inherit: false))
                    .Select(t => $"{name}: {t.FullName}"));
            }

            Assert.IsEmpty(offenders,
                "Class test kế thừa IMySingleton phải đánh [NoAutoCreate], không thì FReflection " +
                "nhét nó vào container DI của game lúc editor Play (mìn đã ghi sổ):\n" +
                string.Join("\n", offenders));
        }
    }
}
