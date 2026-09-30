using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperScrollView
{
    public class LoopListViewItem : MonoBehaviour
    {
        public List<RectTransform> childItemList;
        public Action onInit;

        public void Init()
        {
            onInit?.Invoke();
        }
    }
}
