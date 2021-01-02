using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityInjector.Attributes;
using PluginExt;
using System.Collections;
using System.Data;
using System.Text;
using static COM3D2.ScriplayLilly2.Plugin.ScriplayPlugin;
using UnityEngine.UI;
namespace COM3D2.ScriplayLilly2.Plugin
{
    /// <summary>
    /// Interpreterパターン
    /// メイドさんの制御とインタラクティブなゲーム進行制御
    /// </summary>
    public class ScriplayContextVer01 : ScriplayContext
    {
        private ScriplayContextVer01(string scriptName, bool finished = false) : base(scriptName,finished)
        {
        }
        /// <summary>
        /// 初期化用Nullオブジェクト
        /// </summary>
        public static ScriplayContext None = new ScriplayContextVer01(" - ", finished: true);
        /// <summary>
        /// ラベル名：行番号、ジャンプ時に使用
        /// </summary>
        IDictionary<string, int> labelMap = new Dictionary<string, int>();
        /// <summary>
        /// スクリプト全文
        /// </summary>
        string[] scriptArray = new string[0];
        private float waitSecond = 0f;
        /// <summary>
        /// 選択肢待ち時間
        /// </summary>
        private float selection_waitSecond = 0f;
        /// <summary>
        /// talk　発言終わるまで待ち
        /// key:maidNo, value:発言待ちか否か
        /// </summary>
        public Dictionary<int, bool> talk_waitUntilFinishSpeekingDict = new Dictionary<int, bool>();
        private float showText_waitTime = -1f;
        static Regex reg_comment = new Regex(@"^//.+", RegexOptions.IgnoreCase);     // //...　コメントアウト（を明示。解釈できない行はコメント同様実行されない）
        static Regex reg_label = new Regex(@"^#+\s*(.+)", RegexOptions.IgnoreCase);      //#...　ジャンプ先ラベル
        static Regex reg_require = new Regex(@"^@require\s+(.+)", RegexOptions.IgnoreCase);   //@require maidNum=2           //スクリプトの実行条件　（未実装：manNum=1）
        static Regex reg_auto = new Regex(@"^@auto\s+(.+)", RegexOptions.IgnoreCase);   //@auto auto1=じっくり auto2=ふつう             //autoモードの定義 auto1~9まで
        static Regex reg_posRelative = new Regex(@"^@posRelative\s+(.+)", RegexOptions.IgnoreCase);   //@posRelative x=1 y=1 z=1       //メイド位置を相対位置で指定
        static Regex reg_posAbsolute = new Regex(@"^@posAbsolute\s+(.+)", RegexOptions.IgnoreCase);   //@posAbsolute x=1 y=1 z=1       //メイド位置を絶対位置で指定
        static Regex reg_rotRelative = new Regex(@"^@rotRelative\s+(.+)", RegexOptions.IgnoreCase);   //@rotRelative x=1 y=1 z=1       //メイド向きを相対角度（度）で指定
        static Regex reg_rotAbsolute = new Regex(@"^@rotAbsolute\s+(.+)", RegexOptions.IgnoreCase);   //@rotAbsolute x=1 y=1 z=1       //メイド向きを相対角度（度）で指定
        static Regex reg_sound = new Regex(@"^@sound\s+(.+)", RegexOptions.IgnoreCase);    //@sound name=xxx   / @sound category=xxx     //SE再生 name=stopで再生停止
        static Regex reg_motion = new Regex(@"^@motion\s+(.+)", RegexOptions.IgnoreCase);    //@motion name=xxx   / @motion category=xxx     //モーション指定
        static Regex reg_face = new Regex(@"^@face\s+(.+)", RegexOptions.IgnoreCase);        //@face maid=0 name=エロ我慢 頬=0 涙=0 よだれ=1 /@face category=xxx       //表情指定    hoho=0 namida=0 yodare=1 も可, hoho/namida:0~3,yodare:0~1
        static Regex reg_wait = new Regex(@"^@wait\s+(.+)", RegexOptions.IgnoreCase);        //@wait 3sec                         //n秒待ち
        static Regex reg_goto = new Regex(@"^@goto\s+(.+)", RegexOptions.IgnoreCase);        //@goto （ラベル名）                  //ラベルへジャンプ
        static Regex reg_show = new Regex(@"^@show\s+(.+)", RegexOptions.IgnoreCase);        //@show text=（表示文字列） wait=3s                 //テキストを表示
        static Regex reg_talk = new Regex(@"^@talk\s+(.+)", RegexOptions.IgnoreCase);        //@talk name=xxx finish=1 /@talk category=絶頂１                      //oncevoice発話, nameなしor name=stopで停止
        static Regex reg_talkRepeat = new Regex(@"^@talkrepeat\s+(.+)", RegexOptions.IgnoreCase);        //@talkRepeat name=xxx     //loopvoice設定, nameなしor name=stopで停止
        static Regex reg_selection = new Regex(@"^@selection\s*([^\s]+)?", RegexOptions.IgnoreCase);   //@selection wait=3sec...    //選択肢開始
        static Regex reg_selectionItem = new Regex(@"[-]\s+([^\s]+)\s+(.+)", RegexOptions.IgnoreCase);   //- 選択肢名 goto=ジャンプ先ラベル (auto1Name)=90   //選択肢項目
        static Regex reg_eyeToCam = new Regex(@"^@eyeToCam\s+(.+)", RegexOptions.IgnoreCase);    //@eyeToCam mode=no/auto/yes             //目線をカメラへ向けるか
        static Regex reg_headToCam = new Regex(@"^@headToCam\s+(.+)", RegexOptions.IgnoreCase);    //@headToCam mode=no/auto/yes fade=1sec            //目線をカメラへ向けるか
        /// <summary>
        /// スクリプト終了時の処理
        /// </summary>
        protected override void tearDown()
        {
            selection_selectionList.Clear();
            foreach (IMaid m in maidList)
            {
                m.change_stopVoice();
            }
            change_SE("");
        }
        /// <summary>
        /// 本バージョンに対応したScriptContextのインスタンスを作成
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="scriptArray"></param>
        /// <returns></returns>
        public static ScriplayContextVer01 createScriplayContext(string scriptName, string[] scriptArray)
        {
            ScriplayContextVer01 ret = new ScriplayContextVer01(scriptName);
            List<string> list = new List<string>(scriptArray);
            list.Insert(0, "");                 //スクリプトの行番号とcurrentExecuteLineを合わせるためにダミー行挿入
            scriptArray = list.ToArray();
            ret.scriptArray = scriptArray;
            //構文解析
            for (int i = 0; i < ret.scriptArray.Length; i++)
            {
                string line = ret.scriptArray[i];
                if (reg_label.IsMatch(line))
                {
                    //ラベルがあればlabelMapに追加
                    Match matched = reg_label.Match(line);
                    string labelStr = matched.Groups[1].Value;    //1 origin
                    ret.labelMap.Add(labelStr, i);
                }
            }
            return ret;
        }
        /// <summary>
        /// 毎フレームスクリプトを実行
        /// 空白行などは読み飛ばして、１フレームにつき1コマンド実行
        /// </summary>
        public override void Update()
        {
            //最低実行条件：メイド１人以上表示中
            if (maidList.Count == 0) return;
            if (waitSecond > 0f)
            {
                //@wait　で待ちの場合
                waitSecond -= Time.deltaTime;
                return;
            }
            if (showText_waitTime > 0f)
            {
                //@show で待ちの場合
                showText_waitTime -= Time.deltaTime;
                if (showText_waitTime < 0f)
                {
                    showText = "";  //表示を解除
                }
                return;
            }
            if (selection_waitSecond > 0f)
            {
                //選択肢待ちの場合 선택 기다리는 경우
                selection_waitSecond -= Time.deltaTime;
                if (selection_waitSecond < 0f)
                {
                    //時間切れ
                    selection_selectionList = new List<Selection>();
                    return; //次フレームからスクリプト処理開始 다음 프레임에서 스크립트 처리 시작
                }
                if (selection_selectedItem != Selection.None)
                {
                    //選択あり 선택 있습니다
                    exec_goto(selection_selectedItem.paramLabel);
                    selection_waitSecond = -1;
                    selection_selectedItem = Selection.None;
                    selection_selectionList.Clear();
                    return; //次フレームからスクリプト処理開始 다음 프레임에서 스크립트 처리 시작
                }
                else
                {
                    //選択されるまで待つ 선택 될 때까지 기다린다
                    return;
                }
            }
            List<int> talk_wait_removeKeyList = new List<int>();
            foreach (KeyValuePair<int, bool> kvp in talk_waitUntilFinishSpeekingDict)
            {
                //@talkの発言待ち
                int maidNo = kvp.Key;
                bool isWaiting = kvp.Value;
                if (isWaiting)
                {
                    if (!(maidList[maidNo].getPlayingVoiceState() == IMaid.PlayingVoiceState.None))
                    {
                        return; //発言終わるまで待ち
                    }
                    else
                    {
                        talk_wait_removeKeyList.Add(maidNo);        //発言終わったら待ちを解除
                    }
                }
            }
            foreach (int i in talk_wait_removeKeyList)
            {
                talk_waitUntilFinishSpeekingDict.Remove(i);
            }
            //スクリプト1行ずつ実行
            while (!scriptFinished)
            {
                currentExecuteLine++;
                if (currentExecuteLine >= scriptArray.Length)
                {
                    //スクリプト終了
                    this.scriptFinished = true;
                    Util.Info(string.Format("すべてのスクリプトを実行しました. 行数：{0},{1}", currentExecuteLine.ToString(), scriptName));
                    return;
                }
                string line = scriptArray[currentExecuteLine];
                //対象行の解釈
                if (reg_comment.IsMatch(line))
                {
                    continue;
                }
                else if (reg_label.IsMatch(line))
                {
                    continue;
                }
                else if (reg_scriptInfo.IsMatch(line))
                {
                    continue;
                }
                else if (reg_require.IsMatch(line))
                {
                    exec_require(parseParameter(reg_require, line));
                    return;
                }
                else if (reg_auto.IsMatch(line))
                {
                    var paramDict = parseParameter(reg_auto, line);
                    for (int i = 1; i < 10; i++)
                    {
                        string key = "auto" + i.ToString();
                        if (paramDict.ContainsKey(key))
                        {
                            autoModeList.Add(paramDict[key]);
                        }
                    }
                }
                else if (reg_posAbsolute.IsMatch(line))
                {
                    exec_posAbsolute(parseParameter(reg_posAbsolute, line));
                    return;
                }
                else if (reg_posRelative.IsMatch(line))
                {
                    exec_posRelative(parseParameter(reg_posRelative, line));
                    return;
                }
                else if (reg_rotAbsolute.IsMatch(line))
                {
                    exec_rotAbsolute(parseParameter(reg_rotAbsolute, line));
                    return;
                }
                else if (reg_rotRelative.IsMatch(line))
                {
                    exec_rotRelative(parseParameter(reg_rotRelative, line));
                    return;
                }
                else if (reg_show.IsMatch(line))
                {
                    exec_show(parseParameter(reg_show, line));
                    return;
                }
                else if (reg_sound.IsMatch(line))
                {
                    exec_sound(parseParameter(reg_sound, line));
                    return;
                }
                else if (reg_motion.IsMatch(line))
                {
                    exec_motion(parseParameter(reg_motion, line));
                    return;
                }
                else if (reg_face.IsMatch(line))
                {
                    exec_face(parseParameter(reg_face, line));
                    return;
                }
                else if (reg_wait.IsMatch(line))
                {
                    Match matched = reg_wait.Match(line);
                    string waitStr = matched.Groups[1].Value;
                    selection_waitSecond = parseFloat(waitStr, suffix: new string[] { "sec", "s" });
                    return;
                }
                else if (reg_goto.IsMatch(line))
                {
                    //goto　-------------------------------------
                    Match matched = reg_goto.Match(line);
                    string gotoLabel = matched.Groups[1].Value;
                    exec_goto(gotoLabel);
                }
                else if (reg_talk.IsMatch(line))
                {
                    //talk　-------------------------------------
                    exec_talk(parseParameter(reg_talk, line), lineNo: currentExecuteLine);
                    return;
                }
                else if (reg_talkRepeat.IsMatch(line))
                {
                    //talkrepeat　-------------------------------------
                    exec_talk(parseParameter(reg_talkRepeat, line), loop: true, lineNo: currentExecuteLine);
                    return;
                }
                else if (reg_eyeToCam.IsMatch(line))
                {
                    exec_eyeToCam(parseParameter(reg_eyeToCam, line));
                    return;
                }
                else if (reg_headToCam.IsMatch(line))
                {
                    exec_headToCam(parseParameter(reg_headToCam, line));
                    return;
                }
                else if (reg_selection.IsMatch(line))
                {
                    //選択肢-------------------------------------
                    var paramDict = parseParameter(reg_selection, line);
                    if (paramDict.ContainsKey(key_wait))
                    {
                        selection_waitSecond = parseFloat(paramDict[key_wait], suffix: new string[] { "sec", "s" });
                    }
                    else
                    {
                        //待ち時間 指定ないときは表示したままにする
                        selection_waitSecond = 60 * 60 * 24 * 365f;
                    }
                    //各選択肢を読む
                    while (true)
                    {
                        //次の行が選択肢でなければ終了
                        int nextLine = currentExecuteLine + 1;
                        if (nextLine >= scriptArray.Length || !reg_selectionItem.IsMatch(scriptArray[nextLine])) break;
                        //次の行へ進んで選択肢追加処理
                        currentExecuteLine++;
                        line = scriptArray[currentExecuteLine];
                        Match matched = reg_selectionItem.Match(line);
                        string itemStr = matched.Groups[1].Value;
                        string paramStr = matched.Groups[2].Value;
                        paramDict = parseParameter(paramStr);
                        addSelection(itemStr, paramDict);
                    }
                    //次フレームから選択肢待ち
                    return;
                }
                else if (line == "")
                {
                    continue;
                }
                else
                {
                    //解釈できない行は読み飛ばし
                    Util.Info(string.Format("解釈できませんでした：{0}:{1}", currentExecuteLine.ToString(), line));
                }
            }
        }
        private void exec_eyeToCam(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : eyeToCam ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                IMaid.EyeHeadToCamState state = IMaid.EyeHeadToCamState.Auto;
                if (paramDict.ContainsKey(key_mode))
                {
                    string mode = paramDict[key_mode].ToLower();
                    if (mode == IMaid.EyeHeadToCamState.Auto.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Auto;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.Yes.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Yes;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.No.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.No;
                    }
                    else
                    {
                        Util.Info(string.Format("line{0} : モード指定が不適切です모드 지정이 잘못되었습니다:{1}", currentExecuteLine.ToString(), mode));
                        continue;
                    }
                }
                else
                {
                    Util.Info(string.Format("line{0} : モードが指定されていません", currentExecuteLine.ToString()));
                }
                maid.change_eyeToCam(state);
            }
        }
        private void exec_headToCam(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : headToCam ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                IMaid.EyeHeadToCamState state = IMaid.EyeHeadToCamState.Auto;
                float fadeSec = -1f;
                if (paramDict.ContainsKey(key_mode))
                {
                    string mode = paramDict[key_mode].ToLower();
                    if (mode == IMaid.EyeHeadToCamState.Auto.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Auto;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.Yes.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.Yes;
                    }
                    else if (mode == IMaid.EyeHeadToCamState.No.viewStr)
                    {
                        state = IMaid.EyeHeadToCamState.No;
                    }
                    else
                    {
                        Util.Info(string.Format("line{0} : モード指定が不適切です모드 지정이 잘못되었습니다:{1}", currentExecuteLine.ToString(), mode));
                        continue;
                    }
                }
                else
                {
                    Util.Info(string.Format("line{0} : モードが指定されていません", currentExecuteLine.ToString()));
                }
                if (paramDict.ContainsKey(key_fade))
                {
                    fadeSec = parseFloat(paramDict[key_fade], new string[] { "sec", "s" });
                    maid.change_headToCam(state, fadeSec: fadeSec);
                    return;
                }
                else
                {
                    maid.change_headToCam(state);
                    return;
                }
            }
        }
        /// <summary>
        /// コマンドのパラメータ文字列を解釈して辞書を返す
        /// ex.
        /// @command key1=value1...
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="lineStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(Regex reg, string lineStr)
        {
            string paramStr = "";
            if (!reg.IsMatch(lineStr)) return new Dictionary<string, string>();
            paramStr = reg.Match(lineStr).Groups[1].Value;
            return parseParameter(paramStr);
        }
        /// <summary>
        /// コマンドのパラメータ文字列を解釈して辞書を返す
        /// パラメータ形式：
        /// ex. key1=value1 key2=value2
        /// </summary>
        /// <param name="paramStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(string paramStr)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            paramStr = parseParameter_regex.Replace(paramStr, " "); //複数かもしれない空白文字を１つへ
            paramStr = parseParameter_regex_header.Replace(paramStr, ""); //先頭空白除去
            paramStr = parseParameter_regex_footer.Replace(paramStr, ""); //後方空白除去
            string[] ss = paramStr.Split(' ');
            foreach (string s in ss)
            {
                string[] kv = s.Split('=');
                if (kv.Length != 2)
                {
                    Util.Info(string.Format("line{0} : パラメータを読み込めませんでした。「key=value」形式になっていますか？ : {1}", currentExecuteLine.ToString(), s));
                    continue;
                }
                ret.Add(kv[0], kv[1]);
            }
            return ret;
        }
        static Regex parseParameter_regex = new Regex(@"\s+");
        static Regex parseParameter_regex_header = new Regex(@"^\s+");
        static Regex parseParameter_regex_footer = new Regex(@"\s+$");
        private List<string> autoModeList = new List<string>();
        /// <summary>
        /// 数値文字列を解釈。除去する接尾辞（単位 sec,sなど）を除去順に列挙のこと
        /// だめならログ出力
        /// </summary>
        /// <param name="floatStr"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private float parseFloat(string floatStr, string[] suffix = null)
        {
            float ret = -1;
            if (suffix != null)
            {
                floatStr = floatStr.ToLower();
                foreach (string s in suffix)
                {
                    floatStr = floatStr.Replace(s, "");
                }
            }
            try
            {
                ret = float.Parse(floatStr);
            }
            catch (Exception e)
            {
                Util.Info(string.Format("line{0} : 数値を読み込めませんでした : {1}", currentExecuteLine.ToString(), floatStr));
                Util.Debug(e.StackTrace);
            }
            return ret;
        }
        private void exec_sound(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : sound ", currentExecuteLine.ToString()));
            if (paramDict.ContainsKey(key_name))
            {
                string name = paramDict[key_name];
                if (name == "stop") name = "";
                change_SE(name);
            }
            else
            {
                //nameパラメータなしなら再生停止
                change_SE("");
            }
        }
        private void exec_require(Dictionary<string, string> paramDict)
        {
            if (paramDict.ContainsKey(key_maidNum))
            {
                int maidNum = (int)parseFloat(paramDict[key_maidNum]);
                if (maidList.Count < maidNum)
                {
                    string mes = string.Format("メイドさんが{0}人以上必要です", maidNum);
                    toast(mes);
                    Util.Info(mes);
                    scriptFinished = true;
                    return;
                }
            }
            //if (paramDict.ContainsKey(key_manNum))
            //{
            //    int manNum = (int)parseFloat(paramDict[key_manNum]);
            //    if (manList.Count < manNum)
            //    {
            //        string mes = string.Format("ご主人様が{0}人以上必要です", manNum);
            //        toast(mes);
            //        Util.info(mes);
            //        scriptFinished = true;
            //        return;
            //    }
            //}
        }
        private void exec_motion(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : motion ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                if (paramDict.ContainsKey(key_name))
                {
                    maid.change_Motion(paramDict[key_name], isLoop: true);
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    List<MotionInfo> motionList = MotionTable.queryTable_motionNameBase(paramDict[key_category]);
                    maid.change_Motion(motionList, isLoop: true);
                }
                else
                {
                    Util.Info(string.Format("line{0} : 모션이 지정되어 있지 않습니다", currentExecuteLine.ToString()));
                }
            }
        }
        private void exec_face(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : face ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                float fadeTime = -1f;
                if (paramDict.ContainsKey(key_fade)) fadeTime = parseFloat(paramDict[key_fade], new string[] { "sec", "s" });
                if (paramDict.ContainsKey(key_name))
                {
                    string name = paramDict[key_name];
                    if (fadeTime == -1f)
                    {
                        maid.change_faceAnime(name);
                    }
                    else
                    {
                        maid.change_faceAnime(name, fadeTime);
                    }
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    List<string> faceList = FaceTable.queryTable(paramDict[key_category]);
                    if (fadeTime == -1f)
                    {
                        maid.change_faceAnime(faceList);
                    }
                    else
                    {
                        maid.change_faceAnime(faceList, fadeTime);
                    }
                }
                int hoho = -1;
                int namida = -1;
                bool yodare = false;
                if (paramDict.ContainsKey(key_namida) || paramDict.ContainsKey(key_涙))
                {
                    if (paramDict.ContainsKey(key_namida)) namida = (int)parseFloat(paramDict[key_namida]);
                    if (paramDict.ContainsKey(key_涙)) namida = (int)parseFloat(paramDict[key_涙]);
                    if (namida < 0 || namida > 3)
                    {
                        Util.Info(string.Format("line{0} : 涙の値は0~3である必要があります。強制的に0にします。", currentExecuteLine.ToString()));
                        namida = 0;
                    }
                }
                if (paramDict.ContainsKey(key_hoho) || paramDict.ContainsKey(key_頬))
                {
                    if (paramDict.ContainsKey(key_hoho)) hoho = (int)parseFloat(paramDict[key_hoho]);
                    if (paramDict.ContainsKey(key_頬)) hoho = (int)parseFloat(paramDict[key_頬]);
                    if (hoho < 0 || hoho > 3)
                    {
                        Util.Info(string.Format("line{0} : 頬の値は0~3である必要があります。強制的に0にします。", currentExecuteLine.ToString()));
                        hoho = 0;
                    }
                }
                if (paramDict.ContainsKey(key_yodare) || paramDict.ContainsKey(key_よだれ))
                {
                    int yodareInt = -1;
                    if (paramDict.ContainsKey(key_yodare)) yodareInt = (int)parseFloat(paramDict[key_yodare]);
                    if (paramDict.ContainsKey(key_頬)) yodareInt = (int)parseFloat(paramDict[key_頬]);
                    if (yodareInt == 1) yodare = true;
                    maid.change_FaceBlend(enableYodare: yodare);
                }
                maid.change_FaceBlend(hoho: hoho, namida: namida, enableYodare: yodare);
            }
        }
        private void exec_posRelative(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_positionRelative(x, y, z);
            }
        }
        private void exec_rotRelative(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_angleRelative(x, y, z);
            }
        }
        private void exec_posAbsolute(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    keepX = false;
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    keepY = false;
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    keepZ = false;
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_positionAbsolute(x, y, z, keepX, keepY, keepZ);
            }
        }
        private void exec_rotAbsolute(Dictionary<string, string> paramDict)
        {
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                bool keepX = true;
                bool keepY = true;
                bool keepZ = true;
                float x = 0;
                float y = 0;
                float z = 0;
                if (paramDict.ContainsKey(key_x))
                {
                    keepX = false;
                    x = parseFloat(paramDict[key_x]);
                }
                if (paramDict.ContainsKey(key_y))
                {
                    keepY = false;
                    y = parseFloat(paramDict[key_y]);
                }
                if (paramDict.ContainsKey(key_z))
                {
                    keepZ = false;
                    z = parseFloat(paramDict[key_z]);
                }
                maid.change_angleAbsolute(x, y, z, keepX, keepY, keepZ);
            }
        }
        const string key_maid = "maid";
        const string key_name = "name";
        const string key_category = "category";
        const string key_mode = "mode";
        const string key_fade = "fade";
        const string key_wait = "wait";
        const string key_finish = "finish";
        const string key_goto = "goto";
        const string key_maidNum = "maidNum";
        const string key_manNum = "manNum";
        const string key_text = "text";
        const string key_hoho = "hoho";
        const string key_namida = "namida";
        const string key_yodare = "yodare";
        const string key_頬 = "頬";
        const string key_涙 = "涙";
        const string key_よだれ = "よだれ";
        const string key_x = "x";
        const string key_y = "y";
        const string key_z = "z";
        private void exec_show(Dictionary<string, string> paramDict, int lineNo = -1)
        {
            Util.Debug(string.Format("line{0} : show ", currentExecuteLine.ToString()));
            if (paramDict.ContainsKey(key_text))
            {
                this.showText = paramDict[key_text];
            }
            else
            {
                Util.Info(string.Format("line{0} : 表示するテキストが見つかりません", currentExecuteLine.ToString()));
                return;
            }
            if (paramDict.ContainsKey(key_wait))
            {
                this.showText_waitTime = parseFloat(paramDict[key_wait], new string[] { "sec", "s" });
            }
            else
            {
                //文字数から自動計算　10文字1秒、最小で1秒
                this.showText_waitTime = ((float)this.showText.Length) / 10f;
                this.showText_waitTime = Math.Max(showText_waitTime, 1f);
            }
        }
        /// <summary>
        /// talk/talkrepeat　コマンドの実行
        /// </summary>
        /// <param name="paramStr"></param>
        /// <param name="loop"></param>
        /// <param name="lineNo"></param>
        private void exec_talk(Dictionary<string, string> paramDict, bool loop = false, int lineNo = -1)
        {
            Util.Debug(string.Format("line{0} : talk ", currentExecuteLine.ToString()));
            List<IMaid> maidList = selectMaid(paramDict);
            foreach (IMaid maid in maidList)
            {
                List<string> voiceList = new List<string>();
                if (paramDict.ContainsKey(key_finish))
                {
                    if (paramDict[key_finish] == "1") talk_waitUntilFinishSpeekingDict[maid.maidNo] = true;
                }
                if (paramDict.ContainsKey(key_name))
                {
                    voiceList.Add(paramDict[key_name]);
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    string voiceCategory = paramDict[key_category];
                    List<VoiceTable.VoiceInfo> tmp = new List<VoiceTable.VoiceInfo>();
                    if (loop)
                    {
                        tmp = LoopVoiceTable.queryTable(maid.sPersonal, voiceCategory);
                    }
                    else
                    {
                        tmp= OnceVoiceTable.queryTable(maid.sPersonal, voiceCategory);
                    }
                    foreach(VoiceTable.VoiceInfo v in tmp)
                    {
                        voiceList.Add(v.filename);
                    }
                    if (voiceList.Count == 0)
                    {
                        Util.Info(string.Format("line{0} : カテゴリのボイスが見つかりません。カテゴリ：{1}", currentExecuteLine.ToString(), voiceCategory));
                        return;
                    }
                }
                if (voiceList.Count == 0 | (voiceList.Count == 1 && voiceList[0].ToLower().Equals("stop")))
                {
                    //nameパラメータなしor name=stop　の場合は音声停止
                    maid.change_stopVoice();
                }
                if (loop)
                {
                    maid.change_LoopVoice(voiceList);
                }
                else
                {
                    maid.change_onceVoice(voiceList);
                }
            }
        }
        /// <summary>
        /// 指定されたmaidNoのIMaidを返す
        /// 指定なき場合はmaidNo.0のIMaidを返す
        /// </summary>
        /// <param name="paramDict"></param>
        /// <returns></returns>
        private List<IMaid> selectMaid(Dictionary<string, string> paramDict)
        {
            List<IMaid> ret = new List<IMaid>();
            if (paramDict.ContainsKey(key_maid))
            {
                int maidNum = int.Parse(paramDict[key_maid]);
                if (maidNum < ScriplayPlugin.maidList.Count)
                {
                    ret.Add(ScriplayPlugin.maidList[maidNum]);
                }
                else
                {
                    Util.Info(string.Format("メイドは{0}人しか有効にしていません。maidNo.{1}は無効です", ScriplayPlugin.maidList.Count, maidNum));
                }
            }
            else
            {
                ret = new List<IMaid>(ScriplayPlugin.maidList);
            }
            return ret;
        }
        /// <summary>
        /// gotoコマンドの実行
        /// 指定したラベルに対応した行へジャンプ
        /// </summary>
        /// <param name="gotoLabel"></param>
        private void exec_goto(string gotoLabel)
        {
            if (!labelMap.ContainsKey(gotoLabel))
            {
                Util.Info(string.Format("line{0} : ジャンプ先ラベルが見つかりません。ジャンプ先：{1}", currentExecuteLine.ToString(), gotoLabel));
                scriptFinished = true;
            }
            currentExecuteLine = labelMap[gotoLabel];
            Util.Debug(string.Format("line{0} : 「{1}」へジャンプしました", currentExecuteLine.ToString(), gotoLabel));
        }
        /// <summary>
        /// 選択肢項目を追加
        /// </summary>
        /// <param name="itemViewStr"></param>
        public void addSelection(string itemViewStr, Dictionary<string, string> dict)
        {
            string gotoLabel = "";
            if (dict.ContainsKey(key_goto)) gotoLabel = dict[key_goto];
            //上記以外のキーはautomode名として解釈
            Dictionary<string, int> autoProbDict = new Dictionary<string, int>();
            foreach (string key in dict.Keys)
            {
                if (key.Equals(key_goto)) continue;
                string auto_name = key;
                int auto_prob;
                try
                {
                    auto_prob = int.Parse(dict[key].Replace("%", ""));
                    autoProbDict.Add(auto_name, auto_prob);
                }
                catch (Exception e)
                {
                    Util.Info(string.Format("選択肢　自動選択確率の読み込みに失敗 : {0}, {1}, {2} \r\n {3}", itemViewStr, key, dict[key], e.StackTrace));
                }
            }
            selection_selectionList.Add(new Selection(itemViewStr, gotoLabel, Selection.CommandType.GOTO));
            Util.Debug(string.Format("選択肢「{0}」を追加", itemViewStr));
        }
    }
}
