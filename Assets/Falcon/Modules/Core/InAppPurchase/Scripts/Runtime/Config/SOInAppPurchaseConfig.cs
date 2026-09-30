/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// ScriptableObject chứa danh sách sản phẩm và các thiết lập chung.
    /// Được hệ thống IAP sử dụng khi chạy.
    /// </summary>
    [CreateAssetMenu(fileName = "IAPPackageConfig", menuName = "Configs/IAP Package Config")]
    public class SOInAppPurchaseConfig : ScriptableObject
    {
        [Header("General Settings")] 
        [InfoBox("Môi trường khởi tạo environment của Unity Services, để rỗng nếu k muốn khởi tạo")]
        public string UnityServicesEnvironment = "production";
        
        /// <summary>
        /// Thời gian chờ tối đa (tính bằng giây) để xác thực giao dịch.
        /// </summary>
        public float purchaseTimeout = 8f;

        [InfoBox("Có xác thực dưới client không nếu mà hết timeout chờ xác thực")]
        public bool isValidateLocalIfTimeout = true;
        
        [InfoBox("Có ghi log khi mua sản phẩm trong Editor không")]
        public bool isLogPurchaseInEditor;
        
        /// <summary>
        /// Danh sách các sản phẩm IAP hiện có trong ứng dụng.
        /// </summary>
        [Header("Products")] 
        public ProductInfo[] products;
        
        [Title("Validations")]
        public List<TypeToggle> validations = new();
        
        [Title("Loggers")]
        public List<TypeToggle> loggers = new();
        
#if UNITY_EDITOR
        private const string PATH = "Assets/FalconAssets/Modules/Core/InAppPurchase/ProductName.cs";
        [Button]
        private static void GenerateScriptProductName()
        {
            var directory = Path.GetDirectoryName(PATH);
            if (directory == null)
            {
                Debug.LogError("Đường dẫn lỗi");
                return;
            }
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var config = Resources.Load<SOInAppPurchaseConfig>(IAPConstant.NAME_CONFIG);
        
            if (!config)
            {
                Debug.LogError("Không tìm thấy SO_FCM_InAppPurchase_Config, vui lòng tạo config ở Falcon/Modules/InApp/Settings");
                return;
            }
            
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("/* This file is auto-generated. Do not modify. */");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace Falcon.Modules.Core.InAppPurchase.Runtime");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    public static class ProductName");
            stringBuilder.AppendLine("    {");
        
            foreach (var product in config.products)
            {
                stringBuilder.AppendLine($"        public const string {ToValidIdentifier(product.productID)} = \"{product.productID}\";");
            }
        
            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");
        
            File.WriteAllText(PATH, stringBuilder.ToString());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Thông báo",
                $"Product.cs generated successfully at {PATH}",
                "OK"
            );
        }
        
        private static string ToValidIdentifier(string input)
        {
            if (string.IsNullOrEmpty(input)) return "_";
        
            // B1: thay ký tự đặc biệt bằng "_"
            string cleaned = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
        
            // B2: tách theo "_"
            string[] parts = cleaned.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        
            // B3: PascalCase từng phần
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0 && char.IsLetter(parts[i][0]))
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
        
            string result = string.Join("_", parts);
        
            // B4: nếu bắt đầu bằng số → thêm "_" phía trước
            if (!string.IsNullOrEmpty(result) && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
        
            return result.ToUpper();
        }
#endif
    }
    
    /// <summary>
    /// Thông tin về một sản phẩm mua trong ứng dụng.
    /// </summary>
    [Serializable]
    public struct ProductInfo
    {
        /// <summary>
        /// ID duy nhất của sản phẩm.
        /// </summary>
        public string productID;

        /// <summary>
        /// Giá mặc định của sản phẩm.
        /// </summary>
        public string defaultPrice;
        
        /// <summary>
        /// Loại sản phẩm theo Unity IAP (Consumable, NonConsumable, Subscription).
        /// </summary>
        public ProductType type;
    }
    
    [Serializable]
    public class TypeToggle
    {
        public bool enabled;
        [ReadOnly]
        public string assemblyQualifiedTypeName;
        [ReadOnly] 
        public string nameDisplay;
    }
}