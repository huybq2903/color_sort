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
    public class RequirementException: Exception
    {
        public RequirementException(string message) : base(message)
        {
        }

        protected RequirementException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public RequirementException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}