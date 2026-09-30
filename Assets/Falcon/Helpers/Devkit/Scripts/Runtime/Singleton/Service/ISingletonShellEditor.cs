/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonShellEditor : ISingletonLogic
    {
        void EditSingletonsShells(ISet<ISingletonShell> set);
    }

    public interface ISingletonShellSourceDisabler : ISingletonShellEditor
    {
        IEnumerable<Type> DisablingSources { get; }

        void ISingletonShellEditor.EditSingletonsShells(ISet<ISingletonShell> set)
        {
            foreach (var disablingType in DisablingSources)
                foreach (var singletonShell in set.Where(shell => shell.SourceType == disablingType).ToList())
                {
                    set.Remove(singletonShell);
                    SingletonLogger.Instance.Info($"Disabling shell {disablingType} defined in {disablingType}");
                }
        }
    }

    public class NoAutoCreateMySingletonImplementShellDisabler : ISingletonShellEditor
    {
        public int Priority => -1;

        public void EditSingletonsShells(ISet<ISingletonShell> set)
        {
            // ToList() trước khi Remove: LINQ ở đây là lazy trên chính set, xoá giữa lúc duyệt
            // sẽ ném InvalidOperationException (đúng cách ISingletonShellSourceDisabler đang làm).
            foreach (var monoSingletonShell in set.OfType<MonoSingletonShell>().Where(shell =>
                         shell.SourceType.GetCustomAttribute<NoAutoCreateAttribute>() != null).ToList())
                set.Remove(monoSingletonShell);

            foreach (var simpleSingletonShell in set.OfType<ConstructorSingletonShell>()
                         .Where(shell => shell.SourceType.GetCustomAttribute<NoAutoCreateAttribute>() != null)
                         .ToList())
                set.Remove(simpleSingletonShell);
        }
    }
}