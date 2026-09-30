/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using UnityEditor;

namespace Falcon.Manager.Importer
{
    public static class PackImportQueue
    {
        private const string QUEUE_NAME = "Falcon.PackageImportQueue";
        
        public static IImportRequest Poll()
        {
            if (!EditorPrefs.HasKey(QUEUE_NAME)) return null;
            var json = EditorPrefs.GetString(QUEUE_NAME);
            var queue = json.JsonToObjStrict<List<IImportRequest>>();

            if (queue.Count > 0)
            {
                var next = queue[0];
                queue.RemoveAt(0);

                EditorPrefs.SetString(QUEUE_NAME, queue.ToJsonStrict());
                return next;
            }

            EditorPrefs.DeleteKey(QUEUE_NAME);
            return null;
        }
        
        public static void Push(IImportRequest package)
        {
            if (!EditorPrefs.HasKey(QUEUE_NAME))
            {
                var queue = new List<IImportRequest> { package };
                EditorPrefs.SetString(QUEUE_NAME, queue.ToJsonStrict());
            }
            else
            {
                var json = EditorPrefs.GetString(QUEUE_NAME);
                var queue = json.JsonToObjStrict<List<IImportRequest>>();
                queue.Add(package);
                EditorPrefs.SetString(QUEUE_NAME, queue.ToJsonStrict());
            }
        }
        
        public static void Push(IEnumerable<IImportRequest> packages)
        {
            if (!EditorPrefs.HasKey(QUEUE_NAME))
            {
                var list = packages.ToList();
                EditorPrefs.SetString(QUEUE_NAME, list.ToJsonStrict());
            }
            else
            {
                var json = EditorPrefs.GetString(QUEUE_NAME);
                var queue = json.JsonToObjStrict<List<IImportRequest>>();
                queue.AddRange(packages);
                EditorPrefs.SetString(QUEUE_NAME, queue.ToJsonStrict());
            }
        }

        public static void Clear()
        {
            EditorPrefs.DeleteKey(QUEUE_NAME);
        }
    }
}