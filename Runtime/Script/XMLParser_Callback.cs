
using UdonSharp;

using VRC.SDK3.Data;

namespace nekomimiStudio.parser.xml
{
    public class XMLParser_Callback : UdonSharpBehaviour // "Interface"
    {

        virtual public void OnXMLParseEnd(DataDictionary result, string callbackId) { }
        virtual public void OnXMLParseIteration(int processing, int total, string callbackId) { }

    }
}