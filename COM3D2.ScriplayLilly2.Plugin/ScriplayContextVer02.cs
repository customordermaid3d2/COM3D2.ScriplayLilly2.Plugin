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
    public class ScriplayContextVer02 : ScriplayContext
    {
        private ScriplayContextVer02(string scriptName, bool finished = false, bool restoreProp_onTearDown = false) : base(scriptName, finished, restoreProp_onTearDown)
        {
        }
        /// <summary>
        /// 初期化用Nullオブジェクト
        /// </summary>
        public static ScriplayContext None = new ScriplayContextVer02(" - ", finished: true);
        /// <summary>
        /// ラベル名：行番号、ジャンプ時に使用
        /// </summary>
        IDictionary<string, int> labelMap = new Dictionary<string, int>();
        /// <summary>
        /// スクリプト全文
        /// </summary>
        string[] scriptArray = new string[0];
        /// <summary>
        /// コールスタック
        /// </summary>
        private Stack<StackInfo> callStack = new Stack<StackInfo>();
        public class StackInfo
        {
            public readonly int calledLineNo;
            public StackInfo(int lineNo)
            {
                this.calledLineNo = lineNo;
            }
        }
        /// <summary>
        /// 変数名:文字列値のマップ
        /// </summary>
        IDictionary<string, string> variableMap = new Dictionary<string, string>();
        private float waitSecond = 0f;
        /// <summary>
        /// 選択肢待ち時間
        /// </summary>
        private float selection_waitSecond = 0f;
        private int selection_callStackValue = 0;
        /// <summary>
        /// 選択肢　keepモード。選択肢を表示したままにして、選択されたらコマンドを実行
        /// </summary>
        private bool is_selectionKeep = false;
        /// <summary>
        /// talk　発言終わるまで待ち
        /// key:maidNo, value:発言待ちか否か
        /// </summary>
        public Dictionary<int, bool> talk_waitUntilFinishSpeekingDict = new Dictionary<int, bool>();
        public Dictionary<int, bool> motion_waitUntilFinishMovingDict = new Dictionary<int, bool>();
        private float showText_waitTime = -1f;
        //matcherも確認できる正規表現チェッカー
        //https://regex-testdrive.com/ja/dotest
        static Regex reg_comment = new Regex(@"^\s*//.+", RegexOptions.IgnoreCase);     // //...　コメントアウト（を明示。解釈できない行はコメント同様実行されない）
        static Regex reg_label = new Regex(@"^\s*#+\s*(.+)", RegexOptions.IgnoreCase);      //#...　ジャンプ先ラベル
        static Regex reg_multistatement = new Regex(@"(.+?);\s*(@.+)", RegexOptions.IgnoreCase);    //@sio maid=0;@face name=にっこり; @talk...    //;の後に@から始まることでマルチステートメントになる。
        static Regex reg_config = new Regex(@"^\s*@config\s+(.+)", RegexOptions.IgnoreCase);   //@config maid=0 seikaku=Pride restore=1 visible=1           //seikaku:メイドの性格を仮設定、スクリプトのvoiceテーブル検索などに反映される
                                                                                               //Pride/Cool/Pure/Muku/Majime/Rindere
                                                                                               //restore:スクリプト終了時にメイドのほぼすべてのPropを復元する。
                                                                                               //visible:1-表示、0-非表示
        static Regex reg_require = new Regex(@"^\s*@require\s+(.+)", RegexOptions.IgnoreCase);   //@require maidNum=2           //スクリプトの実行条件　（未実装：manNum=1）
        static Regex reg_pos = new Regex(@"^\s*@pos\s+(.+)", RegexOptions.IgnoreCase);   //@pos x=1 y=1 z=1 rx=0 ry=0 rz=0      //Scriplay座標系のXYZ値でメイド位置・向き（度）を指定、パラメータなしなら現状維持
        static Regex reg_move = new Regex(@"^\s*@move\s+(.+)", RegexOptions.IgnoreCase);   //@move x=1 y=1 z=1 rx=0 ry=0 rz=0      //メイド位置・向き（度）をScriplay座標系で移動、パラメータなしなら現状維持
        static Regex reg_origin = new Regex(@"^\s*@origin\s+(.+)", RegexOptions.IgnoreCase);   //@origin x=1 y=1 z=1 rx=0 ry=0 rz=0      //Scriplay座標の原点をワールド座標系で指定
                                                                                               //@origin maid=0            //Scriplay座標の原点をメイド位置０の位置・回転軸にセット
        static Regex reg_camera = new Regex(@"^\s*@camera\s+(.+)", RegexOptions.IgnoreCase);   //@camera maid=0 / @camera x=1m y=1m z=1m azimuth=0deg elevation=0deg d=4m      //カメラ位置をメイドの前へ / Scriplay座標系の位置・角度・距離で指定
        static Regex reg_fadein = new Regex(@"^\s*@fadein\s*(.*)", RegexOptions.IgnoreCase);   //@fadein/@fadeout fade=1s  //画面暗転解除 fade:移行時間 mode:out（暗転）/in(暗転解除）
        static Regex reg_fadeout = new Regex(@"^\s*@fadeout\s*(.*)", RegexOptions.IgnoreCase);
        static Regex reg_sound = new Regex(@"^\s*@sound\s+(.+)", RegexOptions.IgnoreCase);    //@sound name=xxx   / @sound category=xxx     //SE再生 name=stopで再生停止
        static Regex reg_background = new Regex(@"^\s*@background\s+(.+)", RegexOptions.IgnoreCase);    //@background name=xxx              //背景変更
                                                                                                        //name:Shitsumu_ChairRot/Shitsumu_ChairRot_Night/Salon/Syosai/Syosai_Night/DressRoom_NoMirror/MyBedRoom/MyBedRoom_Night/HoneymoonRoom/Bathroom/PlayRoom/PlayRoom2/Pool/SMRoom/SMRoom2/Salon_Garden/LargeBathRoom/OiranRoom/Penthouse/Town/Kitchen/Kitchen_Night/Salon_Entrance/poledancestage/Bar/Toilet/Soap/MaidRoom/
        static Regex reg_bgm = new Regex(@"^\s*@bgm\s+(.+)", RegexOptions.IgnoreCase);    //@bgm name=xxx //BGM変更
        static Regex reg_soundRepeat = new Regex(@"^\s*@soundRepeat\s+(.+)", RegexOptions.IgnoreCase);    //@soundRepeat name=xxx   / @sound category=xxx     //SEループ再生 name=stopで再生停止
        static Regex reg_show = new Regex(@"^\s*@show\s+(.+)", RegexOptions.IgnoreCase);        //@show text=（表示文字列） wait=3s                 //テキストを表示
        static Regex reg_wait = new Regex(@"^\s*@wait\s+(.+)", RegexOptions.IgnoreCase);        //@wait 1/3sec                         //1or3秒待ち
        static Regex reg_goto = new Regex(@"^\s*@goto\s+(.+)", RegexOptions.IgnoreCase);        //@goto （ラベル名１/ラベル名２）                  //ラベル１orラベル２へジャンプ
        static Regex reg_call = new Regex(@"^\s*@call\s+(.+)", RegexOptions.IgnoreCase);        //@call （ラベル名１/ラベル名２）                 //ラベル先を呼び出し、exitで戻ってくる
        static Regex reg_exit = new Regex(@"^\s*@exit(.*)", RegexOptions.IgnoreCase);        //@exit                                        //call呼び出しまで戻る、callスタック空ならスクリプト終了
        static Regex reg_motion = new Regex(@"^\s*@motion\s+(.+)", RegexOptions.IgnoreCase);    //@motion name=xxx   / @motion category=xxx speed=1.2 fade=0.5 similarSec=5s   //モーション指定 similar=5s:5秒ごとにモーション実行中に類似モーションへ変更
        static Regex reg_face = new Regex(@"^\s*@face\s+(.+)", RegexOptions.IgnoreCase);        //@face maid=0 name=エロ我慢 頬=0 涙=0 よだれ=1 eye=0.1  /@face category=xxx       //表情指定    hoho=0 namida=0 yodare=1 も可, hoho/namida:0~3,yodare:0~1,eye:0~1
        static Regex reg_talk = new Regex(@"^\s*@talk\s+(.+)", RegexOptions.IgnoreCase);        //@talk name=xxx start=1s interval=1s finish=1 /@talk category=絶頂１                      //oncevoice発話, nameなしor name=stopで停止, start再生位置, fade再生時間
        static Regex reg_talkRepeat = new Regex(@"^\s*@talkrepeat\s+(.+)", RegexOptions.IgnoreCase);        //@talkRepeat name=xxx     //loopvoice設定, nameなしor name=stopで停止
        static Regex reg_selection = new Regex(@"^\s*@selection\s*(.*)", RegexOptions.IgnoreCase);   //@selection wait=3sec keep=1 mode=gotolist ...    //選択肢開始 wait:表示しておく時間 keep:選択肢ボタンを有効にしたまま以降のコマンドを処理 mode:gotolist-ラベルリストを自動追加
        static Regex reg_selectionItem = new Regex(@"[-]\s+([^\s]+)\s+([^=]+)=(.+)", RegexOptions.IgnoreCase);   //- 選択肢名 goto=ジャンプ先ラベル / - 選択肢名 call=呼出先ラベル / - 選択肢名 exec=実行したいコマンド
        static Regex reg_eyeToCam = new Regex(@"^\s*@eyeToCam\s+(.+)", RegexOptions.IgnoreCase);    //@eyeToCam mode=no/auto/yes             //目線をカメラへ向けるか
        static Regex reg_headToCam = new Regex(@"^\s*@headToCam\s+(.+)", RegexOptions.IgnoreCase);    //@headToCam mode=no/auto/yes fade=1sec            //目線をカメラへ向けるか
        static Regex reg_shapekey = new Regex(@"^\s*@shapekey\s+(.+)", RegexOptions.IgnoreCase);    //@shapekey maid=0 name=xxx mode=fade val=0.1 fade=1s     //シェイプキー操作
                                                                                                    //@shapekey maid=0 name=xxx mode=sin min=0.1 max=0.3 period=1s　finish=5s       //シェイプキー操作、指定時間だけ正弦波振動
                                                                                                    //static Regex reg_nyo = new Regex(@"^\s*@nyo\s*([^\s]+)?", RegexOptions.IgnoreCase);    //@nyo maid=0    //尿
                                                                                                    //static Regex reg_sio = new Regex(@"^\s*@sio\s*([^\s]+)?", RegexOptions.IgnoreCase);    //@sio maid=0    //潮
        static Regex reg_setParticle = new Regex(@"^\s*@setparticle\s+(.+)", RegexOptions.IgnoreCase);    //@setParticle maid=0 name=toiki1                //パーティクル有効化
        static Regex reg_delParticle = new Regex(@"^\s*@delparticle\s+(.+)", RegexOptions.IgnoreCase);    //@delParticle maid=0 name=toiki1                //パーティクル有効化
                                                                                                          //name:toiki1/toiki2/aieki1/aieki2/aieki3/nyo/sio      
        static Regex reg_setSlot = new Regex(@"^\s*@setSlot\s+(.+)", RegexOptions.IgnoreCase);    //@setSlot maid=0 name=wear   //着衣    name:カテゴリ指定で一括ON/OFF可能　all/overwear/exceptacc
                                                                                                  //slot:body, head, eye, hairF, hairR, hairS, hairT, wear, skirt, onepiece, mizugi, panz, bra, stkg, shoes, headset, glove, accHead, hairAho, accHana, accHa, accKami_1_, accMiMiR, accKamiSubR, accNipR, HandItemR, accKubi, accKubiwa, accHeso, accUde, accAshi, accSenaka, accShippo, accAnl, accVag, kubiwa, megane, accXXX, chinko, chikubi, accHat, kousoku_upper, kousoku_lower, seieki_naka, seieki_hara, seieki_face, seieki_mune, seieki_hip, seieki_ude, seieki_ashi, accNipL, accMiMiL, accKamiSubL, accKami_2_, accKami_3_, HandItemL, underhair, moza, end, 
        static Regex reg_delSlot = new Regex(@"^\s*@delSlot\s+(.+)", RegexOptions.IgnoreCase);    //@delSlot maid=0 name=wear   //脱衣    name:カテゴリ指定で一括ON/OFF可能　all/overwear/exceptacc
        static Regex reg_setProp = new Regex(@"^\s*@setProp\s+(.+)", RegexOptions.IgnoreCase);    //@setProp maid=0 name=dress217_glove_i_ part=glove  //prop（服、アクセサリ、道具、髪などオブジェクト全般）を装着, name:装着するpropのファイル名, part:mpn名
                                                                                                  //part:MuneL, MuneS, MuneTare, RegFat, ArmL, Hara, RegMeet, KubiScl, UdeScl, EyeScl, EyeSclX, EyeSclY, EyePosX, EyePosY, EyeClose, EyeBallPosX, EyeBallPosY, EyeBallSclX, EyeBallSclY, EarNone, EarElf, EarRot, EarScl, NosePos, NoseScl, FaceShape, FaceShapeSlim, MayuShapeIn, MayuShapeOut, MayuX, MayuY, MayuRot, HeadX, HeadY, DouPer, sintyou, koshi, kata, west, MuneUpDown, MuneYori, MuneYawaraka, body, moza, head, hairf, hairr, hairt, hairs, hairaho, haircolor, skin, acctatoo, accnail, underhair, hokuro, mayu, lip, eye, eye_hi, eye_hi_r, chikubi, chikubicolor, eyewhite, nose, facegloss, wear, skirt, mizugi, bra, panz, stkg, shoes, headset, glove, acchead, accha, acchana, acckamisub, acckami, accmimi, accnip, acckubi, acckubiwa, accheso, accude, accashi, accsenaka, accshippo, accanl, accvag, megane, accxxx, handitem, acchat, onepiece, set_maidwear, set_mywear, set_underwear, set_body, folder_eye, folder_mayu, folder_underhair, folder_skin, folder_eyewhite, kousoku_upper, kousoku_lower, seieki_naka, seieki_hara, seieki_face, seieki_mune, seieki_hip, seieki_ude, seieki_ashi,
        static Regex reg_delProp = new Regex(@"^\s*@delProp\s+(.+)", RegexOptions.IgnoreCase);    //@delProp maid=0 name=dress217_glove_i_   //propを除去
                                                                                                  //nameは*_i_.menuファイル名で指定。ファイル名にMPN（装着部位）の名前を含む場合、自動的に検出して対象箇所にpropがセットされる。ファイル名にMPNが含まれないときは、partパラメータでMPNを指定すること。
        static Regex reg_defineVariable = new Regex(@"^\s*\$([^=]+)\s*=\s*(.+)", RegexOptions.IgnoreCase);    //$maidNum=0/1 変数定義（0か1どっちかランダムに代入）。変数は文字列として格納、先頭・末尾の半角スペースは除去、/はどっちかの文字列として解釈。
        static Regex reg_replaceVariable = new Regex(@"\$\{([^}]+)\}", RegexOptions.IgnoreCase);    //変数書き方１　　${変数名}　実行時は、行内の変数がなくなるまで展開を繰り返す
        static Regex reg_replaceVariable2 = new Regex(@"\$([^\s]+)(\s)", RegexOptions.IgnoreCase);    //変数書き方２　$変数名（空白文字）
        /// <summary>
        /// スクリプト終了時の処理
        /// </summary>
        protected override void tearDown()
        {
            selection_selectionList.Clear();
            change_SE("");
            foreach (IMaid m in maidList)
            {
                m.change_stopVoice();
                if (restoreProp_onTearDown) { m.prop_snapshot(); m.prop_restore(); }
            }
        }
        /// <summary>
        /// 本バージョンに対応したScriptContextのインスタンスを作成
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="scriptArray"></param>
        /// <returns></returns>
        public static ScriplayContextVer02 createScriplayContext(string scriptName, string[] scriptArray, bool restoreProp_onTearDown = false)
        {
            ScriplayContextVer02 ret = new ScriplayContextVer02(scriptName, restoreProp_onTearDown: restoreProp_onTearDown);
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
                    if (ret.labelMap.ContainsKey(labelStr))
                    {
                        Util.Info(string.Format("line:{0} スクリプト解析エラー：ラベル「{1}」が複数あります。スクリプトを中断します。", i.ToString(), labelStr));
                        ret.scriptFinished = true;
                        return ret;
                    }
                    ret.labelMap.Add(labelStr, i);
                }
            }
            foreach (String label in ret.labelMap.Keys) { if (!label.StartsWith(cfg.initRoutinePrefix)) continue; ret.exec_call(label); }
            return ret;
        }
        /// <summary>
        /// 毎フレームスクリプトを実行
        /// 空白行などは読み飛ばして、１フレームにつき1コマンド実行
        /// </summary>
        public override void Update()
        {
            bool waiting = false;
            //最低実行条件：メイド１人以上表示中
            //if (maidList.Count == 0) return;
            if (waitSecond > 0f)
            {
                //@wait　で待ちの場合
                waitSecond -= Time.deltaTime;
                waiting = true;
            }
            if (showText_waitTime > 0f)
            {
                //@show で待ちの場合
                showText_waitTime -= Time.deltaTime;
                if (showText_waitTime < 0f)
                {
                    showText = "";  //表示を解除
                }
                waiting = true;
            }
            if (selection_waitSecond > 0f)
            {
                waiting = true;
                //選択肢待ちの場合
                selection_waitSecond -= Time.deltaTime;
                if (selection_waitSecond < 0f)
                {
                    //時間切れ
                    init_selection();
                }
                if (_is_selection_selected()) _exec_selection();
            }
            List<int> talk_wait_removeKeyList = new List<int>();
            foreach (KeyValuePair<int, bool> kvp in talk_waitUntilFinishSpeekingDict)
            {
                //@talkの発言待ち
                int maidNo = kvp.Key;
                bool isWaiting = kvp.Value;
                if (isWaiting)
                {
                    waiting = true;
                    if (!(maidList[maidNo].getPlayingVoiceState() == IMaid.PlayingVoiceState.None)) { }//発言終わるまで待ち
                    else { talk_wait_removeKeyList.Add(maidNo); }//発言終わったら待ちを解除
                }
            }
            foreach (int i in talk_wait_removeKeyList)
            {
                talk_waitUntilFinishSpeekingDict.Remove(i);
            }
            List<int> motion_wait_removeKeyList = new List<int>();
            foreach (KeyValuePair<int, bool> kvp in motion_waitUntilFinishMovingDict)
            {
                //@motion　終了待ち
                int maidNo = kvp.Key;
                bool isWaiting = kvp.Value;
                if (isWaiting)
                {
                    waiting = true;
                    if (maidList[maidNo].maid.body0.m_Animation.isPlaying) { }//motion終わるまで待ち
                    else { motion_wait_removeKeyList.Add(maidNo); }//発言終わったら待ちを解除
                }
            }
            foreach (int i in motion_wait_removeKeyList)
            {
                motion_waitUntilFinishMovingDict.Remove(i);
            }
            if (is_selectionKeep && waiting && _is_selection_selected())
            {
                while (selection_callStackValue < callStack.Count)
                {
                    exec_exit();
                }
                //is_selectionKeep = false;  //選択肢実行後も同一動作の選択肢を表示し続ける。
                _exec_selection();
                waitSecond = -1; showText_waitTime = -1; selection_waitSecond = -1;
                talk_waitUntilFinishSpeekingDict.Clear();
                motion_wait_removeKeyList.Clear();
            }
            if (waiting) return;
            //스크립트 한줄 씩 실행
            while (!scriptFinished)
            {
                currentExecuteLine++;
                if (currentExecuteLine >= scriptArray.Length)
                {
                    //스크립트 종료
                    this.scriptFinished = true;
                    Util.Info(string.Format("모든 스크립트를 실행했습니다. 行数：{0},{1}", currentExecuteLine.ToString(), scriptName));
                    return;
                }
                string line = scriptArray[currentExecuteLine];
                if (execLine_if_command(line)) return;
            }
        }
        /// <summary>
        /// 라인별 정규식에따라 변수 입력 처리
        /// </summary>
        /// <param name="allLine"></param>
        /// <returns></returns>
        private bool execLine_if_command(string allLine)
        {
            bool commandExecuted = false;
            Stack<string> s = new Stack<string>();
            string line = "";
            string nextParse = allLine;
            while (!nextParse.Equals(""))
            {
                if (reg_multistatement.IsMatch(nextParse))
                {
                    Match m = reg_multistatement.Match(nextParse);
                    line = m.Groups[1].Value;       //멀티 스테이트먼트 검증 정규 표현식은 전방 일치。홍보는 아직 멀티 스테이트먼트 남아있을 수 있음。
                    nextParse = m.Groups[2].Value;
                }
                else
                {
                    line = nextParse;
                    nextParse = "";
                }
                //변수 확장
                line = expand_variable(line);
                //대상 행의 해석
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
                else if (reg_defineVariable.IsMatch(line))
                {
                    Match matched = reg_defineVariable.Match(line);
                    string varName = parseSingleParameter(matched.Groups[1].Value);
                    string value = parseSingleParameter(matched.Groups[2].Value);
                    variableMap[varName] = value;
                    continue;
                }
                else if (reg_require.IsMatch(line))
                {
                    exec_require(parseParameter(reg_require, line));
                    commandExecuted = true; continue;
                }
                else if (reg_config.IsMatch(line))
                {
                    exec_config(parseParameter(reg_config, line));
                    commandExecuted = true; continue;
                }
                else if (reg_origin.IsMatch(line))
                {
                    exec_origin(parseParameter(reg_origin, line));
                    commandExecuted = true; continue;
                }
                else if (reg_pos.IsMatch(line))
                {
                    exec_pos(parseParameter(reg_pos, line));
                    commandExecuted = true; continue;
                }
                else if (reg_move.IsMatch(line))
                {
                    exec_move(parseParameter(reg_move, line));
                    commandExecuted = true; continue;
                }
                else if (reg_camera.IsMatch(line))
                {
                    exec_camera(parseParameter(reg_camera, line));
                    commandExecuted = true; continue;
                }
                else if (reg_fadein.IsMatch(line))
                {
                    exec_fade(parseParameter(reg_fadein, line), true);
                    commandExecuted = true; continue;
                }
                else if (reg_fadeout.IsMatch(line))
                {
                    exec_fade(parseParameter(reg_fadeout, line), false);
                    commandExecuted = true; continue;
                }
                else if (reg_show.IsMatch(line))
                {
                    exec_show(parseParameter(reg_show, line));
                    commandExecuted = true; continue;
                }
                else if (reg_bgm.IsMatch(line))
                {
                    exec_bgm(parseParameter(reg_bgm, line));
                    commandExecuted = true; continue;
                }
                else if (reg_sound.IsMatch(line))
                {
                    exec_sound(parseParameter(reg_sound, line), false);
                    commandExecuted = true; continue;
                }
                else if (reg_soundRepeat.IsMatch(line))
                {
                    exec_sound(parseParameter(reg_soundRepeat, line), true);
                    commandExecuted = true; continue;
                }
                else if (reg_background.IsMatch(line))
                {
                    exec_background(parseParameter(reg_background, line));
                    commandExecuted = true; continue;
                }
                else if (reg_motion.IsMatch(line))
                {
                    exec_motion(parseParameter(reg_motion, line));
                    commandExecuted = true; continue;
                }
                else if (reg_face.IsMatch(line))
                {
                    exec_face(parseParameter(reg_face, line));
                    commandExecuted = true; continue;
                }
                else if (reg_wait.IsMatch(line))
                {
                    Match matched = reg_wait.Match(line);
                    string waitStr = parseSingleParameter(matched.Groups[1].Value);
                    selection_waitSecond = parseFloat(waitStr, suffix: new string[] { "sec", "s" });
                    commandExecuted = true; continue;
                }
                else if (reg_goto.IsMatch(line))
                {
                    //goto　-------------------------------------
                    Match matched = reg_goto.Match(line);
                    // /区切り対応
                    string gotoLabel = parseSingleParameter(matched.Groups[1].Value);
                    exec_goto(gotoLabel);
                }
                else if (reg_call.IsMatch(line))
                {
                    //goto　-------------------------------------
                    Match matched = reg_call.Match(line);
                    // /区切り対応
                    string callLabel = parseSingleParameter(matched.Groups[1].Value);
                    exec_call(callLabel);
                }
                else if (reg_exit.IsMatch(line))
                {
                    exec_exit();
                    commandExecuted = true; continue;
                }
                else if (reg_talk.IsMatch(line))
                {
                    //talk　-------------------------------------
                    exec_talk(parseParameter(reg_talk, line), lineNo: currentExecuteLine);
                    commandExecuted = true; continue;
                }
                else if (reg_talkRepeat.IsMatch(line))
                {
                    //talkrepeat　-------------------------------------
                    exec_talk(parseParameter(reg_talkRepeat, line), loop: true, lineNo: currentExecuteLine);
                    commandExecuted = true; continue;
                }
                else if (reg_eyeToCam.IsMatch(line))
                {
                    exec_eyeToCam(parseParameter(reg_eyeToCam, line));
                    commandExecuted = true; continue;
                }
                else if (reg_headToCam.IsMatch(line))
                {
                    exec_headToCam(parseParameter(reg_headToCam, line));
                    commandExecuted = true; continue;
                }
                else if (reg_shapekey.IsMatch(line))
                {
                    exec_shapeKey(parseParameter(reg_shapekey, line));
                    commandExecuted = true; continue;
                }
                else if (reg_setParticle.IsMatch(line))
                {
                    exec_particle(parseParameter(reg_setParticle, line), true);
                    commandExecuted = true; continue;
                }
                else if (reg_delParticle.IsMatch(line))
                {
                    exec_particle(parseParameter(reg_delParticle, line), false);
                    commandExecuted = true; continue;
                }
                else if (reg_setSlot.IsMatch(line))
                {
                    exec_slot(parseParameter(reg_setSlot, line), true);
                    commandExecuted = true; continue;
                }
                else if (reg_delSlot.IsMatch(line))
                {
                    exec_slot(parseParameter(reg_delSlot, line), false);
                    commandExecuted = true; continue;
                }
                else if (reg_setProp.IsMatch(line))
                {
                    exec_prop(parseParameter(reg_setProp, line), true);
                    commandExecuted = true; continue;
                }
                else if (reg_delProp.IsMatch(line))
                {
                    exec_prop(parseParameter(reg_delProp, line), false);
                    commandExecuted = true; continue;
                }
                //else if (reg_nyo.IsMatch(line))
                //{
                //    exec_nyo(parseParameter(reg_nyo, line));
                //    commandExecuted = true; continue;
                //}
                //else if (reg_sio.IsMatch(line))
                //{
                //    exec_sio(parseParameter(reg_sio, line));
                //    commandExecuted = true; continue;
                //}
                else if (reg_selection.IsMatch(line))
                {
                    //選択肢-------------------------------------
                    var paramDict = parseParameter(reg_selection, line);
                    init_selection(true);
                    if (paramDict.ContainsKey(key_wait))
                    {
                        selection_waitSecond = parseFloat(parseSingleParameter(paramDict[key_wait]), suffix: new string[] { "sec", "s" });
                    }
                    else
                    {
                        //待ち時間 指定ないときは表示したままにする
                        selection_waitSecond = 60 * 60 * 24 * 365f;
                    }
                    if (paramDict.ContainsKey(key_keep))
                    {
                        int enable = (int)parseFloat(paramDict[key_keep]);
                        if (enable == 1)
                        {
                            selection_callStackValue = callStack.Count;
                            is_selectionKeep = true;
                            if (!paramDict.ContainsKey(key_wait)) selection_waitSecond = 0f;  //待ち時間指定ないときはすぐ次の行を解釈へ
                        }
                        else
                        {
                            is_selectionKeep = false;
                        }
                    }
                    //각 옵션보기
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
                        string typeStr = matched.Groups[2].Value;
                        string paramStr = matched.Groups[3].Value;
                        Dictionary<string, string> selectionItem_paramDict = new Dictionary<string, string>();
                        selectionItem_paramDict.Add(typeStr, paramStr);
                        addSelection(itemStr, selectionItem_paramDict);
                    }
                    //gotolist　그렇다면 선택 추기
                    if (paramDict.ContainsKey(key_mode) && paramDict[key_mode].ToLower().Equals(mode_gotolist))
                    {
                        foreach (string label in labelMap.Keys)
                        {
                            Dictionary<string, string> selectionItem_paramDict = new Dictionary<string, string>();
                            selectionItem_paramDict.Add("goto", label);
                            addSelection(label, selectionItem_paramDict);
                        }
                    }
                    //次フレームから選択肢待ち
                    commandExecuted = true; continue;
                }
                else if (line == "")
                {
                    continue;
                }
                //解釈できない行は読み飛ばし
                info(string.Format("解釈できませんでした：{0}", line));
            }
            return commandExecuted;
        }
        /// <summary>
        /// 행 변수를 확장하고 확장 된 문자열을 반환한다.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string expand_variable(string line)
        {
            while (reg_replaceVariable.IsMatch(line) | reg_replaceVariable2.IsMatch(line))
            {
                if (reg_replaceVariable.IsMatch(line))
                {
                    Match matched = reg_replaceVariable.Match(line);
                    string varName = parseSingleParameter(matched.Groups[1].Value);
                    if (!variableMap.ContainsKey(varName))
                    {
                        info(string.Format("変数名がみつかりません:{0}", varName));
                        break;
                    }
                    line = reg_replaceVariable.Replace(line, variableMap[varName], 1);      //一致箇所1回だけ置換
                }
                else
                {
                    Match matched = reg_replaceVariable2.Match(line);
                    string varName = parseSingleParameter(matched.Groups[1].Value);
                    string endBlankCharacter = parseSingleParameter(matched.Groups[2].Value);
                    if (!variableMap.ContainsKey(varName))
                    {
                        info(string.Format("変数名がみつかりません:{0}", varName));
                        break;
                    }
                    Util.Debug(variableMap[varName] + " " + endBlankCharacter);
                    line = reg_replaceVariable2.Replace(line, variableMap[varName] + " " + endBlankCharacter, 1);      //一致箇所1回だけ置換
                }
            }
            return line;
        }
        private bool _is_selection_selected()
        {
            return (selection_selectedItem != Selection.None);
        }
        /// <summary>
        /// 選択肢が選択されていたら、処理をしてtrueを返す。
        /// 옵션이 선택되어 있으면, 처리를하고 true를 반환한다.
        /// </summary>
        /// <returns>true if selected</returns>
        private void _exec_selection()
        {
            string selectedItemLabel = expand_variable(selection_selectedItem.paramLabel);
            Selection.CommandType itemType = selection_selectedItem.itemType;
            init_selection();
            //いずれか選択されたとき
            //하나 선택되었을 때
            if (itemType == Selection.CommandType.GOTO) { exec_goto(selectedItemLabel); }
            else if (itemType == Selection.CommandType.CALL) { exec_call(selectedItemLabel); }
            else if (itemType == Selection.CommandType.EXEC) { execLine_if_command(selectedItemLabel); }
            else { throw new NotImplementedException(); }
        }
        private void init_selection(bool force_clearList = false)
        {
            selection_waitSecond = -1;
            selection_selectedItem = Selection.None;
            if (!is_selectionKeep | force_clearList) selection_selectionList.Clear();
        }
        private void exec_config(Dictionary<string, string> paramDict)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            string seikaku = "";
            if (paramDict.ContainsKey(key_seikaku))
                foreach (IMaid maid in charaList) { maid.setPersonal(paramDict[key_seikaku]); }
            //else { info(string.Format("seikakuパラメータがみつかりませんでした")); }
            if (paramDict.ContainsKey(key_visible))
                foreach (IMaid maid in charaList) { maid.change_visible(paramDict[key_visible] == "1"); }
            if (paramDict.ContainsKey(key_restore))
            {
                if (paramDict[key_restore] == "1")
                {
                    this.restoreProp_onTearDown = true;
                    Util.Info("スクリプト終了時のメイドProp復元を有効にしました。");
                }
                else
                {
                    this.restoreProp_onTearDown = false;
                    Util.Info("スクリプト終了時のメイドProp復元を無効にしました。");
                }
            }
        }
        //private void exec_nyo(Dictionary<string, string> paramDict)
        //{
        //    List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
        //    foreach (IMaid maid in charaList)
        //    {
        //        maid.change_nyo();
        //    }
        //}
        //private void exec_sio(Dictionary<string, string> paramDict)
        //{
        //    List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
        //    foreach (IMaid maid in charaList)
        //    {
        //        maid.change_sio();
        //    }
        //}
        private void exec_slot(Dictionary<string, string> paramDict, bool visible)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
            {
                string name = "";
                if (paramDict.ContainsKey(key_name)) { name = paramDict[key_name]; }
                else { info("slot:nameパラメータが見つかりませんでした"); continue; }
                maid.change_slot(name, visible);
                info(string.Format("slot:{0}を{1}にしました", name, visible ? "表示" : "非表示"));
            }
        }
        private void exec_prop(Dictionary<string, string> paramDict, bool visible)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
            {
                string propFilename = "";
                string mpnName = "";
                if (paramDict.ContainsKey(key_name)) { propFilename = paramDict[key_name]; }
                else { info("prop:nameパラメータが見つかりませんでした"); continue; }
                if (paramDict.ContainsKey(key_part)) { mpnName = paramDict[key_part]; }
                if (visible) maid.change_setProp(propFilename, mpnName);
                else
                {
                    if (mpnName != "") maid.change_delProp_byMPNName(mpnName);
                    else maid.change_delProp(propFilename);
                }
                info(string.Format("prop:{0}を{1}しました", propFilename, visible ? "装着" : "除去"));
            }
        }
        private void exec_particle(Dictionary<string, string> paramDict, bool enable)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            string name = "";
            if (paramDict.ContainsKey(key_name)) { name = paramDict[key_name]; }
            else { info("particle:nameパラメータが見つかりませんでした"); return; }
            foreach (IMaid maid in charaList)
            {
                if (!enable && name.ToLower().Equals("all")) { maid.change_removeAllPrefab(); }
                else
                {
                    switch (name.ToLower())
                    {
                        case "toiki1": maid.change_toiki1(enable); break;
                        case "toiki2": maid.change_toiki2(enable); break;
                        case "aieki1": maid.change_aieki1(enable); break;
                        case "aieki2": maid.change_aieki2(enable); break;
                        case "aieki3": maid.change_aieki3(enable); break;
                        case "sio": maid.change_sio(enable); break;
                        case "nyo": maid.change_nyo(enable); break;
                        default: info(string.Format("particle:{0}　は処理できませんでした。名前は間違っていませんか？", name)); return;
                    }
                }
                info(string.Format("particle:{0}を{1}にしました", name, enable ? "有効" : "無効"));
            }
        }
        private void exec_shapeKey(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : shapeKey ", currentExecuteLine.ToString()));
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            string sTag = "";
            if (paramDict.ContainsKey(key_name)) { sTag = paramDict[key_name]; }
            else { info(string.Format("nameパラメータに操作対象のシェイプキー名が必要です。")); return; }
            //動作モード、すべて小文字とする
            string mode = "";
            if (paramDict.ContainsKey(key_mode)) { mode = paramDict[key_mode].ToLower(); }
            else { info("modeパラメータがありません。fadeとして解釈します。"); mode = "fade"; }
            if (mode == "fade")
            {
                float fadeSec = 0f;
                if (paramDict.ContainsKey(key_fade)) { fadeSec = parseFloat(paramDict[key_fade], new string[] { "sec", "s" }); }
                else { info("fadeパラメータがありません。fade=0sec として解釈します。"); }
                if (!paramDict.ContainsKey(key_val)) { info(string.Format("valパラメータがありません。shapekeyコマンドは実行しません。")); return; }
                float val = parseFloat(paramDict[key_val]);
                foreach (IMaid maid in charaList) { maid.change_shapekey(sTag, val, fadeSec); }
            }
            else
            {
                float finishTime = 60f;
                if (paramDict.ContainsKey(key_finish)) { finishTime = parseFloat(paramDict[key_finish], new string[] { "sec", "s" }); }
                else { info("finishパラメータがありません。finish=60sec として解釈します。"); }
                if (!paramDict.ContainsKey(key_max)) { info(string.Format("maxパラメータがありません。shapekeyコマンドは実行しません。")); return; }
                if (!paramDict.ContainsKey(key_min)) { info(string.Format("minパラメータがありません。shapekeyコマンドは実行しません。")); return; }
                if (!paramDict.ContainsKey(key_period)) { info(string.Format("periodパラメータがありません。shapekeyコマンドは実行しません。")); return; }
                float max = parseFloat(paramDict[key_max]);
                float min = parseFloat(paramDict[key_min]);
                float period = parseFloat(paramDict[key_period], new string[] { "sec", "s" });
                foreach (IMaid maid in charaList)
                {
                    if (mode == "sin") { maid.change_shapekey_likeSin(sTag, max, min, period, finishTime); }
                    else if (mode == "triangle") { maid.change_shapekey_likeTriangle(sTag, max, min, period, animationSec: finishTime); }
                    else if (mode == "keiren") { maid.change_shapekey_likeKeiren(sTag, max, min, period, animationSec: finishTime); }
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
                        info(string.Format("モード指定が不適切です모드 지정이 잘못되었습니다:{0}", mode));
                        continue;
                    }
                }
                else
                {
                    info("モードが指定されていません");
                }
                maid.change_eyeToCam(state);
            }
        }
        private void exec_headToCam(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : headToCam ", currentExecuteLine.ToString()));
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
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
                        info(string.Format("モード指定が不適切です모드 지정이 잘못되었습니다:{0}", mode));
                        continue;
                    }
                }
                else
                {
                    info("モードが指定されていません");
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
        /// コマンドの単一パラメータを解釈して返す
        /// /区切りならランダムに１つ選択する
        /// 
        /// ex.
        /// @goto xxx/yyy
        /// のxxx/yyyを解釈して、xxxを返す
        /// </summary>
        /// <param name="singleParamStr"></param>
        /// <returns></returns>
        private string parseSingleParameter(string singleParamStr)
        {
            singleParamStr = parseParameter_regex.Replace(singleParamStr, " "); //複数かもしれない空白文字を１つへ
            singleParamStr = parseParameter_regex_header.Replace(singleParamStr, ""); //先頭空白除去
            singleParamStr = parseParameter_regex_footer.Replace(singleParamStr, ""); //後方空白除去
            // /区切りの値は、ランダムに１つ選択
            string[] values = singleParamStr.Split('/');
            return Util.pickOneOrEmptyString(new List<string>(values));
        }
        /// <summary>
        /// 명령의 매개 변수 문자열을 해석하고 사전을 반환
        /// ex.
        /// @command key1=value1...
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="lineStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(Regex reg, string lineStr)
        {
            string paramStr = "";
            if (!reg.IsMatch(lineStr))
            {
                Util.Debug(string.Format("line:{0} parseParameter 인수 형식이 잘못되었습니다.。　入力：{1}, パターン:{2}", currentExecuteLine, lineStr, reg.ToString()));
                return new Dictionary<string, string>();
            }
            paramStr = reg.Match(lineStr).Groups[1].Value;
            return parseParameter(paramStr);
        }
        /// <summary>
        /// 명령의 매개 변수 문자열을 해석하고 사전을 반환
        /// パラメータ形式：
        /// ex. key1=value1 key2=value2
        /// </summary>
        /// <param name="paramStr"></param>
        /// <returns></returns>
        private Dictionary<string, string> parseParameter(string paramStr)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            paramStr = parseParameter_regex.Replace(paramStr, " "); //여러지도 모른다 공백을 1 개에
            paramStr = parseParameter_regex_header.Replace(paramStr, ""); //先頭空白除去
            paramStr = parseParameter_regex_footer.Replace(paramStr, ""); //後方空白除去
            string[] ss = paramStr.Split(' ');
            foreach (string s in ss)
            {
                string[] kv = s.Split('=');
                if (kv.Length != 2)
                {
                    info(string.Format("매개 변수를 읽을 수 없습니다。「key=value」형식으로되어 있습니까?？ : {0}", s));
                    continue;
                }
                // /구분 값은 임의로 하나 선택
                string[] values = kv[1].Split('/');
                string v = Util.pickOneOrEmptyString(new List<string>(values));
                ret.Add(kv[0].ToLower(), v);
            }
            Util.Debug(string.Format("line{0} : parseParameter成功 : {1}", currentExecuteLine.ToString(), Util.list2Str(ret)));
            return ret;
        }
        static Regex parseParameter_regex = new Regex(@"\s+");
        static Regex parseParameter_regex_header = new Regex(@"^\s+");
        static Regex parseParameter_regex_footer = new Regex(@"\s+$");
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
                info(string.Format("数値を読み込めませんでした : {0}", floatStr));
                Util.Debug(e.StackTrace);
            }
            return ret;
        }
        private void exec_background(Dictionary<string, string> paramDict)
        {
            if (paramDict.ContainsKey(key_name))
            {
                string name = paramDict[key_name];
                change_BackGround(name);
                info(string.Format("background:背景を「{0}」に変更しました", bgDict[name]));
            }
            else
            {
                info("background:nameパラメータが見つかりませんでした。");
            }
        }
        private void exec_bgm(Dictionary<string, string> paramDict)
        {
            if (paramDict.ContainsKey(key_name))
            {
                string name = paramDict[key_name];
                change_BGM(name);
                info(string.Format("BGM:BGMを「{0}」に変更しました", name));
            }
            else
            {
                info("BGM:nameパラメータが見つかりませんでした。");
            }
        }
        private void exec_sound(Dictionary<string, string> paramDict, bool loop)
        {
            Util.Debug(string.Format("line{0} : sound ", currentExecuteLine.ToString()));
            if (paramDict.ContainsKey(key_name))
            {
                string name = paramDict[key_name];
                if (name.ToLower() == "stop") name = "";
                switch (name.ToLower())
                {
                    case "vibe_low": name = "se020.ogg"; break;
                    case "vibe_high": name = "se019.ogg"; break;
                    case "kuchu_low": name = "se028.ogg"; break;
                    case "kuchu_high": name = "se029.ogg"; break;
                    case "slap_low": name = "se012.ogg"; break;
                    case "slap_high": name = "se013.ogg"; break;
                }
                Util.Debug("@sound name:" + name);
                change_SE(name, loop);
            }
            else
            {
                //nameパラメータなしなら再生停止
                change_SE("", loop);
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
                    info(mes);
                    scriptFinished = true;
                    return;
                }
            }
            if (paramDict.ContainsKey(key_manNum))
            {
                int manNum = (int)parseFloat(paramDict[key_manNum]);
                if (manList.Count < manNum)
                {
                    string mes = string.Format("ご主人様が{0}人以上必要です", manNum);
                    toast(mes);
                    info(mes);
                    scriptFinished = true;
                    return;
                }
            }
        }
        private void exec_motion(Dictionary<string, string> paramDict)
        {
            Util.Debug(string.Format("line{0} : motion ", currentExecuteLine.ToString()));
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
            {
                float speed = -1;
                float afterSpeed = -1;
                float fade = -1;
                float similarSec = -1;
                bool loop = true;
                if (paramDict.ContainsKey(key_finish))
                {
                    if (paramDict[key_finish] == "1") motion_waitUntilFinishMovingDict[maid.maidNo] = true;
                    loop = false;//1 회 재생 종료 대기 할 때 루프하지
                }
                if (paramDict.ContainsKey(key_speed)) { speed = parseFloat(paramDict[key_speed]); }
                if (paramDict.ContainsKey(key_afterSpeed)) { afterSpeed = parseFloat(paramDict[key_afterSpeed]); }
                if (paramDict.ContainsKey(key_fade)) { fade = parseFloat(paramDict[key_fade], new string[] { "sec", "s" }); }
                if (paramDict.ContainsKey(key_similar)) { if (paramDict[key_similar] == "1") similarSec = 0; }
                if (paramDict.ContainsKey(key_similarSec)) { similarSec = parseFloat(paramDict[key_similarSec], new string[] { "sec", "s" }); }
                if (paramDict.ContainsKey(key_name))
                {
                    maid.change_Motion(paramDict[key_name], isLoop: loop, motionSpeed: speed, fadeTime: fade, similarMotionSec: similarSec, afterSpeed: afterSpeed);
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    List<MotionInfo> motionList = MotionTable.queryTable_motionNameBase(paramDict[key_category]);
                    maid.change_Motion(motionList, isLoop: loop, motionSpeed: speed, fadeTime: fade, similarMotionSec: similarSec, afterSpeed: afterSpeed);
                }
                else { info(string.Format("모션이 지정되어 있지 않습니다")); }
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
                        info(string.Format("涙の値は0~3である必要があります。強制的に0にします。"));
                        namida = 0;
                    }
                }
                if (paramDict.ContainsKey(key_hoho) || paramDict.ContainsKey(key_頬))
                {
                    if (paramDict.ContainsKey(key_hoho)) hoho = (int)parseFloat(paramDict[key_hoho]);
                    if (paramDict.ContainsKey(key_頬)) hoho = (int)parseFloat(paramDict[key_頬]);
                    if (hoho < 0 || hoho > 3)
                    {
                        info(string.Format("頬の値は0~3である必要があります。強制的に0にします。"));
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
                if (paramDict.ContainsKey(key_eye))
                {
                    float eyePosY = parseFloat(paramDict[key_eye]);
                    maid.change_eyePosY(eyePosY);
                }
                maid.change_FaceBlend(hoho: hoho, namida: namida, enableYodare: yodare);
            }
        }
        private void exec_fade(Dictionary<string, string> paramDict, bool fadeIn)
        {
            float fade = 0.5f;
            if (paramDict.ContainsKey(key_fade)) { fade = parseFloat(paramDict[key_fade], new string[] { "s", "sec" }); }
            if (fadeIn) { change_fadeInCamera(fade); }
            else { change_fadeOutCamera(fade); }
        }
        private void exec_camera(Dictionary<string, string> paramDict)
        {
            int maidNo = -1;
            if (paramDict.ContainsKey(key_maid)) { maidNo = (int)parseFloat(paramDict[key_maid]); }
            float distance = maidNo != -1 ? 1.5f : 0f;
            if (paramDict.ContainsKey(key_d)) { distance = parseFloat(paramDict[key_d], new string[] { "m" }); }
            if (maidNo != -1) { ScriplayPlugin.moveCamera(maidList[maidNo], distance); }
            else
            {
                PosRot pr = toScriplay(ScriplayPlugin.getCameraPos(), ScriplayPlugin.getCameraAngle());
                Vector3 pos = pr.pos;
                Vector3 angle = pr.rot;
                if (paramDict.ContainsKey(key_x)) { pos.x = parseFloat(paramDict[key_x], new string[] { "m" }); }
                if (paramDict.ContainsKey(key_y)) { pos.y = parseFloat(paramDict[key_y], new string[] { "m" }); }
                if (paramDict.ContainsKey(key_z)) { pos.z = parseFloat(paramDict[key_z], new string[] { "m" }); }
                //if (paramDict.ContainsKey(key_azimuth)) { azimuth += parseFloat(paramDict[key_azimuth], new string[] { "deg" }); }
                //if (paramDict.ContainsKey(key_elevation)) { elevation += parseFloat(paramDict[key_elevation], new string[] { "deg" }); }
                if (paramDict.ContainsKey(key_rx)) { angle.x = parseFloat(paramDict[key_rx], new string[] { "deg" }); }
                if (paramDict.ContainsKey(key_ry)) { angle.y = parseFloat(paramDict[key_ry], new string[] { "deg" }); }
                if (paramDict.ContainsKey(key_rz)) { angle.z = parseFloat(paramDict[key_rz], new string[] { "deg" }); }
                ScriplayPlugin.moveCamera(convertPos_to_World(pos), convertAngle_to_World(angle), distance);
            }
        }
        private void exec_origin(Dictionary<string, string> paramDict)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            Vector3 origin_pos = getOriginPos();
            Vector3 origin_rot = getOriginAngle();
            float x = origin_pos.x;
            float y = origin_pos.y;
            float z = origin_pos.z;
            float rx = origin_rot.x;
            float ry = origin_rot.y;
            float rz = origin_rot.z;
            if (paramDict.ContainsKey(key_maid) && charaList.Count == 1)
            {
                //特定のメイドを指定したときは、そのメイドの現在座標をScriplay原点とする
                IMaid m = charaList[0];
                Vector3 v = m.getPosition();
                x = v.x;
                y = v.y;
                z = v.z;
                Vector3 r = m.getAngle();
                rx = r.x;
                ry = r.y;
                rz = r.z;
            }
            else
            {
                if (paramDict.ContainsKey(key_x))
                    x = parseFloat(paramDict[key_x]);
                if (paramDict.ContainsKey(key_y))
                    y = parseFloat(paramDict[key_y]);
                if (paramDict.ContainsKey(key_z))
                    z = parseFloat(paramDict[key_z]);
                if (paramDict.ContainsKey(key_rx))
                    rx = parseFloat(paramDict[key_rx]);
                if (paramDict.ContainsKey(key_ry))
                    ry = parseFloat(paramDict[key_ry]);
                if (paramDict.ContainsKey(key_rz))
                    rz = parseFloat(paramDict[key_rz]);
            }
            setOriginPos(new Vector3(x, y, z));
            setOriginAngle(new Vector3(rx, ry, rz));
        }
        private void exec_pos(Dictionary<string, string> paramDict)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
            {
                Vector3 oldpos = maid.getPosition();
                Vector3 oldrot = maid.getAngle();
                //ワールド→Scriplay座標系
                PosRot pr = toScriplay(oldpos, oldrot);
                float x = pr.pos.x;
                float y = pr.pos.y;
                float z = pr.pos.z;
                if (paramDict.ContainsKey(key_x)) x = parseFloat(paramDict[key_x]);
                if (paramDict.ContainsKey(key_y)) y = parseFloat(paramDict[key_y]);
                if (paramDict.ContainsKey(key_z)) z = parseFloat(paramDict[key_z]);
                float rx = pr.rot.x;
                float ry = pr.rot.y;
                float rz = pr.rot.z;
                if (paramDict.ContainsKey(key_rx)) rx = parseFloat(paramDict[key_rx]);
                if (paramDict.ContainsKey(key_ry)) ry = parseFloat(paramDict[key_ry]);
                if (paramDict.ContainsKey(key_rz)) rz = parseFloat(paramDict[key_rz]);
                //Scriplay座標系→ワールド座標系
                PosRot newpr = toWorld(new Vector3(x, y, z), new Vector3(rx, ry, rz));
                Util.Debug(string.Format("pos移動　old:{0} -> new:{1}", pr.pos.ToString(), newpr.pos.ToString()));
                maid.change_positionAbsolute(newpr.pos.x, newpr.pos.y, newpr.pos.z);
                Util.Debug(string.Format("pos回転　old:{0} -> new:{1}", pr.rot.ToString(), newpr.rot.ToString()));
                maid.change_angleAbsolute(newpr.rot.x, newpr.rot.y, newpr.rot.z);
            }
        }
        private void exec_move(Dictionary<string, string> paramDict)
        {
            List<IMaid> charaList = selectMaid(paramDict); charaList.AddRange(selectMan(paramDict));
            foreach (IMaid maid in charaList)
            {
                Vector3 oldpos = maid.getPosition();
                Vector3 oldrot = maid.getAngle();
                //ワールド→Scriplay座標系
                PosRot pr = toScriplay(oldpos, oldrot);
                float x = pr.pos.x;
                float y = pr.pos.y;
                float z = pr.pos.z;
                if (paramDict.ContainsKey(key_x)) x += parseFloat(paramDict[key_x]);
                if (paramDict.ContainsKey(key_y)) y += parseFloat(paramDict[key_y]);
                if (paramDict.ContainsKey(key_z)) z += parseFloat(paramDict[key_z]);
                float rx = pr.rot.x;
                float ry = pr.rot.y;
                float rz = pr.rot.z;
                if (paramDict.ContainsKey(key_rx)) rx += parseFloat(paramDict[key_rx]);
                if (paramDict.ContainsKey(key_ry)) ry += parseFloat(paramDict[key_ry]);
                if (paramDict.ContainsKey(key_rz)) rz += parseFloat(paramDict[key_rz]);
                //Scriplay座標系→ワールド座標系
                PosRot newpr = toWorld(new Vector3(x, y, z), new Vector3(rx, ry, rz));
                Util.Debug(string.Format("move移動　old:{0} -> new:{1}", pr.pos.ToString(), newpr.pos.ToString()));
                maid.change_positionAbsolute(newpr.pos.x, newpr.pos.y, newpr.pos.z);
                Util.Debug(string.Format("move回転　old:{0} -> new:{1}", pr.rot.ToString(), newpr.rot.ToString()));
                maid.change_angleAbsolute(newpr.rot.x, newpr.rot.y, newpr.rot.z);
            }
        }
        const string key_maid = "maid";
        const string key_man = "man";
        const string key_name = "name";
        const string key_category = "category";
        const string key_mode = "mode";
        const string key_seikaku = "seikaku";
        const string key_visible = "visible";
        const string key_restore = "restore";
        const string key_start = "start";
        const string key_fade = "fade";
        const string key_interval = "interval";
        const string key_fadein = "fadein";
        const string key_speed = "speed";
        const string key_afterSpeed = "afterspeed";
        const string key_wait = "wait";
        const string key_keep = "keep";
        const string key_similar = "similar";
        const string key_similarSec = "similarsec";
        const string key_finish = "finish";
        const string key_goto = "goto";
        const string key_call = "call";
        const string key_exec = "exec";
        const string key_maidNum = "maidnum";
        const string key_manNum = "mannum";
        const string key_text = "text";
        const string key_hoho = "hoho";
        const string key_namida = "namida";
        const string key_yodare = "yodare";
        const string key_頬 = "頬";
        const string key_涙 = "涙";
        const string key_よだれ = "よだれ";
        const string key_eye = "eye";
        const string key_x = "x";
        const string key_y = "y";
        const string key_z = "z";
        const string key_rx = "rx";
        const string key_ry = "ry";
        const string key_rz = "rz";
        const string key_azimuth = "azumuth";
        const string key_elevation = "elevation";
        const string key_d = "d";
        const string key_val = "val";
        const string key_max = "max";
        const string key_min = "min";
        const string key_period = "period";
        const string key_part = "part";
        const string mode_gotolist = "gotolist";
        private void exec_show(Dictionary<string, string> paramDict, int lineNo = -1)
        {
            if (paramDict.ContainsKey(key_text))
            {
                this.showText = paramDict[key_text];
            }
            else
            {
                info(string.Format("表示するテキストが見つかりません"));
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
            Util.Debug(string.Format("line{0} show:表示文字列「{1}」 ", currentExecuteLine.ToString(), this.showText));
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
                List<VoiceTable.VoiceInfo> voiceList = new List<VoiceTable.VoiceInfo>();
                if (paramDict.ContainsKey(key_finish))
                {
                    if (paramDict[key_finish] == "1") talk_waitUntilFinishSpeekingDict[maid.maidNo] = true;
                }
                float startSec = 0f;
                float intervalSec = 0f;
                float fadeinSec = 0f;
                if (paramDict.ContainsKey(key_start))
                {
                    startSec = parseFloat(paramDict[key_start], new string[] { "sec", "s" });
                }
                if (paramDict.ContainsKey(key_interval))
                {
                    intervalSec = parseFloat(paramDict[key_interval], new string[] { "sec", "s" });
                }
                if (paramDict.ContainsKey(key_fadein))
                {
                    fadeinSec = parseFloat(paramDict[key_fadein], new string[] { "sec", "s" });
                }
                if (paramDict.ContainsKey(key_name))
                {
                    voiceList.Add(new VoiceTable.VoiceInfo(paramDict[key_name], startSec, intervalSec, fadeinSec));
                }
                else if (paramDict.ContainsKey(key_category))
                {
                    string voiceCategory = paramDict[key_category];
                    if (loop)
                    {
                        voiceList = LoopVoiceTable.queryTable(maid.sPersonal, voiceCategory, hannyou_seikaku: true);
                    }
                    else
                    {
                        voiceList = OnceVoiceTable.queryTable(maid.sPersonal, voiceCategory, hannyou_seikaku: true);
                    }
                    if (voiceList.Count == 0)
                    {
                        info(string.Format("카테고리의 보이스가 없습니다. 카테고리：{0}", voiceCategory));
                        //return;
                        continue;
                    }
                }
                if (voiceList.Count == 0 | (voiceList.Count == 1 && voiceList[0].filename.ToLower().Equals("stop")))
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
        /// 指定なき場合はすべてのIMaidを返す
        /// </summary>
        /// <param name="paramDict"></param>
        /// <returns></returns>
        private List<IMaid> selectMaid(Dictionary<string, string> paramDict)
        {
            List<IMaid> ret = new List<IMaid>();
            int maidNum = 0;
            if (!paramDict.ContainsKey(key_maid))
            {
                if (paramDict.ContainsKey(key_man)) { return ret; } //manを指定しているときは、maidリストは空とする
                else return new List<IMaid>(ScriplayPlugin.maidList);
            }
            string maidStr = paramDict[key_maid];
            if (maidStr.ToLower().Equals("all")) { return new List<IMaid>(ScriplayPlugin.maidList); }
            if (!int.TryParse(maidStr, out maidNum)) { info(string.Format("maidパラメータ「{0}」を解釈できませんでした。", maidStr)); return ret; }
            if (maidNum < 0) { info(string.Format("maidパラメータに0より小さい数が指定されました。無効です。0以上の整数を指定してください。")); return ret; }
            else if (maidNum < ScriplayPlugin.maidList.Count) { ret.Add(ScriplayPlugin.maidList[maidNum]); return ret; }
            else { info(string.Format("メイドは{0}人しか有効にしていません。maidNo.{1}は無効です", ScriplayPlugin.maidList.Count, maidNum)); return ret; }
            //return ret;
        }
        private List<IMaid> selectMan(Dictionary<string, string> paramDict)
        {
            List<IMaid> ret = new List<IMaid>();
            int manNum = 0;
            if (!paramDict.ContainsKey(key_man))
            {
                if (paramDict.ContainsKey(key_maid)) { return ret; } //maidを指定しているときは、manリストは空とする
                else return new List<IMaid>(ScriplayPlugin.manList);
            }
            string manStr = paramDict[key_man];
            if (manStr.ToLower().Equals("all")) { return new List<IMaid>(ScriplayPlugin.manList); }
            if (!int.TryParse(manStr, out manNum)) { info(string.Format("manパラメータ「{0}」を解釈できませんでした。", manStr)); return ret; }
            if (manNum < 0) { info(string.Format("manパラメータに0より小さい数が指定されました。無効です。0以上の整数を指定してください。")); return ret; }
            else if (manNum < ScriplayPlugin.manList.Count) { ret.Add(ScriplayPlugin.manList[manNum]); return ret; }
            else { info(string.Format("ご主人様は{0}人しか有効にしていません。manNo.{1}は無効です", ScriplayPlugin.manList.Count, manNum)); return ret; }
            //return ret;
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
                info(string.Format("ジャンプ先ラベルが見つかりません。ジャンプ先：{0}", gotoLabel));
                scriptFinished = true; return;
            }
            currentExecuteLine = labelMap[gotoLabel];
            Util.Debug(string.Format("line{0} : 「{1}」へジャンプしました", currentExecuteLine.ToString(), gotoLabel));
        }
        private void exec_call(string gotoLabel)
        {
            if (!labelMap.ContainsKey(gotoLabel))
            {
                info(string.Format(" call: ジャンプ先ラベルが見つかりません。ジャンプ先：{0}", gotoLabel));
                scriptFinished = true; return;
            }
            callStack.Push(new StackInfo(currentExecuteLine));
            currentExecuteLine = labelMap[gotoLabel];
            Util.Debug(string.Format("line{0}  call: 「{1}」へジャンプしました", currentExecuteLine.ToString(), gotoLabel));
        }
        private void exec_exit()
        {
            int _currentLineNo = currentExecuteLine;
            if (callStack.Count == 0)
            {
                info(string.Format(" exit:コールスタックが空です。スクリプトを終了します"));
                scriptFinished = true;
                return;
            }
            currentExecuteLine = callStack.Pop().calledLineNo;
            Util.Debug(string.Format("line{0} exit: 「{1}」へジャンプしました", _currentLineNo.ToString(), currentExecuteLine));
        }
        /// <summary>
        /// 選択肢項目を追加
        /// </summary>
        /// <param name="itemViewStr"></param>
        public void addSelection(string itemViewStr, Dictionary<string, string> dict)
        {
            string paramLabel = "";
            Selection.CommandType command = Selection.CommandType.GOTO;
            if (dict.ContainsKey(key_goto))
            {
                paramLabel = dict[key_goto];
                command = Selection.CommandType.GOTO;
            }
            else if (dict.ContainsKey(key_call))
            {
                paramLabel = dict[key_call];
                command = Selection.CommandType.CALL;
            }
            else if (dict.ContainsKey(key_exec))
            {
                paramLabel = dict[key_exec];
                command = Selection.CommandType.EXEC;
            }
            selection_selectionList.Add(new Selection(itemViewStr, paramLabel, command));
            Util.Debug(string.Format("選択肢「{0}」を追加", itemViewStr));
        }
    }
}
