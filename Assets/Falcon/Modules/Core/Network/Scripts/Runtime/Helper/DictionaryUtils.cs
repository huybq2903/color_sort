using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
namespace Falcon.Modules.Core.Network
{
    public static class Extension
    {
        public static bool IsGenericList(this Type oType)
        {
            return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>)));
        }
    }
    public class DictionaryUtils
    {

        public static object Decode(object ret, Dictionary<string, object> dictionary)
        {
            Type type = ret.GetType();
            FieldInfo[] infos = type.GetFields();
            if (infos != null)
            {
                foreach (FieldInfo info in infos)
                {
                    string name = info.Name;
                    Type propertyType = info.FieldType;
                    if (dictionary.ContainsKey(name))
                    {
                        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string)))
                        {
                            info.SetValue(ret, Convert.ChangeType(dictionary[name], propertyType));
                        }
                        else if (propertyType.IsArray)
                        {
                            Type elementType = propertyType.GetElementType();
                            if (elementType.IsPrimitive || propertyType.Equals(typeof(string)))
                            {
                                info.SetValue(ret, dictionary[name]);
                            }
                            else
                            {
                                Dictionary<string, object>[] dicts = (Dictionary<string, object>[])dictionary[name];
                                Array arr = Array.CreateInstance(elementType, dicts.Length);
                                for (int i = 0; i < dicts.Length; i++)
                                {
                                    object o = Activator.CreateInstance(elementType);
                                    Decode(o, dicts[i]);
                                    arr.SetValue(o, i);
                                }
                                info.SetValue(ret, arr);
                            }
                        }
                        else if (propertyType.IsGenericList())
                        {
                            var elementType = propertyType.GetGenericArguments().Single();
                            IList arr = (IList)Activator.CreateInstance(propertyType);
                            if (elementType.IsPrimitive || propertyType.Equals(typeof(string)) || elementType.Equals(typeof(string)))
                            {
                                IList v = (IList)dictionary[name];
                                for (int i = 0; i < v.Count; i++)
                                {
                                    arr.Add(Convert.ChangeType(v[i], elementType));
                                }
                            }
                            else
                            {
                                IList dicts = (IList)dictionary[name];
                                for (int i = 0; i < dicts.Count; i++)
                                {
                                    object o = Activator.CreateInstance(elementType);
                                    Decode(o, (Dictionary<string, object>)dicts[i]);
                                    arr.Add(o);
                                }
                            }
                            info.SetValue(ret, arr);
                        }
                        else if (propertyType.FullName.Contains("Dictionary"))
                        {
                            Dictionary<string, object> val = (Dictionary<string, object>)dictionary[name];
                            info.SetValue(ret, val);
                        }
                        else
                        {
                            Dictionary<string, object> val = (Dictionary<string, object>)dictionary[name];
                            info.SetValue(ret, Decode(Activator.CreateInstance(propertyType), val));
                        }
                    }
                }
            }
            return ret;
        }
        public static Dictionary<string, object> encode(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            Dictionary<string, object> ret = new Dictionary<string, object>();
            Type type = obj.GetType();
            FieldInfo[] infos = type.GetFields();
            if (infos != null)
            {
                foreach (FieldInfo info in infos)
                {

                    string name = info.Name;
                    Type propertyType = info.FieldType;
                    if (propertyType.IsPrimitive || propertyType.Equals(typeof(string)))
                    {
                        ret.Add(name, info.GetValue(obj));
                    }
                    else if (propertyType.IsArray)
                    {
                        Array value = (Array)info.GetValue(obj);
                        if (value != null)
                        {
                            Type elementType = propertyType.GetElementType();
                            if (elementType.IsPrimitive || propertyType.Equals(typeof(string)) || elementType.Equals(typeof(string)))
                            {
                                ret.Add(name, value);
                            }
                            else
                            {
                                Dictionary<string, object>[] dicts = new Dictionary<string, object>[value.Length];
                                for (int i = 0; i < value.Length; i++)
                                {
                                    dicts[i] = encode(value.GetValue(i));
                                }
                                ret.Add(name, dicts);
                            }
                        }
                    }
                    else if (propertyType.IsGenericList())
                    {
                        var elementType = propertyType.GetGenericArguments().Single();
                        IList arr = (IList)info.GetValue(obj);
                        if (arr != null)
                        {
                            if (elementType.IsPrimitive || propertyType.Equals(typeof(string)) || elementType.Equals(typeof(string)))
                            {
                                ret.Add(name, arr);
                            }
                            else
                            {
                                List<Dictionary<string, object>> dicts = new List<Dictionary<string, object>>();
                                for (int i = 0; i < arr.Count; i++)
                                {
                                    dicts.Add(encode(arr[i]));
                                }
                                ret.Add(name, dicts);
                            }
                        }
                    }
                    else
                    {
                        object value = info.GetValue(obj);
                        if (value != null)
                        {
                            ret.Add(name, encode(value));
                        }
                    }

                }
            }
            return ret;
        }

    }
}
