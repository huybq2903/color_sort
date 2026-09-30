/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class ConcurrentDictionaryExtensions
    {
        public static TV Compute<TK, TV>(this ConcurrentDictionary<TK, TV> dict, TK key, Func<TV, TV> computation) where TV : class
        {
            try
            {
                return dict.AddOrUpdate(key,
                    _ =>
                    {
                        TV result = computation.Invoke(null);
                        
                        if (result != null)
                        {
                            return result;
                        }
                        throw new ReturnNullException();
                    },
                    (k, v) =>
                    {
                        TV result = computation.Invoke(v);
                        if (result != null)
                        {
                            return result;
                        }
                        dict.TryRemove(k, out _);
                        throw new ReturnNullException();
                    });
            }
            catch (ReturnNullException)
            {
                return null;
            }
        }

        public static TV? Compute<TK, TV>(this ConcurrentDictionary<TK, TV> dict, TK key, Func<TV?, TV?> computation) where TV : struct
        {
            try
            {
                return dict.AddOrUpdate(key,
                    _ =>
                    {
                        TV? result = computation.Invoke(null);
                        
                        if (result.HasValue)
                        {
                            return result.Value;
                        }
                        throw new ReturnNullException();
                    },
                    (k, v) =>
                    {
                        TV? result = computation.Invoke(v);
                        if (result.HasValue)
                        {
                            return result.Value;
                        }
                        dict.TryRemove(k, out _);
                        throw new ReturnNullException();
                    });
            }
            catch (ReturnNullException)
            {
                return null;
            }
        }
        
        public static TV GetOrDefault<TK, TV>(this ConcurrentDictionary<TK, TV> dict,TK key, TV orDefault)
        {
            return dict.GetValueOrDefault(key, orDefault);
        }
        
        public static TV GetOrDefault<TK, TV>(this ConcurrentDictionary<TK, TV> dict,TK key, Func<TV> orDefault)
        {
            return dict.TryGetValue(key, out var result) ? result : orDefault.Invoke();
        }
    }

    public class ReturnNullException : Exception
    {
    }
}