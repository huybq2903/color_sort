
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Translate.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    public class GoogleClientTranslateGroup : MonoBehaviour
    {
        #region FIELDS

        [Header("Nếu _autoTranslate = true, sẽ tự dịch mà không cần bấm nút. Bỏ nút đi nhé.")]
        [SerializeField] private bool _autoTranslate = false;
        [SerializeField] private TranslateButton translateButton;
        public List<TextMeshProUGUI> tmps = new List<TextMeshProUGUI>();
        public List<Text> texts = new List<Text>();
        private List<TranslateTextBase> needTranslateList = new List<TranslateTextBase>();

        public List<TranslateTextBase> NeedTranslateList { get
            {
                CheckAndRebuildNeedTranslateList();
                return needTranslateList;
            }
            set => needTranslateList = value; 
        }

        private void CheckAndRebuildNeedTranslateList()
        {
            tmps.RemoveAll(e => e == null);
            texts.RemoveAll(e => e == null);

            bool needRebuild = false;

            int expectedCount = tmps.Count + texts.Count;

            // 1. Null hoặc số lượng khác là rebuild ngay
            if (needTranslateList == null || needTranslateList.Count != expectedCount)
            {
                needRebuild = true;
            }
            else
            {
                // 2. So từng phần tử với list gốc
                int idx = 0;
                for (int i = 0; i < tmps.Count; i++, idx++)
                {
                    var tmp = tmps[i];
                    var comp = needTranslateList[idx];

                    if (tmp == null)
                        Debug.LogError("Null! Fix please!");

                    // Check null, hoặc không phải đúng object TMP, hoặc không đúng instance bọc quanh
                    if (comp.OriginTextComponent != tmp)
                    {
                        needRebuild = true;
                        break;
                    }
                }
                for (int i = 0; i < texts.Count; i++, idx++)
                {
                    var text = texts[i];
                    var comp = needTranslateList[idx];

                    if (text == null)
                        Debug.LogError("Null! Fix please!");

                    if (comp.OriginTextComponent != text)
                    {
                        needRebuild = true;
                        break;
                    }
                }
            }

            if (needRebuild)
            {
                needTranslateList = new List<TranslateTextBase>(expectedCount);

                foreach (var tmp in tmps)
                {
                    if (tmp != null)
                    {
                        var translateComp = tmp.GetComponent<TranslateTextTMP>();
                        if (translateComp == null)
                            translateComp = tmp.gameObject.AddComponent<TranslateTextTMP>();
                        needTranslateList.Add(translateComp);
                    }
                }
                foreach (var text in texts)
                {
                    if (text != null)
                    {
                        var translateComp = text.GetComponent<TranslateTextLegacy>();
                        if (translateComp == null)
                            translateComp = text.gameObject.AddComponent<TranslateTextLegacy>();
                        needTranslateList.Add(translateComp);
                    }
                }
            }
        }

        #endregion

        #region DATA

        private List<TranslateUnit> translateUnitList = new List<TranslateUnit>();
        private List<string> requestingUuidList = new List<string>();

        private TranslateStatus status;

        #endregion

        #region PROPERTIES

        public TranslateStatus Status
        {
            get => status;
            set
            {
                if (status != value && translateButton != null) translateButton.OnStatusChanged(status, value);
                status = value;
            }
        }

        #endregion

        #region EVENTS

        private void Awake()
        {
            InitTranslateUnitList();
        }

        private void OnEnable()
        {
            RefreshStatus();
            SCTranslateRsp.onGetDataEvent += OnGetTranslatedData;
            StartCoroutine(CheckTextChange());
        }

        private void OnDisable()
        {
            SCTranslateRsp.onGetDataEvent -= OnGetTranslatedData;
        }

        private void Start()
        {
            Status = TranslateStatus.Translate;

            // FG
            if (translateButton != null)
                translateButton.Button.onClick.AddListener(OnClickTranslateButton);
        }

        #endregion

        #region INIT

        void InitTranslateUnitList()
        {
            translateUnitList = new List<TranslateUnit>();
            int count = NeedTranslateList.Count;
            for (int i = 0; i < count; i++)
            {
                translateUnitList.Add(new TranslateUnit(needTranslateList[i]));
            }
        }

        #endregion
        
        #region MESSAGE HANDLE

        void OnGetTranslatedData(SCTranslateRsp rsp)
        {
            if (rsp == null)
            {
                Debug.LogWarning("[Google Client Translate] Translate response is null!!");
                return;
            }

            if (requestingUuidList.Contains(rsp.request_uuid))
            {
                requestingUuidList.Remove(rsp.request_uuid);
                foreach (var translateUnit in translateUnitList)
                {
                    if (translateUnit != null && translateUnit.translateData != null &&
                        string.Equals(translateUnit.translateData.requestUuid, rsp.request_uuid))
                    {
                        translateUnit.translateData.translatedText = rsp.text_translated;
                        translateUnit.TranslateDependOnData();
                    }
                }
                
                if (requestingUuidList.Count == 0)
                {
                    /*foreach (var translateUnit in translateUnitList)
                    {
                        if (translateUnit != null)
                        {
                            translateUnit.TranslateDependOnData();
                        }
                    }*/

                    Status = TranslateStatus.Revert;
                }
            }
        }

        #endregion

        #region TRANSLATE HANDLE

        public void OnClickTranslateButton()
        {
            switch (Status)
            {
                case TranslateStatus.Translate:
                    //Translate();
                    TranslatePieceByPiece();
                    break;
                case TranslateStatus.Revert:
                    Revert();
                    break;
                case TranslateStatus.Translating:
                    StopTranslate();
                    break;
            }
        }
        
        void Translate()
        {
            if (!AccountManager.Instance.IsLogin)
            {
                Debug.LogError("Chưa login!!!");
                return;
            }

            List<CSTranslateReq> csReqList = new List<CSTranslateReq>();
            foreach (var translateUnit in translateUnitList)
            {
                if (translateUnit != null && translateUnit.NeedTranslate())
                {
                    var cs = translateUnit.PrepareMessage();
                    csReqList.Add(cs);
                    translateUnit.status = TranslateStatus.Translating;
                    translateUnit.translateData.requestUuid = cs.request_uuid;
                }
            }

            if (csReqList.Count == 0) // Đã dịch hết và không có text nào cần dịch mới
            {
                foreach (var translateUnit in translateUnitList)
                {
                    if (translateUnit != null)
                    {
                        translateUnit.TranslateDependOnData();
                    }
                }

                Status = TranslateStatus.Revert;
            }
            else
            {
                Status = TranslateStatus.Translating;
                requestingUuidList = csReqList.Select(cs => cs.request_uuid).ToList();
                foreach (var csTranslateReq in csReqList)
                {
                    csTranslateReq.Send();
                }
            }
        }

        void TranslatePieceByPiece()
        {
            if (!AccountManager.Instance.IsLogin)
            {
                Debug.LogError("Chưa login, nên chưa dịch được.");
                return;
            }

            List<CSTranslateReq> csReqList = new List<CSTranslateReq>();
            foreach (var translateUnit in translateUnitList)
            {
                if (translateUnit != null)
                {
                    if (translateUnit.NeedTranslate())
                    {
                        var cs = translateUnit.PrepareMessage();
                        csReqList.Add(cs);
                        translateUnit.status = TranslateStatus.Translating;
                        translateUnit.translateData.requestUuid = cs.request_uuid;
                    }
                    else
                    {
                        translateUnit.TranslateDependOnData();
                    }
                }
            }

            if (csReqList.Count == 0) // Đã dịch hết và không có text nào cần dịch mới
            {
                Status = TranslateStatus.Revert;
            }
            else
            {
                Status = TranslateStatus.Translating;
                requestingUuidList = csReqList.Select(cs => cs.request_uuid).ToList();
                foreach (var csTranslateReq in csReqList)
                {
                    csTranslateReq.Send();
                }
            }
        }

        void Revert()
        {
            Status = TranslateStatus.Translate;
            foreach (var translateUnit in translateUnitList)
            {
                if (translateUnit != null)
                {
                    translateUnit.Revert();
                }
            }
        }

        void StopTranslate()
        {
            foreach (var translateUnit in translateUnitList)
            {
                if (translateUnit.status == TranslateStatus.Translating)
                    translateUnit.StopTranslate();
                else if (translateUnit.status == TranslateStatus.Revert)
                    translateUnit.Revert();
            }
            requestingUuidList.Clear();
            Status = TranslateStatus.Translate;
        }
        
        void OnTextChangedCallback()
        {
            RefreshStatus();
        }

        void RefreshStatus()
        {
            requestingUuidList.Clear();

            if (Status == TranslateStatus.Translating)
            {
                StopTranslate();
            }
            else if (Status == TranslateStatus.Revert)
            {
                bool hasChanged = false;
                foreach (var unitData in translateUnitList)
                {
                    if (unitData.HasChanged())
                    {
                        hasChanged = true;
                        break;
                    }
                }

                if (hasChanged)
                {
                    for (int i = 0; i < translateUnitList.Count; i++)
                    {
                        translateUnitList[i].Revert();
                    }
                    
                    Status = TranslateStatus.Translate;
                }
            }

            if (this.gameObject.activeInHierarchy == true && _autoTranslate == true && Status == TranslateStatus.Translate)
                StartCoroutine(IEWaitAndAutoTranslate());
        }

        IEnumerator IEWaitAndAutoTranslate()
        {
            yield return new WaitUntil(() => AccountManager.Instance.IsLogin);
            TranslatePieceByPiece();
        }

        void ResetData()
        {
            foreach (var translateUnit in translateUnitList)
            {
                translateUnit.ResetData();
            }
        }

        #endregion

        #region EXTEND

        public void PublicRevert()
        {
            Revert();
        }
        
        public void PublicRefresh()
        {
            RefreshStatus();
        }
        
        public void Hide()
        {
            if (translateButton != null)
                translateButton.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (translateButton != null)
                translateButton.gameObject.SetActive(true);
        }

        public void ForceRevert()
        {
            Revert();
        }

        #endregion

        #region EDITOR

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (enabled && Application.isEditor)
            {
                if (!translateButton && _autoTranslate == false)
                {
                    translateButton = GetComponentInChildren<TranslateButton>();
                    if (!translateButton)
                    {
                        translateButton = (PrefabUtility
                            .InstantiatePrefab(Resources.Load<TranslateButton>(TranslateManager.ResourcesPathOfYourTranslateButton())) as TranslateButton);
                        EditorApplication.delayCall += DelaySetGoParent;
                    }
                }
                else if (_autoTranslate == true && translateButton != null)
                    EditorApplication.delayCall += DelayDestroyTranslateButton;
            }
        }

        void DelaySetGoParent()
        {
            EditorApplication.delayCall -= DelaySetGoParent;

            Transform translateButtonTransform;
            (translateButtonTransform = translateButton.transform).SetParent(transform);
            translateButtonTransform.localPosition = Vector3.zero;
            translateButtonTransform.localScale = Vector3.one;
        }
        void DelayDestroyTranslateButton()
        {
            if (translateButton != null)
            {
                DestroyImmediate(translateButton.gameObject);
                translateButton = null;
            }
        }
#endif

        #endregion

        IEnumerator CheckTextChange()
        {
            yield return new WaitForSeconds(0.1f);
            bool theSame = true;
            int count = NeedTranslateList.Count;
            for (int i = 0; i < count; i++)
            {
                if (needTranslateList[i] != null && needTranslateList[i].text != needTranslateList[i].BeforeText)
                {
                    theSame = false;
                    break;
                }
            }

            if (theSame == false)
            {
                OnTextChangedCallback();
                for (int i = 0; i < count; i++)
                {
                    if (needTranslateList[i] != null && needTranslateList[i].text != needTranslateList[i].BeforeText)
                    {
                        needTranslateList[i].BeforeText = needTranslateList[i].text;
                    }
                }
            }
        }
    }
}