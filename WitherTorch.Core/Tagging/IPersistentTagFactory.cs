using System;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Tagging
{
    /// <summary>
    /// 表示一個可以持久儲存的標籤物件之工廠，
    /// </summary>
    public interface IPersistentTagFactory
    {
        /// <summary>
        /// 取得該持久標籤類型的類型物件
        /// </summary>
        Type GetTagType();

        /// <summary>
        /// 取得該持久標籤類型的唯一辨識字串
        /// </summary>
        string GetTagTypeId();

        /// <summary>
        /// 建立新的持久標籤物件
        /// </summary>
        /// <returns></returns>
        IPersistentTag Create();

        /// <summary>
        /// 在使用者呼叫 <see cref="PersistentTagFactoryRegister.TryRegisterFactoryAsync(IPersistentTagFactory)"/> 來註冊伺服器軟體時會呼叫的初始化程式碼
        /// </summary>
        /// <returns>初始化作業是否成功</returns>
        Task<bool> TryInitializeAsync(CancellationToken cancellationToken);
    }
}
