
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using Falcon.Modules.Core.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Falcon.Modules.Translate.Runtime
{
    [FAMessage("sc_translate_req")]
    public class SCTranslateReq : SCMessage
    {
        public string request_uuid;
        public string text;
        public string target_language;
        public string source_language;

        public override void OnData()
        {
            TranslateText(text, target_language, source_language);
        }

        public void TranslateText(string sourceText, string targetLanguage, string sourceLanguage)
        {
            if (string.IsNullOrWhiteSpace(sourceLanguage))
                sourceLanguage = "auto";

            // Bắt buộc phải chạy coroutine qua một MonoBehaviour
            // Có thể thay 'CoroutineRunnerForTranslate.Instance' bằng bất kỳ MonoBehaviour nào trong project bạn
            CoroutineRunnerForTranslate.Instance.StartCoroutine(TranslateCoroutine(sourceText, targetLanguage, sourceLanguage));
        }

        /*private IEnumerator TranslateCoroutine(string sourceText, string targetLanguage, string sourceLanguage)
        {
            string url =
                $"https://translate.google.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={UnityWebRequest.EscapeURL(sourceText)}";

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    Debug.Log("GoogleTranslateClient error=" + www.error);
                }
                else
                {
                    string result = www.downloadHandler.text;
                    JToken tmp = null;
                    try
                    {
                        tmp = JToken.Parse(result);
                    }
                    catch
                    {
                        Debug.Log($"GoogleTranslateClient error= Cannot deserialize response result into JToken. Response = {result}");
                    }

                    if (tmp != null)
                    {
                        var mainTranslationInfo = tmp[0];
                        var translatedText = GetTranslation(mainTranslationInfo);
                        string detectedSourceLanguage = source_language;
                        if (string.IsNullOrEmpty(source_language) || source_language.Equals("auto"))
                        {
                            detectedSourceLanguage = "unknown";
                            if (tmp.Count() > 8)
                            {
                                JArray languageDetections = tmp[8] as JArray;
                                if (languageDetections != null)
                                {
                                    detectedSourceLanguage = GetDetectedSourceLanguage(languageDetections);
                                }
                            }
                        }

                        new CSTranslateRsp(this, translatedText, detectedSourceLanguage).Send();
                        Debug.Log(
                            $"GoogleTranslateClient \n{text}\n({detectedSourceLanguage}=>{target_language}){translatedText}");
                    }
                }
            }
        }*/

        #region New
        private IEnumerator TranslateCoroutine(string sourceText, string targetLanguage, string sourceLanguage)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                Debug.LogWarning("TranslateCoroutine: sourceText is empty");
                yield break;
            }

            // Chuẩn hoá tham số
            var sl = string.IsNullOrEmpty(sourceLanguage) ? "auto" : sourceLanguage;
            var tl = string.IsNullOrEmpty(targetLanguage) ? "en" : targetLanguage;

            // LƯU Ý: endpoint này là không chính thức, có thể thay đổi/bị chặn
            string url =
                $"https://translate.googleapis.com/translate_a/single" +
                $"?client=gtx&sl={UnityWebRequest.EscapeURL(sl)}&tl={UnityWebRequest.EscapeURL(tl)}&dt=t&q={UnityWebRequest.EscapeURL(sourceText)}";

            using (var www = UnityWebRequest.Get(url))
            {
                www.timeout = 10; // giây
                yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                bool hasError = www.result != UnityWebRequest.Result.Success;
#else
        bool hasError = www.isNetworkError || www.isHttpError;
#endif

                if (hasError)
                {
                    Debug.LogError($"GoogleTranslateClient error={www.error} (code {www.responseCode})");
                    yield break;
                }

                var jsonText = www.downloadHandler.text;
                JArray root = null;
                try
                {
                    root = JArray.Parse(jsonText);
                }
                catch
                {
                    Debug.LogError($"GoogleTranslateClient error= Cannot parse JSON. Response = {jsonText}");
                    yield break;
                }

                // Lấy text dịch
                string translatedText = ExtractTranslatedText(root);

                // Xác định ngôn ngữ nguồn (khi sl=auto)
                string detectedSourceLanguage = sl;
                if (sl == "auto")
                {
                    detectedSourceLanguage = ExtractDetectedSourceLanguage(root) ?? "unknown";
                }

                // TODO: thay bằng callback/event thực tế của bạn
                new CSTranslateRsp(this, translatedText, detectedSourceLanguage).Send();

                Debug.Log($"GoogleTranslateClient\n{sourceText}\n({detectedSourceLanguage}=>{tl}) {translatedText}");
            }
        }

        // Tổng hợp các mảnh câu ở root[0][i][0]
        private static string ExtractTranslatedText(JArray root)
        {
            var sb = new StringBuilder();

            var sentences = root.Count > 0 ? root[0] as JArray : null;
            if (sentences != null)
            {
                foreach (var seg in sentences)
                {
                    // seg dạng: [ translated, original, ... ]
                    var t = (seg as JArray)?[0]?.ToString();
                    if (!string.IsNullOrEmpty(t))
                        sb.Append(t);
                }
            }
            return sb.ToString();
        }

        // Cố gắng đọc ngôn ngữ nguồn (tuỳ biến thể JSON của Google)
        // Nhiều bản trả về ở root[2]; fallback thử root[8] (bảng detect)
        private static string ExtractDetectedSourceLanguage(JArray root)
        {
            // Thử root[2] trước (thường là mã ngôn ngữ)
            if (root.Count > 2 && root[2] != null && root[2].Type != JTokenType.Null)
            {
                var code = root[2].ToString();
                if (!string.IsNullOrEmpty(code))
                    return code;
            }

            // Fallback: root[8][0][0][0] → "en"/"vi"/...
            if (root.Count > 8 && root[8] is JArray arr8 && arr8.Count > 0)
            {
                var top = arr8[0] as JArray;
                var cand = top != null && top.Count > 0 ? top[0] as JArray : null;
                var code = cand != null && cand.Count > 0 ? cand[0]?.ToString() : null;
                if (!string.IsNullOrEmpty(code))
                    return code;
            }

            return null;
        }
        #endregion

        private static string GetTranslation(JToken translationInfo)
        {
            string r = "";
            foreach (var item in translationInfo)
            {
                if (item.Count() > 4)
                {
                    if (item.First != null) r += item.First.Value<string>();
                }
            }

            return r;
        }

        private static string GetDetectedSourceLanguage(JArray item)
        {
            JArray languages = item[0] as JArray;
            if (languages != null && languages.Count > 0)
            {
                return (string) languages[0];
            }

            return null;
        }

        // MonoBehaviour helper cho coroutine (nếu chưa có, copy luôn class này vào đâu cũng được!)
        public class CoroutineRunnerForTranslate : MonoBehaviour
        {
            private static CoroutineRunnerForTranslate _instance;
            public static CoroutineRunnerForTranslate Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var obj = new GameObject("CoroutineRunnerForTranslate");
                        DontDestroyOnLoad(obj);
                        _instance = obj.AddComponent<CoroutineRunnerForTranslate>();
                    }
                    return _instance;
                }
            }
        }
    }
}