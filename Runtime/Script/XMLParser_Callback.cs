
using UdonSharp;

using VRC.SDK3.Data;

public class XMLParser_Callback : UdonSharpBehaviour // "Interface"
{

    virtual public void OnXMLParseEnd(DataDictionary result, string callbackId) { }
    virtual public void OnXMLParseIteration(int processing, int total) { }

}
