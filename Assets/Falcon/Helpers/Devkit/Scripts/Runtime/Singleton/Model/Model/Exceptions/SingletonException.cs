/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [Serializable]
    public class SingletonException : Exception
    {
        public SingletonException(string message) : base(message)
        {
        }

        protected SingletonException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SingletonException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}