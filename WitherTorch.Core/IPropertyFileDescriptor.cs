namespace WitherTorch.Core
{
    public class PropertyFileNode
    {
        public string[] Path { get; protected set; }
        public string Title { get; protected set; }
        public string Description { get; protected set; }
        public bool IsOptional { get; protected set; }
        public string DefaultValue { get; protected set; }
        public PropertyFileNode(string[] _path, string _title, string _description, bool isOptional = true, string defaultValue = null)
        {
            Path = _path;
            Title = _title;
            Description = _description;
            IsOptional = isOptional;
            DefaultValue = defaultValue ?? string.Empty;
        }
    }
    public interface IPropertyFileDescriptor
    {
        PropertyFileNode GetNode(string[] path);
        void SetNode(PropertyFileNode node);
        PropertyFileNode[] GetNodes();
    }
}
