using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.IO;

namespace WitherTorch.Core
{
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
            _path = "";
            currentObject = null;
        }

        protected bool isDirty = false;
        public JToken this[string key]
        {
            get
            {
                if (key == null) return null;
                else
                {
                    if (!isInitialized) Initialize();
                    return currentObject.SelectToken(key, false);
                }
            }
            set
            {
                if (key == null) return;
                if (value == null)
                {
                    if (!isInitialized) Initialize();
                    currentObject.SelectToken(key, false)?.Remove();
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
                    JObject _obj = null;
                    try
                    {
                        _obj = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd());
                    }
                    catch (Exception)
                    {

                    }
                    if (_obj != null)
                    {
                        currentObject = _obj;
                    }
                    else if (create && currentObject == null)
                    {
                        currentObject = new JObject();
                    }
                    GC.Collect();
                    reader.Close();
                }
            }
            else if (create && currentObject == null)
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
                using (StreamWriter writer = new StreamWriter(new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    try
                    {
                        writer.Write(JsonConvert.SerializeObject(currentObject, Formatting.Indented));
                        writer.Flush();
                        writer.Close();
                    }
                    catch (Exception)
                    {

                    }
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
                string path = enumerator.Current as string;
                canMove = enumerator.MoveNext();
                bool isLastPath = !canMove;
                if (path != null)
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
                            throw new ArgumentException();
                        }
                    }
                    JToken tempToken = result[path];
                    if (index == -1)
                    {
                        if (isLastPath)
                        {
                            result[path] = token;
                        }
                        else if (tempToken == null || tempToken.Type == JTokenType.Array)
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
                        if (tempToken == null || tempToken.Type != JTokenType.Array)
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
                            else if (tempToken == null)
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
