/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using System;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelLinePresenter
    {
        private readonly ILevelLineView _view;
        private readonly LevelLineModel _model;
        private readonly Action<LevelNodeModel> _onClickNode;
        private readonly Action _onClickBtnPlay;

        public LevelLinePresenter(ILevelLineView view, LevelLineModel model, Action<LevelNodeModel> onClickNode, Action onClickBtnPlay)
        {
            _view = view;
            _model = model;
            _onClickNode = onClickNode;
            _onClickBtnPlay = onClickBtnPlay;
        }

        public void Render()
        { 
            _view.ResetAllNodes();
            _view.SetWinstreak(_model.WinStreak);
            
            var nodes = _model.GetLevelNodes();
            foreach (var node in nodes)
            {
                _view.ShowLevelNode(node, _onClickNode);

                if (node.isCurrent)
                    _view.UpdateBtnPlay(node.difficulty, _onClickBtnPlay);
            }
            
            _view.UpdateRemainThings();
        }
    }
}