namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示一個設定檔案的節點
    /// </summary>
    public class PropertyFileNode
    {
        /// <summary>
        /// 取得該設定檔節點的路徑
        /// </summary>
        public string[] Path { get; }
        /// <summary>
        /// 取得該設定檔節點的標題
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// 取得該設定檔節點的功能描述
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// 指示該設定節點是否為可選
        /// </summary>
        public bool IsOptional { get; }
        /// <summary>
        /// 取得該設定節點的預設值
        /// </summary>
        public string DefaultValue { get; }

        /// <summary>
        /// <see cref="PropertyFileNode"/> 的建構子
        /// </summary>
        /// <param name="path">節點的路徑</param>
        /// <param name="title">節點的標題</param>
        /// <param name="description">節點的功能描述</param>
        /// <param name="isOptional">節點是否為可選</param>
        /// <param name="defaultValue">節點的預設值</param>
        public PropertyFileNode(string[] path, string title, string description, bool isOptional = true, string? defaultValue = null)
        {
            Path = path;
            Title = title;
            Description = description;
            IsOptional = isOptional;
            DefaultValue = defaultValue ?? string.Empty;
        }
    }

    /// <summary>
    /// 表示一個設定檔案的描述器，描述器可以定義一份設定檔案的節點架構
    /// </summary>
    public interface IPropertyFileDescriptor
    {
        /// <summary>
        /// 取得特定的設定檔節點
        /// </summary>
        /// <param name="path">以 . 分隔的節點路徑</param>
        PropertyFileNode GetNode(string[] path);
        /// <summary>
        /// 設定特定的設定檔節點
        /// </summary>
        /// <param name="node">要設定的節點物件</param>
        void SetNode(PropertyFileNode node);
        /// <summary>
        /// 設定所有的設定檔節點
        /// </summary>
        PropertyFileNode[] GetNodes();
    }
}
