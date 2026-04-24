using System.Text.Json.Nodes;

namespace WitherTorch.Core.Tagging
{
    /// <summary>
    /// 表示一個持久化標籤
    /// </summary>
    public interface IPersistentTag
    {
        /// <summary>
        /// 取得建立此持久化標籤的工廠
        /// </summary>
        IPersistentTagFactory GetFactory();

        /// <summary>
        /// 從 <paramref name="source"/> 載入持久化標籤資料
        /// </summary>
        /// <param name="source">要載入持久化標籤資料的原始 JSON 物件</param>
        /// <returns>是否成功載入</returns>
        bool Load(JsonObject source);

        /// <summary>
        /// 將資料儲存至 <paramref name="destination"/> 所指定之物件內
        /// </summary>
        /// <param name="destination">要儲存持久化標籤資料的原始 JSON 物件</param>
        /// <returns>是否成功儲存</returns>
        bool Store(JsonObject destination);
    }
}
