/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Diagnostics;
using System.Reflection;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public abstract class MyLogger<T> : UtilSingleton<T> where T : MyLogger<T>, new()
    {
        [Conditional("FALCON_LOG_DEBUG")]
        public void Info(object info)
        {
            var color = Instance.GetColor();
            Debug.Log(color != null ? $"<color={color}> {info} </color>" : info);
        }

        [Conditional("FALCON_LOG_DEBUG")]
        public void Warning(object info)
        {
            switch (info)
            {
                case AggregateException exception:
                {
                    foreach (var innerException in exception.InnerExceptions) Warning(innerException);
                    return;
                }
                // ReSharper disable once RedundantAssignment
                case TargetInvocationException invocationException:
                    Warning(invocationException.InnerException);
                    return;
                default:
                {
                    var color = Instance.GetColor();
                    Debug.LogWarning(color != null ? $"<color={color}> {info} </color>" : info);
                    break;
                }
            }
        }
        
        [Conditional("FALCON_LOG_DEBUG")]
        public void Warning(String message, Exception exception)
        {
            switch (exception)
            {
                case AggregateException aggregateException:
                {
                    foreach (var innerException in aggregateException.InnerExceptions) Error(innerException);
                    return;
                }
                // ReSharper disable once RedundantAssignment
                case TargetInvocationException invocationException:
                    Error(invocationException.InnerException);
                    return;
                default:
                {
                    var color = Instance.GetColor();
                    Debug.LogWarning(new Exception($"<color={color}> {message} </color>", exception));
                    break;
                }
            }
        }

        [Conditional("FALCON_LOG_DEBUG")]
        public void Error(Exception exception)
        {
            switch (exception)
            {
                case AggregateException aggregateException:
                {
                    foreach (var innerException in aggregateException.InnerExceptions) Error(innerException);
                    return;
                }
                // ReSharper disable once RedundantAssignment
                case TargetInvocationException invocationException:
                    Error(invocationException.InnerException);
                    return;
                default:
                {
                    var color = Instance.GetColor();
                    Debug.LogException(new Exception($"<color={color}> {exception.Message} </color>", exception));
                    break;
                }
            }
        }
        
        [Conditional("FALCON_LOG_DEBUG")]
        public void Error(String message, Exception exception)
        {
            switch (exception)
            {
                case AggregateException aggregateException:
                {
                    foreach (var innerException in aggregateException.InnerExceptions) Error(innerException);
                    return;
                }
                // ReSharper disable once RedundantAssignment
                case TargetInvocationException invocationException:
                    Error(invocationException.InnerException);
                    return;
                default:
                {
                    var color = Instance.GetColor();
                    Debug.LogException(new Exception($"<color={color}> {message} </color>", exception));
                    break;
                }
            }
        }

        [Conditional("FALCON_LOG_DEBUG")]
        public void Error(object info)
        {
            var color = Instance.GetColor();
            Debug.LogError(color != null ? $"<color={color}> {info} </color>" : info);
        }
        
        protected abstract string GetColor();
    }
}