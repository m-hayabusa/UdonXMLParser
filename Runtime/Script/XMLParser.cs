
using UdonSharp;
using VRC.SDK3.Data;

namespace studio.nekomimi.parser.xml
{
    public class XMLParser : UdonSharpBehaviour
    {
        public static DataDictionary GetNodeByPath(DataDictionary root, string path)
        {
            string[] query = path.Split('/');
            var elem = root;
            for (int i = 1; i < query.Length; i++)
            {
                var tag = "";
                var idx = 0;
                if (query[i].Contains("["))
                {
                    var sep = query[i].IndexOf('[');
                    tag = query[i].Substring(0, sep);
                    idx = int.Parse(query[i].Substring(sep + 1, query[i].IndexOf(']') - sep - 1));
                }
                else
                {
                    tag = query[i];
                }

                var children = GetChildNodes(elem);
                if (children.Count == 0) return InitDictionary();
                elem = InitDictionary();

                int j = 0;
                foreach (var c in children.ToArray())
                {
                    if (tag == GetNodeName(c.DataDictionary))
                    {
                        if (j == idx)
                        {
                            elem = c.DataDictionary;
                            break;
                        }
                        j++;
                    }
                }
            }
            return elem;
        }
        public static string GetNodeName(DataDictionary elem)
        {
            DataToken res;
            if (elem.TryGetValue("tag", TokenType.String, out res))
                return (string)res;
            else
                return "";
        }
        public static DataDictionary GetAttributes(DataDictionary elem)
        {
            DataToken res;
            if (elem.TryGetValue("attribute", TokenType.DataDictionary, out res))
                return (DataDictionary)res;
            else
                return new DataDictionary();
        }
        public static DataList GetChildNodes(DataDictionary elem)
        {
            DataToken res;
            if (elem.TryGetValue("children", TokenType.DataList, out res))
                return (DataList)res;
            else
                return new DataList();
        }
        public static string GetText(DataDictionary elem)
        {
            DataToken res;
            if (GetChildNodes(elem).TryGetValue(0, TokenType.String, out res))
                return (string)res;
            else
                return "";
        }
        public static DataDictionary Parse(string input)
        {
            DataList path = new DataList();

            path.Add(InitDictionary());

            int head = 0;
            bool inCdataSection = false;
            string data = "";

            DataDictionary state = new DataDictionary();
            state.Add("path", path);
            state.Add("head", head);
            state.Add("inCdataSection", inCdataSection);
            state.Add("data", data);

            while (head < input.Length)
            {
                state = _Parse(state, path, input, head, inCdataSection, data);

                DataToken token;

                state.TryGetValue("head", TokenType.Int, out token);
                head = (int)token;

                state.TryGetValue("inCdataSection", TokenType.Boolean, out token);
                inCdataSection = (bool)token;

                state.TryGetValue("data", TokenType.String, out token);
                data = (string)token;

                state.TryGetValue("path", TokenType.DataList, out token);
                path = (DataList)token;
            }

            DataToken tmp;
            path.TryGetValue(0, TokenType.DataDictionary, out tmp);
            return (DataDictionary)tmp;
        }

        private static DataDictionary _Parse(DataDictionary state, DataList path, string input, int head, bool inCdataSection, string data)
        {
            int tail = input.IndexOfAny(new char[] { '<', '>' }, head);
            if (tail == -1)
                tail = input.Length - 1;

            if (inCdataSection)
                data += input.Substring(head - 1, tail - head + 1);
            else
                data = input.Substring(head, tail - head);

            if (!inCdataSection && data.StartsWith("![CDATA["))
            {
                inCdataSection = true;
                data = data.Substring("![CDATA[".Length);
            }

            if (inCdataSection && data.EndsWith("]]"))
            {
                inCdataSection = false;

                data = data.Substring(0, data.Length - "]]".Length);

                DataToken elem;
                path.TryGetValue(path.Count - 1, TokenType.DataDictionary, out elem);

                DataToken children;
                ((DataDictionary)elem).TryGetValue("children", TokenType.DataList, out children);

                ((DataList)children).Add(data);
                ((DataDictionary)elem).SetValue("children", children);

                path.SetValue(path.Count - 1, elem);
            }
            else if (!inCdataSection && data != "")
            {
                if (input[head - 1] == '<')
                {
                    if (!data.StartsWith("/"))
                    {
                        var selfClosing = false;

                        if (data.EndsWith("/") || data.StartsWith("?xml "))
                        {
                            data = data.Substring(0, data.Length - 1);
                            selfClosing = true;
                        }
                        if (data.StartsWith("!"))
                        {
                            selfClosing = true;
                        }

                        var elem = new DataDictionary();
                        var t = data.Split(new char[] { ' ', '\n' });

                        elem.Add("tag", t[0]);

                        // Debug.Log(pathToStr(path) + "/" + t[0]);

                        var attr = new DataDictionary();

                        for (int i = 1; i < t.Length; i++) //TODO: スペースが混じってる場合について ("を見てフラグたててやっていき)
                        {
                            if (t[i].Trim() == "")
                                break;

                            var a = t[i].Split('=');
                            var key = a[0];
                            a[0] = "";
                            var val = System.String.Join("", a);

                            attr.Add(key, val);
                            // Debug.Log(pathToStr(path) + "/" + t[0] + "\t" + key + ":" + val);
                        }
                        elem.Add("attribute", attr);
                        elem.Add("children", new DataList());

                        if (selfClosing)
                        {
                            DataToken parent;
                            path.TryGetValue(path.Count - 1, TokenType.DataDictionary, out parent);

                            DataToken children;
                            ((DataDictionary)parent).TryGetValue("children", TokenType.DataList, out children);

                            ((DataList)children).Add(elem);
                            ((DataDictionary)parent).SetValue("children", children);

                            path.SetValue(path.Count - 1, parent);
                        }
                        else
                        {
                            path.Add(elem);
                        }
                    }
                    else if (data.StartsWith("/"))
                    {
                        DataToken elem, parent;
                        path.TryGetValue(path.Count - 1, TokenType.DataDictionary, out elem);
                        path.TryGetValue(path.Count - 2, TokenType.DataDictionary, out parent);

                        path.RemoveAt(path.Count - 1);

                        DataToken children;
                        ((DataDictionary)parent).TryGetValue("children", TokenType.DataList, out children);

                        ((DataList)children).Add(elem);
                        ((DataDictionary)parent).SetValue("children", children);

                        path.SetValue(path.Count - 1, parent);

                    }
                }
                else
                {
                    if (data.Trim() != "")
                    {
                        // Debug.Log(pathToStr(path) + "\t" + data);
                        DataToken elem;
                        path.TryGetValue(path.Count - 1, TokenType.DataDictionary, out elem);

                        DataToken children;
                        ((DataDictionary)elem).TryGetValue("children", TokenType.DataList, out children);

                        ((DataList)children).Add(data);
                        ((DataDictionary)elem).SetValue("children", children);

                        path.SetValue(path.Count - 1, elem);
                    }
                }
            }
            head = tail + 1;

            state.SetValue("head", head);
            state.SetValue("inCdataSection", inCdataSection);
            state.SetValue("data", data);
            state.SetValue("path", path);

            return state;
        }

