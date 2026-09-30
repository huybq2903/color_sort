/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [NoLazy]
    public class AppFlowService : MonoSingleton<AppFlowService>, IPostConstruct
    {
        private Scene? _currentScene;
        private bool _gameStop;

        [Inject][SingletonSorting(SortingOrder.CREATING)]private List<IPioneer> _pioneers;
        [Inject][SingletonSorting(SortingOrder.CREATING)]private List<ISceneSingleton> _sceneSingletons;
        [Inject][SingletonSorting(SortingOrder.DESTROYING)]private List<ITerminal> _terminals;

        public bool ApplicationRunning { get; private set; }

        private void Update()
        {
            var activeScene = SceneManager.GetActiveScene();
            _currentScene ??= activeScene;
            if (_currentScene.Value.name == activeScene.name) return;
            foreach (var sceneSingleton in _sceneSingletons)
            {
                sceneSingleton.OnNewScene(_currentScene.Value, activeScene);
                _currentScene = activeScene;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            switch (Application.isPlaying)
            {
                case true when !hasFocus:
                    CheckGameStop();
                    break;
                case true:
                    CheckGameContinue();
                    break;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            switch (Application.isPlaying)
            {
                case true when pauseStatus:
                    CheckGameStop();
                    break;
                case true:
                    CheckGameContinue();
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            ApplicationRunning = false;
            CheckGameStop();
        }

        public event Action OnGameStopped;

        public event Action OnGameContinued;
        private void CheckGameStop()
        {
            if (!_gameStop)
                _gameStop = true;
            else
                return;

            SingletonLogger.Instance.Info("On Game Stop");

            try
            {
                OnGameStopped?.Invoke();
            }
            catch (Exception e)
            {
                SingletonLogger.Instance.Error(e);
            }

            foreach (var terminalService in _terminals)
                try
                {
                    terminalService.OnPostStop();
                }
                catch (Exception e)
                {
                    SingletonLogger.Instance.Error(e);
                }
        }

        private void CheckGameContinue()
        {
            if (_gameStop)
                _gameStop = false;
            else
                return;
            SingletonLogger.Instance.Info("On Game Continue");

            foreach (var pioneerService in _pioneers)
                try
                {
                    pioneerService.OnPreContinue();
                }
                catch (Exception e)
                {
                    SingletonLogger.Instance.Error(e);
                }

            try
            {
                OnGameContinued?.Invoke();
            }
            catch (Exception e)
            {
                SingletonLogger.Instance.Error(e);
            }
        }

        public void OnPostConstruct()
        {
            ApplicationRunning = true;
        }
    }
}