# XML Parser

-   UdonSharp で XML をパースするライブラリ
-   `ParseAsync()` を利用する場合は
    -   `UdonSharpBehaviour` でなく `XMLParser_Callback` を継承する必要がある
    -   `ParseAsync()` 内で `Instantiate(this.gameObject)` と `Destroy(this.gameObject)` を呼ぶので、それ以外のスクリプトは**アタッチしないこと**
-   それ以外はすべて static なメソッド

## Function

-   「XML オブジェクト」: このライブラリでパースしたオブジェクトのこと 型は DataDictionary
-   `class XMLParser`
    -   `float frameLimit`
        -   `ParseAsync()` が 1 回の `Update()` 内で使ってよい最大の秒数
        -   `Parse()` には関係しない
    -   `static DataDictionary Parse(string xml)`
        -   | 引数 |                       |
            | ---- | --------------------- |
            | xml  | パースする XML データ |
        -   | 返り値 |                      |
            | ------ | -------------------- |
            | 成功時 | 「XML オブジェクト」 |
    -   `void ParseAsync(string xml, XMLParser_Callback callback, string callbackId)`
        -   この函数のみ **static ではない**
        -   内部で `Instantiate(this.gameObject)` と `Destroy(this.gameObject)` を呼ぶので、XMLParser 以外のスクリプトは **アタッチしないこと**
        -   | 引数       |                                                                                     |
            | ---------- | ----------------------------------------------------------------------------------- |
            | xml        | パースする XML データ                                                               |
            | callback   | コールバック先 一般的には `this` を入れることになる                                 |
            | callbackId | `OnXMLParseIteration` / `OnXMLParseEnd` を呼ぶとき、`callbackId` に代入される文字列 |
    -   `static DataDictionary GetNodeByPath(DataDictionary root, string path)`
        -   | 引数 |                               |
            | ---- | ----------------------------- |
            | root | 「XML オブジェクト」          |
            | path | パス `/honi/moni[4]` のような |
        -   | 返り値                     |                          |
            | -------------------------- | ------------------------ |
            | 成功時                     | 「XML オブジェクト」     |
            | 該当するノードが存在しない | 空の「XML オブジェクト」 |
    -   `static string GetNodeName(DataDictionary elem)`
        -   | 引数 |                      |
            | ---- | -------------------- |
            | elem | 「XML オブジェクト」 |
        -   | 返り値 |                                     |
            | ------ | ----------------------------------- |
            | 成功時 | ノード名 ( `<honi>` なら `"honi"` ) |
    -   `static DataDictionary GetAttributes(DataDictionary elem)`
        -   | 引数 |                      |
            | ---- | -------------------- |
            | elem | 「XML オブジェクト」 |
        -   | 返り値 |                                                                                                 |
            | ------ | ----------------------------------------------------------------------------------------------- |
            | 成功時 | そのノードの Attributes ( `<honi moni="puni", kani>`なら `{ {"moni", "puni"}, {"kani", ""} }` ) |
    -   `static DataList GetChildNodes(DataDictionary elem)`
        -   | 引数 |                      |
            | ---- | -------------------- |
            | elem | 「XML オブジェクト」 |
        -   | 返り値 |                                               |
            | ------ | --------------------------------------------- |
            | 成功時 | elem の子である、「XML オブジェクト」のリスト |
    -   `static string GetText(DataDictionary elem)`
        -   | 引数 |                      |
            | ---- | -------------------- |
            | elem | 「XML オブジェクト」 |
        -   | 返り値 |                   |
            | ------ | ----------------- |
            | 成功時 | elem の子の文字列 |
    -   `static string Render(DataDictionary elem)`
        -   | 引数 |                      |
            | ---- | -------------------- |
            | elem | 「XML オブジェクト」 |
        -   | 返り値 |                                        |
            | ------ | -------------------------------------- |
            | 成功時 | XML の階層に従ってインデントした文字列 |
-   `class XMLParser_Callback`
    -   `void OnXMLParseIteration(int processing, int total, string callbackId)`
        -   パース中、`Update()` 内から呼ばれる
        -   | 引数       |                                     |
            | ---------- | ----------------------------------- |
            | processing | 処理中の場所                        |
            | total      | 全体の個数                          |
            | callbackId | `ParseAsync()`に指定した callbackId |
    -   `void OnXMLParseEnd(DataDictionary result, string callbackId)`
        -   パース終了時、`Update()` 内から呼ばれる
        -   | 引数       |                                     |
            | ---------- | ----------------------------------- |
            | result     | 「XML オブジェクト」                |
            | callbackId | `ParseAsync()`に指定した callbackId |

```csharp

using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using nekomimiStudio.parser.xml;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;

public class xmltest : XMLParser_Callback
{
    [SerializeField] VRCUrl src;
    [SerializeField] XMLParser parser;

    void Start()
    {
        VRCStringDownloader.LoadUrl(src, this.GetComponent<UdonBehaviour>());
    }

    public override void OnStringLoadSuccess(IVRCStringDownload res)
    {
        var result = XMLParser.Parse(res.Result);
        Debug.Log("/feed/entry[0]/content " + XMLParser.GetText(XMLParser.GetNodeByPath(result, "/feed/entry[0]/content")));

        parser.ParseAsync(res.Result, this, this.gameObject.GetInstanceID() + "_" + src);
    }

    public override void OnXMLParseEnd(DataDictionary result, string callbackId)
    {
        Debug.Log("/feed/entry[0]/content " + XMLParser.GetText(XMLParser.GetNodeByPath(result, "/feed/entry[0]/content")));
    }

    public override void OnXMLParseIteration(int processing, int total, string callbackId)
    {
        Debug.Log(callbackId + ": " + processing + "/" + total);
    }
}
```