        [RecursiveMethod]
        public static void Render(DataDictionary elem, int depth)
        {
            DataToken tag, attr, children;
            elem.TryGetValue("tag", TokenType.String, out tag);
            elem.TryGetValue("attribute", TokenType.DataDictionary, out attr);
            elem.TryGetValue("children", TokenType.DataList, out children);
            UnityEngine.Debug.Log(new string('\t', depth) + tag);

            foreach (var key in attr.DataDictionary.GetKeys().ToArray())
            {
                DataToken val;
                attr.DataDictionary.TryGetValue(key, out val);
                UnityEngine.Debug.Log(new string('\t', depth) + tag + "\t" + key + ": " + val);
            }

            foreach (var c in children.DataList.ToArray())
            {
                if (c.TokenType == TokenType.DataDictionary)
                    Render(((DataDictionary)c), depth + 1);
                else
                    UnityEngine.Debug.Log(new string('\t', depth + 1) + c);
            }
        }

        private static DataDictionary InitDictionary()
        {
            DataDictionary dict = new DataDictionary();
            dict.Add("tag", "");
            dict.Add("attribute", new DataDictionary());
            dict.Add("children", new DataList());
            return dict;
        }

        private string cb_input;
        private bool cb_ready;
        private int cb_head;
        private bool cb_inCdataSection;
        private XMLParser_Callback cb_target;
        private DataList cb_path;
        private string cb_data;
        private string cb_callbackId;

        public void ParseWithCallback(string input, XMLParser_Callback callback, string callbackId)
        {
            var parser = Instantiate(this.gameObject, this.transform);
            parser.transform.parent = this.transform;
            parser.GetComponent<XMLParser>()._ParseWithCallback(input, callback, callbackId);
        }

        protected void _ParseWithCallback(string input, XMLParser_Callback callback, string callbackId)
        {
            cb_path = new DataList();
            cb_path.Add(XMLParser.InitDictionary());
            cb_input = input;
            cb_head = 0;
            cb_inCdataSection = false;
            cb_target = callback;
            cb_callbackId = callbackId;

            cb_ready = true;
        }

        void Update()
        {
            if (cb_ready)
            {
                cb_target.OnXMLParseIteration(cb_head, cb_input.Length);
                DataDictionary state = new DataDictionary();
                state.Add("path", cb_path);
                state.Add("head", cb_head);
                state.Add("inCdataSection", cb_inCdataSection);
                state.Add("data", cb_data);

                var time = UnityEngine.Time.realtimeSinceStartup;
                while (UnityEngine.Time.realtimeSinceStartup - time < 0.01F)
                {
                    if (cb_head < cb_input.Length)
                    {
                        state = XMLParser._Parse(state, cb_path, cb_input, cb_head, cb_inCdataSection, cb_data);

                        DataToken token;

                        state.TryGetValue("head", TokenType.Int, out token);
                        cb_head = (int)token;

                        state.TryGetValue("inCdataSection", TokenType.Boolean, out token);
                        cb_inCdataSection = (bool)token;

                        state.TryGetValue("data", TokenType.String, out token);
                        cb_data = (string)token;

                        state.TryGetValue("path", TokenType.DataList, out token);
                        cb_path = (DataList)token;
                    }
                    else
                    {
                        DataToken tmp;
                        cb_path.TryGetValue(0, TokenType.DataDictionary, out tmp);
                        cb_target.OnXMLParseEnd((DataDictionary)tmp, cb_callbackId);
                        Destroy(this.gameObject);
                        return;
                    }
                }
            }
        }

        static string pathToStr(DataList path)
        {
            string res = "";
            foreach (var i in path.ToArray())
            {
                DataToken tag;
                if (i.TokenType == TokenType.DataDictionary)
                {
                    ((DataDictionary)i).TryGetValue("tag", out tag);
                    res += (string)tag + "/";
                }
            }
            return res;
        }
    }
}