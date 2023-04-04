using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.IO;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示以 JSON 格式 (.json) 儲存的設定檔
    /// </summary>
    public class JsonPropertyFile : IPropertyFile
    {
        protected IPropertyFileDescriptor descriptor;
        protected string _path;
        protected JObject currentObject;
        protected bool create;
        protected bool isInitialized;

        public JsonPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false)
        {
            this.create = create;
            _path = path;
            if (!WTCore.UseLazyLoadingOnPropertyFiles || ignoreLazyRequest)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            isInitialized = true;
            Reload();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _path = "";
            currentObject = null;
        }

        protected bool isDirty = false;
        public JToken this[string key]
        {
            get
            {
                if (key is null) return null;
                else
                {
                    if (!isInitialized) Initialize();
                    return currentObject.SelectToken(key, false);
                }
            }
            set
            {
                if (key is null) return;
                if (value is null)
                {
                    if (!isInitialized) Initialize();
                    JToken token = currentObject.SelectToken(key, false);
                    if (token is JValue val)
                    {
                        if (val?.Parent is JProperty property && property?.Parent is JObject obj)
                        {
                            obj?.Remove(property.Name);
                        }
                    }
                    else
                    {
                        token?.Remove();
                    }
                }
                else
                {
                    if (!isInitialized) Initialize();
                    EditNode(key, value);
                }
                isDirty = true;
            }
        }

        public virtual void Reload()
        {
            if (File.Exists(_path))
            {
                using (StreamReader reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    using (JsonTextReader jsonReader = new JsonTextReader(reader))
                    {
                        JObject _obj;
                        try
                        {
                            _obj = GlobalSerializers.JsonSerializer.Deserialize(jsonReader) as JObject;
                        }
                        catch (Exception)
                        {
                            _obj = null;
                        }
                        if (_obj != null)
                        {
                            currentObject = _obj;
                        }
                        else if (create && currentObject is null)
                        {
                            currentObject = new JObject();
                        }
                    }
                    try
                    {
                        reader.Close();
                    }
                    catch (IOException)
                    {
                    }
                    catch (NullReferenceException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
            else if (create && currentObject is null)
            {
                currentObject = new JObject();
            }
        }

        public virtual void Save(bool force)
        {
            if (force && !isInitialized)
            {
                Initialize();
            }
            if ((isDirty && isInitialized) || force)
            {
                using (JsonTextWriter writer = new JsonTextWriter(new StreamWriter(new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))) { CloseOutput = true })
                {
                    try
                    {
                        GlobalSerializers.JsonSerializer.Serialize(writer, currentObject);
                        writer.Flush();
                        create = false;
                    }
                    catch (Exception)
                    {
                    }
                    writer.Close();
                }
            }
        }

        public IPropertyFileDescriptor GetDescriptor()
        {
            return descriptor;
        }

        public void SetDescriptor(IPropertyFileDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        private void EditNode(string nodeLocation, object value)
        {
            JToken token;
            try
            {
                if (value is JToken jVal)
                {
                    token = jVal;
                }
                else
                {
                    token = new JValue(value);
                }
            }
            catch (Exception)
            {
                return;
            }
            string[] paths = nodeLocation.Split('.');
            JToken result = currentObject;
            IEnumerator enumerator = paths.GetEnumerator();
            bool canMove = enumerator.MoveNext();
            while (canMove)
            {
                canMove = enumerator.MoveNext();
                bool isLastPath = !canMove;
                if (enumerator.Current is string path)
                {
                    int leftBracketIndexOf = path.LastIndexOf('[');
                    int rightBracketIndexOf = path.LastIndexOf(']');
                    int index = -1;
                    if (leftBracketIndexOf != -1 && rightBracketIndexOf == path.Length - 1 && leftBracketIndexOf < rightBracketIndexOf)
                    {
                        string indexString = path.Substring(leftBracketIndexOf, path.Length - leftBracketIndexOf - 2);
                        if (int.TryParse(indexString, out index))
                        {
                            path = path.Substring(0, leftBracketIndexOf);
                        }
                        else
                        {
                            throw new ArgumentException("無效的路徑", nameof(path));
                        }
                    }
                    JToken tempToken = result[path];
                    if (index == -1)
                    {
                        if (isLastPath)
                        {
                            result[path] = token;
                        }
                        else if (tempToken is null || tempToken.Type == JTokenType.Array)
                        {
                            tempToken = new JObject();
                            result[path] = tempToken;
                            result = tempToken;
                        }
                        else
                        {
                            result = tempToken;
                        }
                    }
                    else
                    {
                        if (tempToken is null || tempToken.Type != JTokenType.Array)
                        {
                            tempToken = isLastPath ? token : new JObject() as JToken;
                            result[path] = new JArray(new JToken[] { tempToken });
                            result = tempToken;
                        }
                        else
                        {
                            JArray tempArray = tempToken as JArray;
                            tempToken = tempArray[index];
                            if (isLastPath)
                            {
                                tempToken = token;
                                tempArray[index] = tempToken;
                            }
                            else if (tempToken is null)
                            {
                                tempToken = new JObject();
                                tempArray[index] = tempToken;
                            }
                            result = tempToken;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!isInitialized) Initialize();
            return JsonConvert.SerializeObject(currentObject);
        }

        public string GetFilePath()
        {
            return _path;
        }
    }
}
