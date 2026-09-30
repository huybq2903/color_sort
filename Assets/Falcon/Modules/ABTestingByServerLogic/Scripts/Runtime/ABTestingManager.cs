using Falcon.Modules.Core.SaveLoad.Runtime;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */
namespace Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime
{
    public class ABTestingManager
    {
        public const string AB_TESTING_LEVEL_KEY = "ab_levels_by_server";
        //Tạo singleton
        private static ABTestingManager _instance;
        private ABTestingManager() { }
        public static ABTestingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ABTestingManager();
                }
                return _instance;
            }
        }

        private string _ab_testing_campaign;
        private const string _ab_testing_campaign_key = "ab_testing_campaign_key";
        public string Ab_testing_campaign
        {
            get
            {
                if (string.IsNullOrEmpty(_ab_testing_campaign))
                    _ab_testing_campaign = SaveLoadHandler.ExistsKey(_ab_testing_campaign_key) ? SaveLoadHandler.Load<string>(_ab_testing_campaign_key) : "";
                return _ab_testing_campaign;
            }
            set
            {
                _ab_testing_campaign = value;
                SaveLoadHandler.Save(_ab_testing_campaign_key, _ab_testing_campaign);
            }
        }

        private string _ab_testing_value = "";
        private const string _ab_testing_value_key = "ab_testing_value_key";
        public string Ab_testing_value
        {
            get
            {
                if (string.IsNullOrEmpty(_ab_testing_value)) 
                    _ab_testing_value = SaveLoadHandler.ExistsKey(_ab_testing_value_key) ? SaveLoadHandler.Load<string>(_ab_testing_value_key) : "";
                return _ab_testing_value;
            }
            set
            {
                _ab_testing_value = value;
                SaveLoadHandler.Save(_ab_testing_value_key, _ab_testing_value);
            }
        }
        
        public bool IsInABTestingLevel => !string.IsNullOrEmpty(Ab_testing_campaign) && Ab_testing_campaign.Contains(AB_TESTING_LEVEL_KEY);
         
    }
}