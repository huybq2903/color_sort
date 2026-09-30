/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-05
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.Tutorial
{
    public abstract class ATutorialManager : MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        protected readonly Dictionary<string, ATutorial> _tutorials = new();
        protected readonly Queue<ATutorial> _pendingTutorials = new();
        protected ATutorial _currentTutorial;

        public ATutorial CurrentTutorial => _currentTutorial;
        public ATutorial PreloadTutorial(string tutorialId)
        {
            if (!TutorialRegistry.Map.ContainsKey(tutorialId))
                return null;
            if (!_tutorials.ContainsKey(tutorialId))
            {
                var tutorial = (ATutorial)Activator.CreateInstance(TutorialRegistry.Map[tutorialId]);
                _tutorials[tutorialId] = tutorial;
                tutorial.OnInitialize(this);
            }
            return _tutorials[tutorialId];
        }

        public void RemoveTutorial(string tutorialId)
        {
            if (!_tutorials.TryGetValue(tutorialId, out var tutorial)) return;

            // Xoá đúng cái đang chạy mà không nhả slot thì IsAnyTutorialActive kẹt true vĩnh viễn.
            if (_currentTutorial == tutorial) _currentTutorial = null;
            tutorial.OnDispose();
            _tutorials.Remove(tutorialId);
        }

        internal void StartTutorial(ATutorial tutorial)
        {
            if (_currentTutorial != null)
            {
                if (tutorial != _currentTutorial && !_pendingTutorials.Contains(tutorial))
                    _pendingTutorials.Enqueue(tutorial);
                return;
            }
            _currentTutorial = tutorial;
            _currentTutorial.OnStartTutorialInternal();
        }

        internal void OnTutorialCompleted(ATutorial tutorial)
        {
            if (_currentTutorial != tutorial)
                return;
            _currentTutorial = null;
            while (_pendingTutorials.Count > 0)
            {
                var next = _pendingTutorials.Dequeue();
                if (!_tutorials.ContainsValue(next))
                    continue;
                StartTutorial(next);
                return;
            }
        }

        protected virtual void OnDestroy()
        {
            foreach (var tutorial in _tutorials.Values)
                tutorial.OnDispose();
        }
    }
}