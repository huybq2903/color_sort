/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-05
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Falcon.Helpers.FReflection;

namespace Falcon.Shared.Tutorial
{
    public abstract class ATutorial : IFReflection
    {
        protected int _currentStepIndex = -1;
        protected readonly List<ATutorialStep> _steps = new();
        protected ATutorialManager _tutorialManager;

        public event Action OnTutorialStarted, OnTutorialCompleted;
        public string Id => GetType().GetCustomAttribute<TutorialAttribute>()?.Id;
        public bool IsStarted => _currentStepIndex >= 0;

        public virtual void OnInitialize(ATutorialManager tutorialManager)
        {
            _tutorialManager = tutorialManager;
        }

        public virtual void OnDispose() { }

        public void StartTutorial() => _tutorialManager.StartTutorial(this);

        internal virtual void OnStartTutorialInternal() => OnStartTutorial();

        protected virtual void OnStartTutorial()
        {
            OnTutorialStarted?.Invoke();
            if (_steps.Count == 0)
            {
                return;
            }
            _currentStepIndex = 0;
            _steps[_currentStepIndex].StartStep();
        }

        public virtual void NextStep()
        {
            if (_steps.Count == 0)
            {
                StopTutorial();
                return;
            }
            _steps[_currentStepIndex].StopStep();
            _currentStepIndex++;
            if (_currentStepIndex >= _steps.Count)
            {
                StopTutorial();
                return;
            }
            _steps[_currentStepIndex].StartStep();
        }

        protected virtual void StopTutorial()
        {
            _currentStepIndex = -1;
            _tutorialManager.OnTutorialCompleted(this);
            OnTutorialCompleted?.Invoke();
        }

        public virtual void ForceStopTutorial()
        {
            if (!IsStarted) return;
            _steps[_currentStepIndex].StopStep();
            StopTutorial();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TutorialAttribute : Attribute
    {
        public readonly string Id;
        public TutorialAttribute(string id, int order = 0)
        {
            Id = id;
        }
    }

    public static class TutorialRegistry
    {
        private static Dictionary<string, Type> _map;

        public static Dictionary<string, Type> Map
        {
            get
            {
                if (_map == null) Initialize();
                return _map;
            }
        }

        private static void Initialize()
        {
            _map = new Dictionary<string, Type>();
            foreach (var type in FReflection.Instance.GetTypes())
            {
                if (!typeof(ATutorial).IsAssignableFrom(type))
                    continue;
                var attr = type.GetCustomAttribute<TutorialAttribute>();
                if (attr == null)
                    continue;
                _map[attr.Id] = type;
            }
        }
    }
}