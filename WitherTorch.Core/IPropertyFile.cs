using System;

namespace WitherTorch.Core
{
    public interface IPropertyFile : IDisposable
    {
        void Reload();
        void Save(bool force);
        IPropertyFileDescriptor GetDescriptor();
        void SetDescriptor(IPropertyFileDescriptor descriptor);
        string GetFilePath();
    }
}
