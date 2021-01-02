using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityInjector.Attributes;
using PluginExt;
using System.Collections;
using System.Data;
using System.Text;
using static COM3D2.ScriplayLilly2.Plugin.ScriplayPlugin;
using UnityEngine.UI;
// https://ux.getuploader.com/arch_plugin/download/2
// Scriplay_0.2.0_COM3D2GP-01Ver.1.27.1x64.zip
namespace COM3D2.ScriplayLilly2.Plugin
{
    [PluginFilter("COM3D2x64"), PluginFilter("COM3D2x86"), PluginFilter("COM3D2VRx64"),
    PluginFilter("COM3D2OHx64"), PluginFilter("COM3D2OHx86"), PluginFilter("COM3D2OHVRx64"),
    PluginName("ScriplayLilly2"), PluginVersion("0.1.0.0 edit by lilly")]
    public class ScriplayPlugin : ExPluginBase
    {
        //　設定クラス（Iniファイルで読み書きしたい変数はここに格納する）
        public class ScriplayConfig
        {
            internal readonly float faceAnimeFadeTime = 1f;    //フェイスアニメフェード時間　1sec
            internal readonly string csvPath = @"Sybaris\UnityInjector\Config\Scriplay\csv\";
            internal readonly string scriptsPath = @"Sybaris\UnityInjector\Config\Scriplay\scripts\";
            internal string onceVoicePrefix = "oncevoice_";
            internal string loopVoicePrefix = "loopvoice_";
            internal string motionListPrefix = "motion_";
            internal string faceListPrefix = "face_";
            internal readonly string libFilePrefix = "lib_";
            internal readonly string initRoutinePrefix = "init_";
            internal string PluginName = "ScriplayLilly2";
            internal string debugPrintColor = "red";
            internal bool enModMotionLoad = false;  //true;
            internal float sio_baseTime = 1f;           //潮　ベース時間
            internal float nyo_baseTime = 3f;           //尿　ベース時間
            internal int studioModeSceneLevel = 26;     //スタジオモードのシーンレベル
            internal readonly float similarMotion_intervalSec = 5f;     //類似モーション変更の周期
            internal bool enable_debugLog_onConsole = true;     //logger 디버깅 문자를 출력하거나
        }
        public static ScriplayConfig cfg = null;
        public static List<string> zeccyou_fn_list = new List<string>();  //絶頂モーションファイル名のリスト、絶頂モーション検索用
        public static List<string> motionNameAllList = new List<string>();        //게임 데이터의 모든 모션 데이터
        /// <summary>
        /// 전체 동작 목록
        /// </summary>
        public static HashSet<string> motionCategorySet = new HashSet<string>();
        //メイド情報
        private Transform[] maidHead = new Transform[20];
        /// <summary>
        /// メイドリスト
        /// </summary>
        public static List<IMaid> maidList = new List<IMaid>();
        public static List<IMaid> manList = new List<IMaid>();
        private bool gameCfg_isChuBLipEnabled = false;
        private bool gameCfg_isVREnabled = false;
        private bool gameCfg_isPluginEnabledScene = true;//false;
        public static Dictionary<string, string> bgDict = new Dictionary<string, string>()
        {
            {"Shitsumu_ChairRot"       , "執務室"},
            {"Shitsumu_ChairRot_Night" , "執務室（夜）"},
            {"Salon"                   , "サロン"},
            {"Syosai"                  , "書斎"},
            {"Syosai_Night"            , "書斎（夜）"},
            {"DressRoom_NoMirror"      , "ドレスルーム"},
            {"MyBedRoom"               , "自室"},
            {"MyBedRoom_Night"         , "自室（夜）"},
            {"HoneymoonRoom"           , "ハネムーンルーム（夜）"},
            {"Bathroom"                , "お風呂（夜）"},
            {"PlayRoom"                , "プレイルーム"},
            {"PlayRoom2"               , "プレイルーム2"},
            {"Pool"                    , "プール"},
            {"SMRoom"                  , "SMルーム"},
            {"SMRoom2"                 , "地下室"},
            {"Salon_Garden"            , "中庭"},
            {"LargeBathRoom"           , "大浴場"},
            {"OiranRoom"               , "花魁部屋"},
            {"Penthouse"               , "ペントハウス"},
            {"Town"                    , "街"},
            {"Kitchen"                 , "キッチン"},
            {"Kitchen_Night"           , "キッチン（夜）"},
            {"Salon_Entrance"          , "エントランス"},
            {"poledancestage"          , "ポールダンス"},
            {"Bar"                     , "バー（夜）"},
            {"Toilet"                  , "トイレ"},
            {"Soap"                    , "ソープ"},
            {"MaidRoom"                , "メイド部屋"},
        };
        /// <summary>
        /// モーションのベース名　算出のための定義リスト
        /// sufix　を除去していくことでベース名を求める。同一レベル内の微変更のために使用（1a01 -> 1b02とか）。
        /// 例：  xx_1a01_xxx   -> xx_1
        ///       xx_1_xxx      -> xx_1
        /// </summary>
        private static Dictionary<string, string> motionBaseRegexDefDic = new Dictionary<string, string>()
            {
                {@"_1_.*",          "_1" },
                {@"_2_.*",          "_2" },
                {@"_3_.*",          "_3" },
                {@"_1\w\d\d.*",     "_1" },
                {@"_2\w\d\d.*",     "_2" },
                {@"_3\w\d\d.*",     "_3" },
                {@"\.anm",          "" },
                //once系モーションのonce部分抽出
                {@"_ikou_.*",       ""},
                {@"_shasei_.*",     ""},
                {@"_zeccyou_.*",    ""},
                {@"_f_once_.*",     ""},
                {@"_f2_once_.*",    ""},
                {@"_m_once_.*",     ""},
                {@"_m2_once_.*",    ""},
        };
        //厳密版
        //例：    xx_1a01_xx  -> xx_
        private static Dictionary<string, string> motionBaseRegexDefDic_strict = new Dictionary<string, string>()
            {
                {@"_[123]_.*",          "_" },
                {@"_[123]\w\d\d.*",     "_" },
                {@"\.anm",              "" },
                {@"_asiname_.*",          "_"},
                //{@"_aibu_.*",          "_"},
                {@"_cli[\d]?_.*",       "_"},
                {@"_daki_.*",            "_"},
                {@"_fera_.*",             "_"},
                {@"_gr_.*",            "_"},
                {@"_housi_.*",            "_"},
                {@"_hibu_.*",            "_"},
                {@"_hibuhiraki_.*",            "_"},
                {@"_ir_.*",               "_"},
                {@"_kakae_.*",             "_"},
                {@"_kuti_.*",             "_"},
                {@"_kiss_.*",             "_"},
                {@"_momi_.*",             "_" },
                {@"_onani_.*",            "_"},
                {@"_oku_.*",            "_"},
                {@"_peace_.*",            "_"},
                {@"_ran4p_.*",            "_"},
                {@"_ryoutenaburi_.*",            "_"},
                {@"_siri_.*",           "_" },
                {@"_sissin_.*",          "_"},
                {@"_shasei_.*",           "_" },
                {@"_shaseigo_.*",         "_" },
                {@"_sixnine_.*",          "_"},
                {@"_surituke_.*",         "_"},
                {@"_siriname_.*",         "_"},
                {@"_taiki_.*",            "_" },
                {@"_tikubi_.*",          "_"},
                {@"_tekoki_.*",          "_"},
                {@"_tikubiname_.*",       "_"},
                {@"_tati_.*",       "_"},
                {@"_ubi\d?_.*",       "_"},
                {@"_vibe_.*",        "_" },
                {@"_zeccyougo_.*",        "_" },
                {@"_zeccyou_.*",          "_" },
                {@"_zikkyou_.*",       "_"},
            };
        //正規表現をコンパイルするのに時間がかかるため、あらかじめコンパイルしたものを使用する。https://docs.unity3d.com/ja/current/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
        private static Dictionary<Regex, string> motionBaseRegexDic = new Dictionary<Regex, string>();
        private static Dictionary<Regex, string> motionBaseRegexDic_strict = new Dictionary<Regex, string>();
        public static KeyCode currentKeyCode = KeyCode.None;
        public static Vector3 getCameraPos() { return GameMain.Instance.MainCamera.transform.position; }
        public static Vector3 getCameraAngle() { return GameMain.Instance.MainCamera.transform.eulerAngles; }
        //指定座標へ一瞬でカメラを移動
        public static void moveCamera(Vector3 worldPosition, Vector3 worldAngle, float distance = 0f)
        {
            Util.Debug(string.Format("カメラ移動 " + Util.showPosAngle(worldPosition, worldAngle) + ", d:{0:f2}", distance));
            Vector3 cameraAngle = new Vector3(worldAngle.x, worldAngle.y, worldAngle.z);
            CameraMain camera = GameMain.Instance.MainCamera;
            camera.transform.eulerAngles = cameraAngle;
            camera.SetPos(worldPosition);
            camera.SetTargetPos(worldPosition);
            camera.SetDistance(distance);
        }
        /// <summary>
        /// メイドの前にカメラ移動
        /// </summary>
        /// <param name="maid"></param>
        /// <param name="distance"></param>
        public static void moveCamera(IMaid maid, float distance = 1.5f)
        {
            Vector3 pos = maid.maid.body0.trsNeck.transform.position;
            Vector3 a = maid.maid.body0.trsNeck.transform.eulerAngles;
            Vector3 angle = new Vector3(0, (a.y - 90) % 360, 0);
            CameraMain camera = GameMain.Instance.MainCamera;
            camera.transform.eulerAngles = angle;
            camera.SetPos(pos);
            camera.SetTargetPos(pos);
            camera.SetDistance(distance);
        }
        /// <summary>
        /// メイドリスト・男リスト　初期化、メイドステータスも初期化
        /// 메이드 목록 남자 목록 초기화 메이드 상태도 초기화
        /// </summary>
        public static void initMaidList()
        {
            if (maidList == null) return;
            //Util.info("メイド一覧読み込み開始메이드 목록을 불러오는 시작");
            maidList.Clear();
            manList.Clear();
            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            for (int i = 0; i < cm.GetMaidCount(); i++)
            {
                Maid m = cm.GetMaid(i);
                if (!isMaidAvailable(m)) continue;
                maidList.Add(new IMaid(i, m));
                Util.Info(string.Format("メイド「{0}」を検出しました을 발견했습니다", m.status.fullNameJpStyle));
            }
            //男は最大6人、cm.GetManCount()は機能してない？ぽいので決め打ちでループ回す。
            for (int i = 0; i < 6; i++)
            {
                Maid m = cm.GetMan(i);  //無効な男Noならnullが返ってくる、nullチェックする
                if (!isManAvailable(m)) continue;
                manList.Add(new IMaid(i, m, isMan: true));
                Util.Info(string.Format("ご主人様「{0}」を検出しました을 발견했습니다", m.status.fullNameJpStyle));
            }
            GameMain.Instance.SoundMgr.StopSe();
        }
        private static bool isMaidAvailable(Maid m)
        {
            return m != null && m.Visible && m.AudioMan != null;
        }
        //男はAudioManagerがnullなので別に判定関数を用意
        private static bool isManAvailable(Maid m)
        {
            return m != null && m.Visible;
        }
        public void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
            string gameDataPath = UnityEngine.Application.dataPath;
            loadSetting();
            // ChuBLip判別 ChuBLip 판별
            gameCfg_isChuBLipEnabled = gameDataPath.Contains("COM3D2OHx64") || gameDataPath.Contains("COM3D2OHx86") || gameDataPath.Contains("COM3D2OHVRx64");
            // VR判別
            gameCfg_isVREnabled = gameDataPath.Contains("COM3D2OHVRx64") || gameDataPath.Contains("COM3D2VRx64") || Environment.CommandLine.ToLower().Contains("/vr");
            //MotionBase変換用正規表現　事前コンパイル 변환 용 정규식 사전 컴파일
            foreach (KeyValuePair<string, string> kvp in motionBaseRegexDefDic)
            {
                Regex regex1 = new Regex(kvp.Key);
                motionBaseRegexDic.Add(regex1, kvp.Value);
            }
            foreach (KeyValuePair<string, string> kvp in motionBaseRegexDefDic_strict)
            {
                Regex regex1 = new Regex(kvp.Key);
                motionBaseRegexDic_strict.Add(regex1, kvp.Value);
            }
            //UI表示　初期化
            initGuiStyle();
        }
        /// <summary>
        /// ファイル読み込み　コンフィグ、モーション、ボイス
        /// </summary>
        private void loadSetting()
        {
            cfg = ReadConfig<ScriplayConfig>("ScriplayConfig");
            load_ConfigCsv();
            load_motionGameData(cfg.enModMotionLoad);
        }
        private void load_ConfigCsv()
        {
            Util.Info("CSVファイル読み込み");
            OnceVoiceTable.init();
            LoopVoiceTable.init();
            MotionTable.init();
            FaceTable.init();
            motionCategorySet.Clear();
            List<string> filelist = Util.getFileFullpathList(cfg.csvPath, suffix: "csv");
            string filenameList = "\r\n";
            foreach (string fullpath in filelist)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                filenameList += basename + "\r\\n";
                Util.Info(string.Format("CSV:{0}", basename));
                if (basename.Contains(cfg.motionListPrefix))
                {
                    MotionTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.onceVoicePrefix))
                {
                    OnceVoiceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.loopVoicePrefix))
                {
                    LoopVoiceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
                else if (basename.Contains(cfg.faceListPrefix))
                {
                    FaceTable.parse(Util.ReadCsvFile(fullpath, false), basename);
                }
            }
            if (filenameList == "\r\n") filenameList = "（CSVファイルが見つかりませんでした）";
            Util.Info(filenameList);
            foreach (string s in MotionTable.getCategoryList())
            {
                motionCategorySet.Add(s);
            }
        }
        /// <summary>
        /// Unityが把握しているモーションデータを取得して一覧作成
        /// </summary>
        /// <param name="allLoad">バニラのモーションデータのみ読み込み（Modのモーションは含まず）</param>
        private void load_motionGameData(bool allLoad = true)
        {
            // COM3D2のモーションファイル全列挙
            Util.Info("モーションファイル読み込み開始");
            motionNameAllList.Clear();
            if (!allLoad)
            {
                ///*
                // FileSystemArchiveのGetFileは全てのファイルを検索対象とするため時間がかかる
                // 全体を対象とするよりは、「motion」「motion2」配下だけを対象としたほうが早いため、下記のように処理
                //参考：https://github.com/Neerhom/COM3D2.ModLoader/blob/master/COM3D2.ModMenuAccel.patcher/COM3D2.ModMenuAccel.Hook/COM3D2.ModMenuAccel.Hook/FastStart.cs
                //*/
                string[] motionDirList = { "motion", "motion2", "motion_3d21reserve_2", "motion_cos021_2", "motion_denkigai2017w_2" };
                ArrayList Files = new ArrayList();
                foreach (string s in motionDirList)
                {
                    Files.AddRange(GameUty.FileSystem.GetList(s, AFileSystemBase.ListType.AllFile));
                }
                foreach (string file in Files)
                {
                    if (Path.GetExtension(file) == ".anm") motionNameAllList.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            else
            {
                motionNameAllList.AddRange(Util.GetFilenameList_byExtension(".anm"));      //数秒かかる
            }
            //ファイル名でソート
            motionNameAllList.Sort();
            string added = "\r\n";
            foreach (string s in motionNameAllList)
            {
                added += s + "\r\n";
            }
            //Util.debug(added);
            Util.Info("モーションファイル読み込み終了");
            //絶頂モーションファイル名リスト　作成
            foreach (string filename in motionNameAllList)
            {
                if (filename.Contains("zeccyou_f_once")) zeccyou_fn_list.Add(filename);
            }
            zeccyou_fn_list.Sort();
        }
        public void Start()
        {
        }
        public void OnDestroy()
        {
        }
        public void OnLevelWasLoaded(int level)
        {
            Util.Info(string.Format("OnLevelWasLoaded:{0}", level));
            VRUI.OnLevelWasLoaded(level);
            //初回スクリプト読み込み
            if (scripts_fullpathList.Count == 0) reload_scriptList();
            //// スタジオモードならプラグイン有効
            //if (level == cfg.studioModeSceneLevel)            {                gameCfg_isPluginEnabledScene = true;            }
            //else
            //{
            //    Util.info("スタジオモードでないためプラグイン無効化できません");
            //    gameCfg_isPluginEnabledScene = false;
            //    return;
            //}
            initMaidList();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            Util.Info(string.Format("OnSceneLoaded:{0}", scene.name));
            VRUI.OnLevelWasLoaded(0);
            if (scripts_fullpathList.Count == 0) reload_scriptList();
            initMaidList();
        }


            private bool isYotogiScene(int sceneLevel)
        {
            return GameMain.Instance.CharacterMgr.GetMaidCount() != 0;
            //int yotogiManagerCount = FindObjectsOfType<YotogiManager>().Length;
            //return yotogiManagerCount > 0;
        }
        private static readonly float UIwidth = 300;
        private static readonly float UIheight = 400;
        private static readonly float UIposX_rightMargin = 10;
        private static readonly float UIposY_bottomMargin = 120;
        Rect node_scripts = new Rect(
            UnityEngine.Screen.width - UIposX_rightMargin - UIwidth
            , UnityEngine.Screen.height - UIposY_bottomMargin - UIheight
            , UIwidth
            , UIheight);
        Rect node_config = new Rect(UnityEngine.Screen.width - UIposX_rightMargin - UIwidth
            , UnityEngine.Screen.height - (UIheight + UIposY_bottomMargin) - UIheight
            , UIwidth
            , UIheight);
        private static readonly float UIwidth_showArea = 800;
        private static readonly float UIheight_showArea = 150;
        private static readonly float UIposX_rightMargin_showArea = 150;
        private static readonly float UIposY_bottomMargin_showArea = 10;
        Rect node_showArea = new Rect(UnityEngine.Screen.width - UIposX_rightMargin_showArea - UIwidth,
            UnityEngine.Screen.height - UIposY_bottomMargin_showArea - UIheight
            , UIwidth
            , UIheight);
        /*        Rect node_showArea = new Rect(UnityEngine.Screen.width - UIposX_rightMargin_showArea - UIwidth_showArea,
            UnityEngine.Screen.height - UIposY_bottomMargin_showArea - UIheight_showArea
            , UIwidth_showArea
            , UIheight_showArea);
*/
        private Vector2 scrollPosition = new Vector2();
        private Queue<string> debug_strQueue = new Queue<string>();
        private Dictionary<string, string> debug_ovtQueryMap = new Dictionary<string, string>()
        {
            {"Personal","" },
            {"Category","" },
        };
        private string debug_toast = "";
        private Queue<string> debug_toastQueue = new Queue<string>();
        private string debug_playVoice = "";
        private string debug_playVoiceStart = "";
        private string debug_playVoiceInterval = "";
        private Queue<string> debug_playVoiceQueue = new Queue<string>();
        private string debug_playMotion = "";
        private Queue<string> debug_playMotionQueue = new Queue<string>();
        private string debug_face = "";
        private Queue<string> debug_faceQueue = new Queue<string>();
        private string debug_propFilename = "";
        private Queue<string> debug_propQueue = new Queue<string>();
        private string debug_script = "";
        private Queue<string> debug_scriptQueue = new Queue<string>();
        private Queue<string> debug_scriplayCreateQueue = new Queue<string>();
        private string capture_WaitSecText = "0.5";
        private bool quit_captureCoroutine = false;
        private string capture_saveBasePath = @"Sybaris\UnityInjector\Config\Scriplay\capture";
        void OnGUI()
        {
            //if (!gameCfg_isPluginEnabledScene) return;
            if (VRUI.isVREnabled()) return;
            if (scriplayContext.scriptFinished)
            {
                node_scripts = GUI.Window(423, node_scripts, WindowCallback_scriptsView, cfg.PluginName + " スクリプト一覧", gsBox);
                if (en_showConfig)
                {
                    node_config = GUI.Window(421, node_config, WindowCallback_config, cfg.PluginName + " Config", gsBox);
                }
            }
            else
            {
                //テキスト・選択肢表示
                node_showArea = GUI.Window(422, node_showArea, WindowCallback_showArea, "", gsBox);
            }
        }
        private ScriplayContext scriplayContext = ScriplayContextVer01.None;
        private bool en_showConfig = false;
        private Vector2 scriptsList_scrollPosition = new Vector2();
        private List<string> scripts_fullpathList = new List<string>();
        private static MonoBehaviour instance;
        private Vector2 showArea_scrollPosition = new Vector2();
        ScriplayPlugin()
        {
            instance = this;
        }
        bool enReloadScript = true;
        void WindowCallback_scriptsView(int id)
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", gsButtonSmall))
            {
                reload_scriptList();
            }
            if (GUILayout.Button("Config", gsButtonSmall))
            {
                en_showConfig = !en_showConfig;
            }
            GUILayout.EndHorizontal();
            scriptsList_scrollPosition = GUILayout.BeginScrollView(scriptsList_scrollPosition);
            foreach (string fullpath in scripts_fullpathList)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                if (GUILayout.Button(basename, gsButton))
                {
                    scriplayContext = ScriplayContext.readScriptFile(fullpath);
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
        private void reload_scriptList()
        {
            scripts_fullpathList.Clear();
            List<string> mdList = Util.getFileFullpathList(cfg.scriptsPath, suffix: "md");
            foreach (string fullpath in mdList)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                if (basename.StartsWith(cfg.libFilePrefix)) continue;
                scripts_fullpathList.Add(fullpath);
            }
            Util.Info(string.Format("以下のスクリプトが見つかりました"));
            VRUI.setShowText("以下のスクリプトが見つかりました");
            int index = 0;
            VRUI.clearSelection();
            foreach (string fullpath in scripts_fullpathList)
            {
                string basename = Path.GetFileNameWithoutExtension(fullpath);
                Util.Info(basename);
                VRUI.setSelection(index, basename);
                index++;
            }
        }
        private string showText_buf = "";
        void WindowCallback_showArea(int id)
        {
            GUIStyle gsButton_stopScript = new GUIStyle("button");
            gsButton_stopScript.fontSize = 10;
            gsButton_stopScript.alignment = TextAnchor.MiddleCenter;
            gsButton_stopScript.fixedWidth = 100;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop Script", gsButton_stopScript))
            {
                scriplayContext.scriptFinished = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            showArea_scrollPosition = GUILayout.BeginScrollView(showArea_scrollPosition);
            if (scriplayContext.needUpdate_showText)
            {
                scriplayContext.needUpdate_showText = false;
                showText_buf = scriplayContext.showText;
                VRUI.setShowText(scriplayContext.showText);
            }
            GUILayout.Label(showText_buf, gsLabel);
            foreach (ScriplayContext.Selection s in scriplayContext.selection_selectionList)
            {
                if (GUILayout.Button(s.viewStr, gsButton))
                {
                    scriplayContext.selection_selectedItem = s;
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
        GUIStyle gsLabelTitle = new GUIStyle("label");
        GUIStyle gsLabel = new GUIStyle("label");
        GUIStyle gsLabelSmall = new GUIStyle("label");
        GUIStyle gsButton = new GUIStyle("button");
        GUIStyle gsButtonSmall = new GUIStyle("button");
        GUIStyle gsBox = new GUIStyle("box");
        private void initGuiStyle()
        {
            gsLabelTitle.fontSize = 14;
            gsLabelTitle.alignment = TextAnchor.MiddleCenter;
            gsLabel.fontSize = 12;
            gsLabel.alignment = TextAnchor.MiddleLeft;
            gsLabelSmall.fontSize = 10;
            gsLabelSmall.alignment = TextAnchor.MiddleLeft;
            gsButton.fontSize = 12;
            gsButton.alignment = TextAnchor.MiddleCenter;
            gsButtonSmall.fontSize = 10;
            gsButtonSmall.alignment = TextAnchor.MiddleCenter;
            gsBox.fontSize = 11;
            gsBox.alignment = TextAnchor.UpperLeft;
        }
        void WindowCallback_config(int id)
        {
            //メインアイコン------------------------------------------------
            GUILayout.Space(20);    //UIタイトルとかぶらないように
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Maid", gsButtonSmall))
            {
                initMaidList();
            }
            if (GUILayout.Button("Reload CSV", gsButtonSmall))
            {
                load_ConfigCsv();
            }
            GUILayout.EndHorizontal();
            //TODO Configファイル新規作成できてない
            /*
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Reload Config.ini", gsButtonSmall))
                        {
                            cfg = ReadConfig<ScriplayConfig>("ScriplayConfig");
                        }
                        if (GUILayout.Button("Save Config.ini", gsButtonSmall))
                        {
                            SaveConfig<ScriplayConfig>(cfg,"ScriplayConfig");
                        }
                        GUILayout.EndHorizontal();
            */
            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            //メインコンテンツ------------------------------------------------
            GUILayout.Label("スクリプト", gsLabel);
            GUILayout.Label(string.Format("スクリプト名：{0}", scriplayContext.scriptName), gsLabelSmall);
            GUILayout.Label(string.Format("状態：{0}", scriplayContext.scriptFinished ? "スクリプト終了" : "スクリプト実行中"), gsLabelSmall);
            GUILayout.Label(string.Format("現在実行行：{0}", scriplayContext.currentExecuteLine), gsLabelSmall);
            Vector3 oPos = scriplayContext.getOriginPos();
            Vector3 oAngle = scriplayContext.getOriginAngle();
            GUILayout.Label(string.Format("Scriplay原点 " + Util.showPosAngle(oPos, oAngle)), gsLabelSmall);
            Transform trsCamera = GameMain.Instance.MainCamera.transform;
            Vector3 cPos = scriplayContext.convertPos_to_Scriplay(trsCamera.position);
            Vector3 cAngle = scriplayContext.convertAngle_to_Scriplay(trsCamera.eulerAngles);
            GUILayout.Label(string.Format("カメラ位置@Scriplay座標系 " + Util.showPosAngle(cPos, cAngle)), gsLabelSmall);
            for (int i = 0; i < maidList.Count; i++)
            {
                IMaid maid = maidList[i];
                Vector3 pos = scriplayContext.convertPos_to_Scriplay(maid.getPosition());
                Vector3 angle = scriplayContext.convertAngle_to_Scriplay(maid.getAngle());
                GUILayout.Label(string.Format("メイド{0}@Scriplay座標系 " + Util.showPosAngle(pos, angle), i), gsLabelSmall);
            }
            for (int i = 0; i < manList.Count; i++)
            {
                IMaid man = manList[i];
                Vector3 pos = scriplayContext.convertPos_to_Scriplay(man.getPosition());
                Vector3 angle = scriplayContext.convertAngle_to_Scriplay(man.getAngle());
                GUILayout.Label(string.Format("男{0}@Scriplay座標系 " + Util.showPosAngle(pos, angle), i), gsLabelSmall);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Prop復元", gsButtonSmall))
            {
                foreach (IMaid m in maidList) { m.prop_snapshot(); m.prop_restore(); }
            }
            GUILayout.Label("ボイス再生", gsLabelSmall);
            debug_playVoice = GUILayout.TextField(debug_playVoice);
            GUILayout.BeginHorizontal();
            GUILayout.Label("再生開始位置（秒）", gsLabelSmall);
            debug_playVoiceStart = GUILayout.TextField(debug_playVoiceStart);
            //float.TryParse(GUILayout.TextField(debug_playVoiceStart.ToString()), out debug_playVoiceStart);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("再生時間（秒）", gsLabelSmall);
            debug_playVoiceInterval = GUILayout.TextField(debug_playVoiceInterval);
            //float.TryParse(GUILayout.TextField(debug_playVoiceFade.ToString()), out debug_playVoiceFade);
            GUILayout.EndHorizontal();
            if (currentKeyCode == KeyCode.Return && debug_playVoice != "")
            {
                debug_playVoice = Util.forceEndsWith(debug_playVoice, ".ogg");
                //maidList[0].maid.AudioMan.LoadPlay(debug_playVoice, 0f, false, false);
                float start, interval;
                float.TryParse(debug_playVoiceStart, out start);
                float.TryParse(debug_playVoiceInterval, out interval);
                maidList[0].change_onceVoice(debug_playVoice, startSec: start, intervalSec: interval);
                debug_playVoiceQueue.Enqueue(debug_playVoice);
                if (debug_playVoiceQueue.Count > 3) debug_playVoiceQueue.Dequeue();
                debug_playVoice = "";
            }
            foreach (string s in debug_playVoiceQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    maidList[0].maid.AudioMan.LoadPlay(s, 0f, false, false);
                }
            }
            GUILayout.Label("モーション再生", gsLabelSmall);
            debug_playMotion = GUILayout.TextField(debug_playMotion);
            if (currentKeyCode == KeyCode.Return && debug_playMotion != "")
            {
                maidList[0].change_Motion(debug_playMotion, true);
                debug_playMotionQueue.Enqueue(debug_playMotion);
                if (debug_playMotionQueue.Count > 3) debug_playMotionQueue.Dequeue();
                debug_playMotion = "";
            }
            foreach (string s in debug_playMotionQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    maidList[0].change_Motion(s, true);
                }
            }
            GUILayout.Label("表情再生", gsLabelSmall);
            debug_face = GUILayout.TextField(debug_face);
            if (currentKeyCode == KeyCode.Return && debug_face != "")
            {
                maidList[0].change_faceAnime(debug_face);
                debug_faceQueue.Enqueue(debug_face);
                if (debug_faceQueue.Count > 3) debug_faceQueue.Dequeue();
                debug_face = "";
            }
            foreach (string s in debug_faceQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    maidList[0].change_faceAnime(s);
                }
            }
            GUILayout.Label("prop切替", gsLabelSmall);
            debug_propFilename = GUILayout.TextField(debug_propFilename);
            if (currentKeyCode == KeyCode.Return && debug_propFilename != "")
            {
                maidList[0].change_setProp(debug_propFilename);
                debug_propQueue.Enqueue(debug_propFilename);
                if (debug_propQueue.Count > 3) debug_propQueue.Dequeue();
                debug_propFilename = "";
            }
            foreach (string s in debug_propQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    if (maidList[0].getMPN_fromPropFilename(s) == MPN.null_mpn) maidList[0].change_setProp(s);
                    else maidList[0].change_delProp(s);
                }
            }
            GUILayout.Label("トースト再生", gsLabelSmall);
            debug_toast = GUILayout.TextField(debug_toast);
            if (currentKeyCode == KeyCode.Return && debug_toast != "")
            {
                toast(debug_toast);
                debug_toastQueue.Enqueue(debug_toast);
                if (debug_toastQueue.Count > 3) debug_toastQueue.Dequeue();
                debug_toast = "";
            }
            foreach (string s in debug_toastQueue)
            {
                if (GUILayout.Button(s, gsButtonSmall))
                {
                    toast(s);
                }
            }
            GUILayout.Label("スクリプト実行", gsLabelSmall);
            if (!scriplayContext.scriptFinished)
            {
                GUILayout.Label("　（スクリプト実行中）", gsLabelSmall);
            }
            else
            {
                debug_script = GUILayout.TextField(debug_script);
                if (currentKeyCode == KeyCode.Return && debug_script != "")
                {
                    debug_scriplayCreateQueue.Enqueue(debug_script);
                    debug_scriptQueue.Enqueue(debug_script);
                    if (debug_scriptQueue.Count > 3) debug_scriptQueue.Dequeue();
                    debug_script = "";
                }
                foreach (string s in debug_scriptQueue)
                {
                    if (GUILayout.Button(s, gsButtonSmall))
                    {
                        this.scriplayContext = ScriplayContext.readScriptFile("スクリプト実行テスト", s.Split(new string[] { "\r\n" }, StringSplitOptions.None), restoreProp_onTearDown: false);
                    }
                }
            }
            GUILayout.Label("各Table確認", gsLabelSmall);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Personal", gsLabelSmall);
            GUILayout.Label((maidList.Count != 0) ? maidList[0].sPersonal : "メイドがロードされていません", gsLabelSmall);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Category", gsLabelSmall);
            debug_ovtQueryMap["Category"] = GUILayout.TextField(debug_ovtQueryMap["Category"]);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OnceVoice", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (VoiceTable.VoiceInfo v in OnceVoiceTable.queryTable(debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"]))
                {
                    str.Append(v.filename + ",");
                }
                Util.Info(string.Format("OnceVoiceTable　쿼리 결과 {0},{1} \r\n {2}", debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("LoopVoice", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (VoiceTable.VoiceInfo v in LoopVoiceTable.queryTable(debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"]))
                {
                    str.Append(v.filename + ",");
                }
                Util.Info(string.Format("LoopVoiceTable　쿼리 결과 {0},{1} \r\n {2}", debug_ovtQueryMap["Personal"], debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("Motion", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (MotionInfo mi in MotionTable.queryTable_motionNameBase(debug_ovtQueryMap["Category"]))
                {
                    str.Append(mi.motionName + ",");
                }
                Util.Info(string.Format("MotionTable　쿼리 결과 {0}  \r\n {1}", debug_ovtQueryMap["Category"], str.ToString()));
            }
            if (GUILayout.Button("Face", gsButtonSmall))
            {
                debug_ovtQueryMap["Personal"] = maidList[0].sPersonal;
                StringBuilder str = new StringBuilder();
                foreach (string mi in FaceTable.queryTable(debug_ovtQueryMap["Category"]))
                {
                    str.Append(mi + ",");
                }
                Util.Info(string.Format("FaceTable　クエリ結果 {0}  \r\n {1}", debug_ovtQueryMap["Category"], str.ToString()));
            }
            GUILayout.EndHorizontal();
            if (maidList.Count != 0)
            {
                IMaid maid = maidList[0];
                GUILayout.Label("メインメイド状態", gsLabelSmall);
                GUILayout.BeginHorizontal();
                var maidInfoTable = new Dictionary<string, string>()
                    {
                        {"性格",               maid.sPersonal },
                        {"再生中ボイス",       maid.getPlayingVoice() },
                        {"再生中モーション",    maid.getCurrentMotionName() },
                    };
                if (GUILayout.Button("潮", gsButtonSmall))
                {
                    maid.change_sio();
                }
                if (GUILayout.Button("尿", gsButtonSmall))
                {
                    maid.change_nyo();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            GUILayout.Label("撮影", gsLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("表情", gsButton)) { StartCoroutine(_faceCapture_Coroutine()); }
            if (GUILayout.Button("メイド動", gsButton)) { StartCoroutine(_motionCapture_Coroutine(maidList[0])); }
            if (GUILayout.Button("男動", gsButton)) { Util.Debug("manList.count " + manList.Count); StartCoroutine(_motionCapture_Coroutine(manList[0])); }
            if (GUILayout.Button("prop", gsButton)) { StartCoroutine(_propCapture_Coroutine()); }
            if (GUILayout.Button("終了", gsButton)) { quit_captureCoroutine = true; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Capture wait");
            capture_WaitSecText = GUILayout.TextField(capture_WaitSecText);
            GUILayout.Label("sec");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("save path");
            capture_saveBasePath = GUILayout.TextField(capture_saveBasePath);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
        bool last_scriptFinished = false;
        /// <summary>
        /// Unity MonoBehaviour
        /// 毎フレーム呼ばれる処理
        /// </summary>
        public void Update()
        {
            // 창 위치 초기화
            try
            {
                if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.R))
                {
                    this.node_scripts.Set(UnityEngine.Screen.width - UIposX_rightMargin - UIwidth, UnityEngine.Screen.height - UIposY_bottomMargin - UIheight, UIwidth, UIheight);
                    this.node_config.Set(UnityEngine.Screen.width - UIposX_rightMargin - UIwidth, UnityEngine.Screen.height - (UIheight + UIposY_bottomMargin) - UIheight, UIwidth, UIheight);
                    this.node_showArea.Set(
                          UnityEngine.Screen.width - UIposX_rightMargin_showArea - UIwidth
                        , UnityEngine.Screen.height - UIposY_bottomMargin_showArea - UIheight
                        , UIwidth
                        , UIheight);
                    
                }
            }
            catch (Exception ex)
            {
                //UnityEngine.Debug.LogWarning("ScriplayPlugin:GetKeyDown" + ex.ToString());
            }


            //지금 프레임에 입력 된 키 검색
            currentKeyCode = Event.current.keyCode;
            //VR関連　実行
            VRUI.Update();
            //VR画面描画
            if (scriplayContext.scriptFinished)
            {
                //VRUI表示内容　切り替え処理
                if (!last_scriptFinished)
                {
                    //スクリプト一覧再表示
                    reload_scriptList();
                }
                last_scriptFinished = true;
                //スクリプト一覧 ------
                if (enReloadScript && VRUI.getPressedSec(VRUI.VRVirtualController.PadDirection.LEFT) > 1f)
                {
                    reload_scriptList();
                    enReloadScript = false;
                }
                else
                {
                    //연속 스크립트로드를 방지하기 위해
                    if (!(VRUI.getPressedSec(VRUI.VRVirtualController.PadDirection.LEFT) > 1f)) enReloadScript = true;
                }
                //스크립트 선택 상태　確認
                int selectionIndex = 0;
                foreach (string fullpath in scripts_fullpathList)
                {
                    if (VRUI.isSelected(selectionIndex))
                    {
                        scriplayContext = ScriplayContext.readScriptFile(fullpath);
                        break;
                    }
                    selectionIndex++;
                }
            }
            else
            {
                //スクリプト　選択肢表示 ------------
                //VRUI表示内容　切り替え処理
                if (last_scriptFinished)
                {
                    VRUI.clearShowText();
                    VRUI.clearSelection();
                }
                last_scriptFinished = false;
                //1秒以上　←　押し続けたらスクリプト停止
                if (VRUI.getPressedSec(VRUI.VRVirtualController.PadDirection.LEFT) > 1f)
                {
                    scriplayContext.scriptFinished = true;
                }
                if (scriplayContext.selection_selectionList.Count == 0)
                {
                    VRUI.clearSelection();
                }
                else
                {
                    int selectionIndex = 0;
                    foreach (ScriplayContext.Selection s in scriplayContext.selection_selectionList)
                    {
                        VRUI.setSelection(selectionIndex, s.viewStr);
                        if (VRUI.isSelected(selectionIndex))
                        {
                            scriplayContext.selection_selectedItem = s;
                            break;
                        }
                        selectionIndex++;
                    }
                }
            }
            if (!gameCfg_isPluginEnabledScene) return;
            //内部的には要素数１８固定の配列のため判定に使えない
            // 내부적으로 요소 수 18 고정 배열에 대한 판정에 사용할 수없는
            //int gameMaidCount = GameMain.Instance.CharacterMgr.GetMaidCount();    
            //if (GameMain.Instance.CharacterMgr.GetMaid(0)!=null  && maidList.Count != gameMaidCount) initMaidList();
            if (GameMain.Instance.CharacterMgr.GetMaid(maidList.Count) != null    //メイド数1増えた場合 메이드 수 1 증가했을 경우
                || (maidList.Count > 0 && GameMain.Instance.CharacterMgr.GetMaid(maidList.Count - 1) == null) //メイド数1減った場合 메이드 수 1 줄어든 경우
                )
            {
                Util.Info(string.Format("update:메이드수 변경된. {0}", maidList.Count));
                initMaidList();
            }

            if (debug_scriplayCreateQueue.Count != 0)
            {
                string s = debug_scriplayCreateQueue.Dequeue();
                this.scriplayContext = ScriplayContext.readScriptFile("スクリプト実行テスト스크립트 실행 테스트", s.Split(new string[] { "\r\n" }, StringSplitOptions.None));
            }

            try
            {
                //スクリプトの実行v 스크립트 실행 v
                if (!scriplayContext.scriptFinished)
                {
                    scriplayContext.Update();
                }
            }
            catch (Exception e)
            {
                Util.Info(string.Format("update:scriplayContext.Update Exception"));
                Util.Info(string.Format(e.Message));
                initMaidList();
                scriplayContext.scriptFinished = true;
            }

            //各メイド処理 각 메이드 처리
            try
            {
                foreach (IMaid maid in maidList)
                {
                    Util.sw_start();
                    //再生処理　一括変更 재생 처리 일괄 변경
                    Util.sw_showTime("update_playing");
                    maid.update();
                    Util.sw_stop();
                }
            }
            catch (Exception e)
            {
                Util.Info(string.Format("update:in maidList:"));
                Util.Info(string.Format(e.Message));
                initMaidList();
            }
        }

        /// <summary>
        /// Updateの後に呼ばれる処理
        /// </summary>
        public void LateUpdate()
        {
            //各メイド処理
            foreach (IMaid maid in maidList)
            {
                maid.LateUpdate();
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [DllImport("User32.Dll")]
        static extern int GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")]
        extern static IntPtr GetForegroundWindow();
        //존재하지 않았던 표정은 주석 된
        private static string[] faceList = new string[] { "あーん", "エロフェラ愛情", "エロフェラ快楽", "エロフェラ嫌悪", "エロフェラ通常", "エロメソ泣き", /*"エロ愛情２",*/ "エロ我慢１", "エロ我慢２", "エロ我慢３", "エロ期待", "エロ怯え", /*"エロ興通常３",*/ "エロ興奮０", "エロ興奮１", "エロ興奮２", "エロ興奮３", "エロ緊張", "エロ嫌悪１", "エロ好感１", "エロ好感２", "エロ好感３", "エロ絶頂", "エロ舌責", "エロ舌責快楽", "エロ痛み１", "エロ痛み２", "エロ痛み３", "エロ痛み我慢", "エロ痛み我慢２", "エロ痛み我慢３", "エロ通常１", "エロ通常２", "エロ通常３", "エロ放心", "エロ羞恥１", "エロ羞恥２", "エロ羞恥３", "エロ舐め愛情", "エロ舐め愛情２", "エロ舐め快楽", "エロ舐め快楽２", "エロ舐め嫌悪", "エロ舐め通常", "きょとん", "ジト目", "ためいき", "ダンスウインク", "ダンスキス", "ダンスジト目", "ダンス困り顔", "ダンス真剣", "ダンス微笑み", "ダンス目とじ", "ダンス憂い", "ダンス誘惑", "ドヤ顔", "にっこり", "びっくり", "ぷんすか", "まぶたギュ", "むー", "引きつり笑顔", "疑問", "泣き", "居眠り安眠", "興奮射精後１", "興奮射精後２", "苦笑い", "困った", "思案伏せ目", "少し怒り", "照れ", "照れ叫び", "笑顔", "接吻", "絶頂射精後１", "絶頂射精後２", "恥ずかしい", /*"痛み３",*/ "痛みで目を見開いて", "通常", "通常射精後１", "通常射精後２", "怒り", "発情", "悲しみ２", "微笑み", "閉じフェラ愛情", "閉じフェラ快楽", "閉じフェラ嫌悪", "閉じフェラ通常", "閉じ目", "閉じ舐め愛情", "閉じ舐め快楽", "閉じ舐め快楽２", "閉じ舐め嫌悪", "閉じ舐め通常", "目を見開いて", "目口閉じ", "優しさ", "誘惑", "余韻弱", "拗ね", "ウインク照れ", /*"エロ嫌悪",*/ "エロ舐め通常２", "ダンス真剣２", "ダンス目つむり" };
        static float faceFadeSec = 0.05f;
        private System.Collections.IEnumerator _faceCapture_Coroutine()
        {
            IMaid maid = maidList[0];
            SortedDictionary<string, string> savedimagePathDict = new SortedDictionary<string, string>();
            foreach (string sFaceAnimeName in faceList)
            {
                if (quit_captureCoroutine) { quit_captureCoroutine = false; break; }
                maid.change_faceAnime(sFaceAnimeName, faceFadeSec);
                Util.Info("表情撮影표정 촬영：" + sFaceAnimeName);
                yield return new WaitForSeconds(0.1f);
                string p = save_screen(sFaceAnimeName);
                savedimagePathDict[sFaceAnimeName] = p;
                yield return new WaitForSeconds(0.1f);
            }
            writeCatarogHTML(todayFilename("faceCatarog"), savedimagePathDict);
        }
        private System.Collections.IEnumerator _motionCapture_Coroutine(IMaid maid)
        {
            float motionCaptureWait = 0.5f;
            try { motionCaptureWait = float.Parse(capture_WaitSecText); }
            catch (Exception e) { Util.Info(e.StackTrace); yield break; }
            List<string> targetMotionList = new List<string>();
            foreach (string s in motionNameAllList) { if (maid.isValidMotionName(s)) targetMotionList.Add(s); }
            Util.Info("motionCapture:start writing motionName list...");
            writeTextListHTML(DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_motionList.html", Util.list2Str(targetMotionList, "\r\n"));
            Util.Info("motionCapture:finished writing.");
            SortedDictionary<string, string> savedimagePathDict = new SortedDictionary<string, string>();
            foreach (string s in targetMotionList)
            {
                string motionFileName = Util.forceEndsWith(s, ".anm");
                savedimagePathDict[motionFileName] = get_save_screen_relativeFilepath(motionFileName);
            }
            writeCatarogHTML(todayFilename("motionCatarog"), savedimagePathDict);
            foreach (string s in targetMotionList)
            {
                if (quit_captureCoroutine) { quit_captureCoroutine = false; break; }
                string motionFileName = s;
                motionFileName = Util.forceEndsWith(motionFileName, ".anm");
                if (!maid.isValidMotionName(motionFileName)) continue;      //메이드라면 여성 모션 (* _f.anm) 만 재생할 수 있습니다.
                if (motionFileName.StartsWith("dance_")) continue;                                      //많은 수의 사용하기 어려운
                maid.change_Motion(motionFileName);
                Util.Info("모션 촬영：" + motionFileName);
                //toast(motionFileName, motionCaptureWait);
                yield return new WaitForSeconds(motionCaptureWait);
                string p = save_screen(motionFileName);
                savedimagePathDict[motionFileName] = p;
                yield return new WaitForSeconds(0.2f);
            }
            writeCatarogHTML(todayFilename("motionCatarog"), savedimagePathDict);
        }
        string[] excludePropCaptureArray = new string[] { "skin","body" ,
            "_i_hokuro","_i_lip","_i_eye","_i_mayu_",              //카탈로그에서보기 힘든 때문에 제외
            "dress237_mizugi_zurashi",               //오류가 발생하므로 제외
        };
        private System.Collections.IEnumerator _propCapture_Coroutine()
        {
            IMaid maid = maidList[0];
            float captureWait = 0.5f;
            try { captureWait = float.Parse(capture_WaitSecText); }
            catch (Exception e) { Util.Info(e.StackTrace); yield break; }
            maid.prop_snapshot();
            Util.Info("propCapture:searching prop files...");
            ICollection<string> propList = Util.GetFilenameList_byExtension(".menu", excludePropCaptureArray);
            Util.Info("propCapture:finished searching.");
            Util.Info("propCapture:start writing propName list...");
            writeTextListHTML(todayFilename("propList"), Util.list2Str(propList, "\r\n"));
            Util.Info("propCapture:finished writing.");
            SortedDictionary<string, string> savedimagePathDict = new SortedDictionary<string, string>();
            foreach (string s in propList)
            {
                string propFileName = Util.forceEndsWith(s, ".menu");
                savedimagePathDict[propFileName] = get_save_screen_relativeFilepath(propFileName);
            }
            writeCatarogHTML(todayFilename("propCatarog"), savedimagePathDict);
            foreach (string s in propList)
            {
                if (quit_captureCoroutine) { quit_captureCoroutine = false; break; }
                string propFileName = s;
                propFileName = Util.forceEndsWith(propFileName, ".menu");
                if (!maid.change_setProp(propFileName)) continue;
                Util.Info("prop撮影：" + propFileName);
                //toast(propFileName, captureWait);
                float wait = captureWait;
                if (propFileName.StartsWith("_i_mywear")) wait += 2;        //_i_mywear 여러 prop 세트에 갈아 시간이 소요된다
                yield return new WaitForSeconds(wait);
                string p = save_screen(propFileName);
                savedimagePathDict[propFileName] = p;
                maid.change_delProp(propFileName);                //maid.prop_restore()  //메이드의 외형을 기초로 되돌리고 싶은 느린。
                yield return new WaitForSeconds(0.2f);
            }
            writeCatarogHTML(todayFilename("propCatarog"), savedimagePathDict);
        }
        private string todayFilename(string prefixName, string suffix = ".html")
        {
            return prefixName + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + suffix;
        }
        private void writeCatarogHTML(string filename, SortedDictionary<string, string> savedimagePathDict)
        {
            StringBuilder str = new StringBuilder();
            str.Append(
$@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset = ""UTF-8"" >
    <title>" + filename + $@"</title>
  <style type=""text/css"">
figure {{
    display: inline-block;   
    margin: 0px 3px 7px 0px; 
    background-color: #ccc;  
}}
figure img {{
    display: block;          
    margin: 0px 0px 3px 0px; 
}}
figcaption {{
    font - size: 0.9em;        
    text-align: center;      
}}
  </style>
  </meta>
</head>
<body>
                ");
            foreach (KeyValuePair<string, string> kvp in savedimagePathDict)
            {
                string imagePath = kvp.Value.TrimStart('/');
                str.Append("<figure><img src=\"" + imagePath + "\" alt=\"" + kvp.Key + "\"><figcaption>" + kvp.Key + "</figcaption></figure>\r\n");
            }
            str.Append("</body></html>");
            writeAllText_asHTML(filename, str.ToString());
        }
        private void writeTextListHTML(string filename, string content)
        {
            string allContent = "<div style=\"white-space:pre-wrap; word-wrap:break-word;\"> \r\n" + content + "\r\n</div>";
            writeAllText_asHTML(filename, allContent);
        }
        private void writeAllText_asHTML(string filename, string content)
        {
            filename = Util.forceEndsWith(filename, ".html");
            string savepath = capture_saveBasePath + "/" + filename;
            createDirectory_fromFilepath_ifNotExist(savepath);
            File.WriteAllText(savepath, content);
        }
        private void createDirectory_fromFilepath_ifNotExist(string filePath)
        {
            string path = System.IO.Path.GetDirectoryName(filePath);
            if (Directory.Exists(path)) { return; }
            else Directory.CreateDirectory(path);
        }
        private string save_screen(string filename)
        {
            RECT r;
            IntPtr active = GetForegroundWindow();
            GetWindowRect(active, out r);
            //System.Drawing.*:UnityEngine.Colorなどと競合するため、usingで読み込めない
            //System.Drawing.*:UnityEngine.Colorなどと競合するため、usingで読み込めない
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }
            string relativeSavepath = get_save_screen_relativeFilepath(filename);
            string savePath = capture_saveBasePath + relativeSavepath;
            Util.Info("保存：" + savePath);
            createDirectory_fromFilepath_ifNotExist(savePath);
            bmp.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            return relativeSavepath;
        }
        private string get_save_screen_relativeFilepath(string filename)
        {
            return "/image/" + Util.forceEndsWith(filename, ".jpg");
        }
        public static void change_fadeOutCamera(float fadeTime = 0.5f)
        {
            GameMain.Instance.MainCamera.FadeOut(fadeTime);
        }
        public static void change_fadeInCamera(float fadeTime = 0.5f)
        {
            GameMain.Instance.MainCamera.FadeIn(fadeTime);
        }
        /// <summary>
        /// SE変更処理
        /// </summary>
        public static void change_SE(string seFileName, bool loop = true)
        {
            if (seFileName == "") { GameMain.Instance.SoundMgr.StopSe(); return; }
            seFileName = Util.forceEndsWith(seFileName, ".ogg");
            GameMain.Instance.SoundMgr.PlaySe(seFileName, loop);
        }
        public static void change_BGM(string seFileName)
        {
            if (seFileName == "") { GameMain.Instance.SoundMgr.StopBGM(1f); return; }
            seFileName = Util.forceEndsWith(seFileName, ".ogg");
            GameMain.Instance.SoundMgr.PlayBGM(seFileName, 0f, true);
        }
        public void change_SE_vibeLow()
        {
            change_SE("se020.ogg");
        }
        public void change_SE_vibeHigh()
        {
            change_SE("se019.ogg");
        }
        public void change_SE_stop()
        {
            change_SE("");
        }
        public void change_SE_insertLow()
        {
            change_SE("se029.ogg");
        }
        public void change_SE_insertHigh()
        {
            change_SE("se028.ogg");
        }
        public void change_SE_slapLow()
        {
            change_SE("se012.ogg");
        }
        public void change_SE_slapHigh()
        {
            change_SE("se013.ogg");
        }
        public static void change_BackGround(string backgroundName)
        {
            GameMain.Instance.BgMgr.ChangeBg(backgroundName);
        }
        /// <summary>
        /// メイド操作用オブジェクト
        /// </summary>
        public class IMaid
        {
            public readonly bool isMan;
            public readonly Maid maid;
            public string sPersonal;      //性格名 ex.Muku
            public bool loopVoiceBackuped = false; //OnceVoice再生で遮られたLoopVoiceを復元する必要あるか？
            public string currentLoopVoice = "";
            private string currentFaceAnime = "";
            public IMaid(int maidNo, Maid maid, bool isMan = false)
            {
                this.isMan = isMan;
                this.maid = maid;
                this.sPersonal = maid.status.personal.uniqueName;
                this.maidNo = maidNo;

                if (maid.body0.boHeadToCam == false)
                {
                    headToCam_state = EyeHeadToCamState.No;
                }
                else if(maid.body0.boHeadToCam == true)
                {
                    headToCam_state = EyeHeadToCamState.Yes;
                }

                if (maid.body0.boEyeToCam == false)
                {
                    eyeToCam_state = EyeHeadToCamState.No;
                }
                else if (maid.body0.boEyeToCam == true)
                {
                    eyeToCam_state = EyeHeadToCamState.Yes;
                }

                    // 얼굴과 눈의 추종을 활성화
                    //this.maid.EyeToCamera((Maid.EyeMoveType)5, 0.8f); //顔と目の追従を有効にする fadeTime=0.8sec 
                    //        public enum EyeMoveType
                    //{
                    //    無し,
                    //    無視する,
                    //    顔を向ける,
                    //    顔だけ動かす,
                    //    顔をそらす,
                    //    目と顔を向ける,      ←　これ
                    //    目だけ向ける,
                    //    目だけそらす
                    //}
                }
            public enum PlayingVoiceState
            {
                OnceVoice, LoopVoice, None
            }
            public PlayingVoiceState getPlayingVoiceState()
            {
                if (!maid.AudioMan.audiosource.isPlaying) return PlayingVoiceState.None;
                if (maid.AudioMan.audiosource.loop) return PlayingVoiceState.LoopVoice;
                return PlayingVoiceState.OnceVoice;
            }
            public bool isPlayingMotion()
            {
                //Unity Animation.IsPlaying
                //https://docs.unity3d.com/ScriptReference/Animation.IsPlaying.html
                return maid.body0.m_Animation.isPlaying;
                //                return maid.body0.GetAnimation().isPlaying;       //これも可能。VYMより
            }
            public string getCurrentMotionName() { return maid.body0.LastAnimeFN; }
            /// <summary>
            /// 再生中のボイスを返す
            /// </summary>
            /// <param name="maid"></param>
            /// <returns>再生ナシなら"", 再生中なら*.ogg</returns>
            public string getPlayingVoice()
            {
                if (!maid.AudioMan.isPlay()) return "";
                return maid.AudioMan.FileName;
            }
            /// <summary>
            /// メイドの位置を相対座標で指定した分だけ移動
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public Vector3 change_positionRelative(float x = 0, float y = 0, float z = 0)
            {
                Vector3 v = maid.transform.position;
                maid.transform.position = new Vector3(v.x + x, v.y + y, v.z + z);
                return maid.transform.position;
            }
            /// <summary>
            /// メイドの位置を絶対座標で指定
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public Vector3 change_positionAbsolute(float x = 0, float y = 0, float z = 0, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 v = maid.transform.eulerAngles;
                maid.transform.position = new Vector3(keepX ? v.x : x, keepY ? v.y : y, keepZ ? v.z : z);
                return maid.transform.position;
            }
            /// <summary>
            /// メイドの現在位置を絶対座標で返す\
            /// 메이드의 현재 위치를 절대 좌표로 반환
            /// </summary>
            /// <returns></returns>
            public Vector3 getPosition()
            {
                return maid.transform.position;
            }
            /// <summary>
            /// メイドの向きを絶対座標系の向き（度）で指定
            /// 메이드의 방향을 절대 좌표계의 방향 (각도)으로 지정
            /// </summary>
            /// <param name="x_deg"></param>
            /// <param name="y_deg"></param>
            /// <param name="z_deg"></param>
            /// <returns></returns>
            public Vector3 change_angleAbsolute(float x_deg = 0, float y_deg = 0, float z_deg = 0, bool keepX = false, bool keepY = false, bool keepZ = false)
            {
                Vector3 old = maid.transform.eulerAngles;
                Vector3 v = new Vector3(keepX ? old.x : x_deg, keepY ? old.y : y_deg, keepZ ? old.z : z_deg);
                Util.Debug(string.Format("IMaid change_angleAbsolute:{0}", v.ToString()));
                maid.transform.eulerAngles = v;
                return maid.transform.eulerAngles;
            }
            /// <summary>
            /// 指定角度だけメイドの向きを変える
            /// </summary>
            /// <param name="x_deg"></param>
            /// <param name="y_deg"></param>
            /// <param name="z_deg"></param>
            /// <returns></returns>
            public Vector3 change_angleRelative(float x_deg = 0, float y_deg = 0, float z_deg = 0)
            {
                Vector3 v = maid.transform.eulerAngles;
                maid.transform.eulerAngles = new Vector3(v.x + x_deg, v.y + y_deg, v.z + z_deg);
                return maid.transform.eulerAngles;
            }
            /// <summary>
            /// メイドの向きを絶対座標系の向きで返す
            /// 메이드의 방향을 절대 좌표계의 방향으로 반환
            /// </summary>
            /// <returns></returns>
            public Vector3 getAngle()
            {
                return maid.transform.eulerAngles;
            }
            /// <summary>
            /// 現在のメイド状態などに合わせて表情変更
            /// 현재 메이드 상태 등에 따라 표정 변화
            /// </summary>
            public void change_faceAnime(string faceAnime, float fadeTime = -1)
            {
                if (isMan) { Util.Info("ご主人様の表情は変更できません주인님의 표정은 변경할 수 없습니다"); return; }
                if (currentFaceAnime == faceAnime) return;
                if (fadeTime == -1) fadeTime = cfg.faceAnimeFadeTime;
                currentFaceAnime = faceAnime;
                //TODO　Unityの把握している表情一覧に照らし合わせて、存在しない表情なら実行しない
                // Unity 파악하고있는 표정 목록에 비추어 존재하지 않는 표정이라면 실행하지
                maid.FaceAnime(currentFaceAnime, fadeTime, 0);
            }
            public void change_faceAnime(List<string> faceAnimeList, float fadeTime = -1)
            {
                string face = Util.pickOneOrEmptyString(faceAnimeList);
                if (face.Equals(""))
                {
                    Util.Info("表情リストが空でした");
                    return;
                }
                change_faceAnime(face, fadeTime);
            }
            Regex reg_hoho = new Regex(@"(頬.)");
            Regex reg_namida = new Regex(@"(涙.)");
            /// <summary>
            /// 頬・涙・よだれ　を設定
            /// 뺨 눈물 · 침을 설정
            /// </summary>
            /// <param name="hoho">0~3で指定、-1なら変更しない</param>
            /// <param name="namida">0~3で指定、-1なら変更しない</param>
            /// <param name="enableYodare"></param>
            /// <returns></returns>
            public string change_FaceBlend(int hoho = -1, int namida = -1, bool enableYodare = false)
            {
                if (isMan) { Util.Info("ご主人様の表情は変更できません"); return ""; }
                //頬・涙を-1,0,1,2,3のいずれかへ制限
                hoho = (int)Mathf.Clamp(hoho, -1, 3);
                namida = (int)Mathf.Clamp(namida, -1, 3);
                string currentFaceBlend = maid.FaceName3;
                if (currentFaceBlend.Equals("オリジナル")) currentFaceBlend = "頬０涙０";    //初期状態ではオリジナルとなっているため、整合取る
                string cheekStr = reg_hoho.Match(currentFaceBlend).Groups[1].Value;         //現在の頬値で初期化
                string tearsStr = reg_namida.Match(currentFaceBlend).Groups[1].Value;
                string yodareStr = "";
                if (hoho == 0) cheekStr = "頬０";
                if (hoho == 1) cheekStr = "頬１";
                if (hoho == 2) cheekStr = "頬２";
                if (hoho == 3) cheekStr = "頬３";
                if (namida == 0) tearsStr = "涙０";
                if (namida == 1) tearsStr = "涙１";
                if (namida == 2) tearsStr = "涙２";
                if (namida == 3) tearsStr = "涙３";
                if (enableYodare) yodareStr = "よだれ";
                string blendSetStr = cheekStr + tearsStr + yodareStr;
                maid.FaceBlend(blendSetStr);
                return blendSetStr;
            }
            /// <summary>
            /// 瞳のY位置操作
            /// 実際の指定値は０～５０にリスケール
            /// 눈동자의 Y 위치 조작
            /// </summary>
            /// <param name="eyePosY">両目の瞳Y位置　0~1で指定。　０で初期値</param>
            public void change_eyePosY(float eyePosY = 0f)
            {
                if (isMan) { Util.Info("ご主人様の目線は変更できません"); return; }
                eyePosY *= 50;
                eyePosY = Mathf.Clamp(eyePosY, 0f, 50f);    //最大値は不明　50？
                Vector3 vl = maid.body0.trsEyeL.localPosition;
                Vector3 vr = maid.body0.trsEyeR.localPosition;
                const float fEyePosToSliderMul = 5000f;
                maid.body0.trsEyeL.localPosition = new Vector3(vl.x, Math.Max(eyePosY / fEyePosToSliderMul, 0f), vl.z);
                maid.body0.trsEyeR.localPosition = new Vector3(vr.x, -Math.Max(eyePosY / fEyePosToSliderMul, 0f), vr.z);
            }
            /// <summary>
            /// 各種再生処理の一括変更
            /// MaidState、感度、バイブ強度、
            /// </summary>
            public void update()
            {
                if (propChanged) { maid.AllProcPropSeqStart(); propChanged = false; }
                if (loopVoiceBackuped)
                {
                    //　LoopVoiceを再生中、もしくはOnceVoice音声が再生済みなら介入してよい
                    //if (maid.AudioMan.audiosource.loop || (!maid.AudioMan.audiosource.loop && !maid.AudioMan.audiosource.isPlaying))
                    //何も音声再生していない（≒OnceVoice再生終了）ならLoopVoice再生
                    // LoopVoice을 재생 중이거나 OnceVoice 음성이 재생 된 경우 개입 할 수있다
                    // if (maid.AudioMan.audiosource.loop || (! maid.AudioMan.audiosource.loop &&! maid.AudioMan.audiosource.isPlaying))
                    // 아무것도 음성 재생하지 (≒ OnceVoice 재생 종료)이라면 LoopVoice 재생
                    if (getPlayingVoiceState() == PlayingVoiceState.None)
                    {
                        change_LoopVoice(currentLoopVoice);
                        //change_LoopVoice();
                        loopVoiceBackuped = false;
                    }
                }
                if (voicePlayIntervalSec > 0f)
                {
                    voicePlayIntervalSec -= Time.deltaTime;
                    if (voicePlayIntervalSec <= 0f)
                    {
                        voicePlayIntervalSec = 0f;
                        change_stopVoice();
                    }
                }
                //비슷한 모션의 미세 수정
                if (similarMotion_intervalSec > 0f)
                {
                    similarMotion_intervalSec -= Time.deltaTime;
                    if (enable_similarMotion && similarMotion_intervalSec < 0f)
                    {
                        similarMotion_intervalSec = Util.var20p(cfg.similarMotion_intervalSec);
                        string mb = !motionNameBase.Equals("") ? motionNameBase : getMotionNameBase(getCurrentMotionName());
                        List<string> motionList = searchMotionList(mb, similarMotion_attList);
                        if (motionList.Count != 0)
                        {
                            string motion = Util.pickOneOrEmptyString(motionList);
                            if (motion != getCurrentMotionName())
                            {
                                change_Motion(motion, isLoop: true, motionSpeed: similarMotion_speed, similarMotionSec: similarMotion_intervalSec, updateMotionNameBase: false);
                            }
                        }
                        else
                        {
                            Util.Info(string.Format("비슷한 모션를 찾을 수 없습니다. 현재 모션：{0}", getCurrentMotionName()));
                            enable_similarMotion = false;
                        }
                    }
                }
                if (!isPlayingMotion() && afterMotion_name != "")
                {
                    change_Motion(afterMotion_name, isLoop: true, motionSpeed: afterMotion_speed);
                    afterMotion_name = "";
                }
                //顔の向き・目線の更新
                //얼굴의 방향 · 시선의 업데이트
                update_eyeToCam();
                update_headToCam();
            }
            private bool enable_similarMotion = false;
            private List<string> similarMotion_attList = new List<string>();
            private float similarMotion_intervalSec = -1f;
            private float similarMotion_speed = -1f;
            private string afterMotion_name = "";
            private float afterMotion_speed = 1;
            private float eyeToCam_turnSec = 0;
            private float headToCam_turnSec = 0;
            private EyeHeadToCamState eyeToCam_state = EyeHeadToCamState.Auto;
            private EyeHeadToCamState headToCam_state = EyeHeadToCamState.Auto;
            /// <summary>
            /// 目線・顔をカメラへ向けるかの状態管理.
            /// </summary>
            public class EyeHeadToCamState
            {
                private static int nextOrdinal = 0;
                public static readonly List<EyeHeadToCamState> items = new List<EyeHeadToCamState>();
                //フィールド一覧
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly string viewStr;
                //コンストラクタ
                private EyeHeadToCamState(string viewStr)
                {
                    this.viewStr = viewStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    items.Add(this);
                }
                // 参照用インスタンス
                public static readonly EyeHeadToCamState No = new EyeHeadToCamState("no");
                public static readonly EyeHeadToCamState Auto = new EyeHeadToCamState("auto");
                public static readonly EyeHeadToCamState Yes = new EyeHeadToCamState("yes");
            }
            public void change_eyeToCam(EyeHeadToCamState state)
            {
                if (isMan) { Util.Info("ご主人様の目線は変更できません주인님의 시선은 변경할 수 없습니다"); return; }
                this.eyeToCam_state = state;
            }
            public void change_headToCam(EyeHeadToCamState state, float fadeSec = -1f)
            {
                this.headToCam_state = state;
                if (fadeSec != -1)
                {
                    maid.body0.HeadToCamFadeSpeed = fadeSec;
                }
            }
            /// <summary>
            ///目線更新
            /// ６～１０秒ごとに50％の確率で目線変更
            /// </summary>
            private void update_eyeToCam()
            {
                if (eyeToCam_state == EyeHeadToCamState.No)
                {
                    maid.body0.boEyeToCam = false;
                    return;
                }
                else if (eyeToCam_state == EyeHeadToCamState.Yes)
                {
                    maid.body0.boEyeToCam = true;
                    return;
                }
                else if (eyeToCam_state == EyeHeadToCamState.Auto)
                {
                    eyeToCam_turnSec -= Time.deltaTime;
                    if (eyeToCam_turnSec > 0) return;
                    if (UnityEngine.Random.Range(0, 100) < 50) maid.body0.boEyeToCam = !maid.body0.boEyeToCam;
                    eyeToCam_turnSec = UnityEngine.Random.Range(6, 10);  //6~10秒ごとに変える
                }
            }
            /// <summary>
            /// 顔の向き更新
            /// 6~10秒ごとにカメラを向くorそっぽ向く
            /// </summary>
            private void update_headToCam()
            {
                if (headToCam_state == EyeHeadToCamState.No)
                {
                    maid.body0.boHeadToCam = false;
                    return;
                }
                else if (headToCam_state == EyeHeadToCamState.Yes)
                {
                    maid.body0.boHeadToCam = true;
                    return;
                }
                else if (headToCam_state == EyeHeadToCamState.Auto)
                {
                    headToCam_turnSec -= Time.deltaTime;
                    if (headToCam_turnSec > 0) return;
                    if (maid.body0.boHeadToCam)
                    {
                        if (UnityEngine.Random.Range(0, 100) < 70) maid.body0.boHeadToCam = !maid.body0.boHeadToCam;
                    }
                    else
                    {
                        if (UnityEngine.Random.Range(0, 100) < 30) maid.body0.boHeadToCam = !maid.body0.boHeadToCam;
                    }
                    headToCam_turnSec = UnityEngine.Random.Range(6, 10);  //6~10秒ごとに変える
                }
            }
            /// <summary>
            /// このメイドに対するLateUpdate処理
            /// 毎フレームのLateUpdate時に1回呼び出すこと
            /// </summary>
            public void LateUpdate()
            {
                VertexMorph_FixBlendValues();
            }
            //モーション制御属性
            string[] motionControlAttList =
            {
                "_f_",      //女性用モーションカテゴリ
                "_f.anm",   //女性用モーションカテゴリ
                "_f2_",      //複数プレイ時女２のモーション
                "_f2.anm",   //複数プレイ時女２のモーション
                "_m_",      //男１モーション
                "_m_.anm",  //男１モーション
                "_m2_",      //男２モーション
                "_m2_.anm",  //男２モーション
                "_once_",    //非ループモーション
                            };
            /// <summary>
            /// モーション名から制御属性のリストを推定
            /// </summary>
            /// <param name="motionName"></param>
            /// <returns></returns>
            public List<string> inferMotionControlAttList(string motionName)
            {
                List<string> ret = new List<string>();
                foreach (string s in motionControlAttList)
                {
                    if (motionName.Contains(s)) ret.Add(s);
                }
                return ret;
            }
            /// <summary>
            /// 通常のモーション名からモーションのベースとなる名前を推定する
            /// 例：dildo_onani_1a01_f.anm　-> dildo_onani_
            /// </summary>
            /// <param name="motionName"></param>
            /// <param name="strict">false:同レベルの微変更のためのベースネーム検出（ex. _1a01 > _1b02) true:モーションカテゴリのベース名　(ex. xx_taiki > xx)</param>
            /// <returns></returns>
            public string getMotionNameBase(string motionName, bool strict = false)
            {
                if (motionName == null) return motionName;
                string ret = motionName;     //_1_...　などを除去
                Dictionary<Regex, string> dic = strict ? motionBaseRegexDic_strict : motionBaseRegexDic;
                foreach (KeyValuePair<Regex, string> kvp in dic)
                {
                    Regex regex = kvp.Key;
                    //_1.+　などにマッチしたら、一致以降のモーション名部分を除去することでモーションベース名を求める
                    ret = regex.Replace(ret, kvp.Value);
                }
                Util.Debug(string.Format("getMotionNameBase {0} -> {1}", motionName, ret));
                return ret;
            }
            public bool isValidMotionName(string motionFullName)
            {
                if (isMan)
                {
                    if (motionFullName.EndsWith("_m")) return true;
                    if (motionFullName.EndsWith("_m2")) return true;
                    if (motionFullName.Contains("_m_")) return true;
                    if (motionFullName.Contains("_m2_")) return true;
                }
                else
                {
                    if (motionFullName.EndsWith("_f")) return true;
                    if (motionFullName.EndsWith("_f2")) return true;
                    if (motionFullName.Contains("_f_")) return true;
                    if (motionFullName.Contains("_f2_")) return true;
                }
                return false;
            }
            public List<string> searchMotionListByArray(string motionNameBase, string[] requiredAttArray = null)
            {
                if (requiredAttArray == null) return searchMotionList(motionNameBase, null);
                else return searchMotionList(motionNameBase, new List<string>(requiredAttArray));
            }
            /// <summary>
            /// 유효한 모션 이름 찾기
            /// motionNameBase에서 시작 모션 이름의 목록을 반환
            /// requiredAttList을 지정한 경우에는 모든 attribute를 포함 모션 이름의 목록을 반환
            /// </summary>
            /// <param name="motionNameBase"></param>
            /// <param name="requiredAttList"></param>
            /// <param name="eitherAttList">検索条件：하나의 문자열을 포함하는 모션 이름</param>
            /// <returns></returns>
            public List<string> searchMotionList(string motionNameBase, List<string> requiredAttList = null)
            {
                motionNameBase = getMotionNameBase(motionNameBase);
                if (requiredAttList == null) requiredAttList = new List<string>();
                List<string> possibleMotionList = new List<string>();
                if (motionNameBase == null) return possibleMotionList;
                //motionNameAllList.FindAll(s => s.Contains(motionNameBase)).FindAll(s => s.Contains(addition));
                foreach (string mn in motionNameAllList)
                {
                    if (!mn.StartsWith(motionNameBase)) continue;
                    bool possible = true;
                    foreach (string att in requiredAttList)
                    {
                        if (!mn.Contains(att)) { possible = false; /*Util.debug(string.Format("モーション名「{0}」は「{1}」を含まないので除外", mn, att));*/ goto NEXT_MOTION_NAME; }
                    }
                    if (!isValidMotionName(mn)) { /*Util.debug(string.Format("モーション名「{0}」は適切なモーション名でないので除外", mn));*/ continue; }
                    possibleMotionList.Add(mn);
                    NEXT_MOTION_NAME:;
                }
                if (possibleMotionList.Count == 0)
                {
                    Util.Debug(string.Format("searchMotionList:유효한 모션이 없습니다 : {0} \r\n motionBaseName : {1},  全モーション数:{2}",
                        this.maid.name, motionNameBase, motionNameAllList.Count));
                }
                else
                {
                    Util.Debug(string.Format("searchMotionList:유효한 모션을 찾았습니다　: {0} \r\n motionBaseName : {1},  全モーション数:{2}, \r\n 見つけたモーション : {3}",
                 this.maid.name, motionNameBase, motionNameAllList.Count, Util.list2Str(possibleMotionList)));
                }
                return possibleMotionList;
            }
            /// <summary>
            /// 지정한 모션을 실행
            /// </summary>
            /// <param name="motionName"></param>
            /// <param name="isLoop"></param>
            /// <param name="addQue"></param>
            /// <param name="motionSpeed"></param>
            /// <param name="similarMotionSec">-1:off, 0:random, >0:지정 시간에서 모션 변경</param>
            /// <param name="fadeTime"></param>
            /// <returns></returns>
            public string change_Motion(string motionName, bool isLoop = true, bool addQue = false, float motionSpeed = -1, float fadeTime = -1, float similarMotionSec = -1, bool updateMotionNameBase = true, float afterSpeed = -1)
            {
                Util.Info("change_Motion1 : " + motionName);
                List<string> ctrlAttList = inferMotionControlAttList(motionName);
                string motionbase = getMotionNameBase(motionName);
                if (motionSpeed == -1) motionSpeed = Util.var20p(1f);
                if (fadeTime == -1) fadeTime = Util.var20p(0.8f);
                afterMotion_name = "";
                if (ctrlAttList.Contains("_once_"))
                {
                    afterMotion_speed = (afterSpeed == -1) ? 1 : afterSpeed;
                    afterMotion_name = Util.pickOneOrEmptyString(searchMotionListByArray(motionbase, new string[] { "_shaseigo_" }));
                    if (afterMotion_name == "") afterMotion_name = Util.pickOneOrEmptyString(searchMotionListByArray(motionbase, new string[] { "_zeccyougo_" }));
                    if (afterMotion_name == "") afterMotion_name = Util.pickOneOrEmptyString(searchMotionListByArray(motionbase, new string[] { "_idougo_" }));
                    if (afterMotion_name == "") afterMotion_name = Util.pickOneOrEmptyString(searchMotionListByArray(motionbase, new string[] { "_taiki_" }));
                    if (afterMotion_name != "")
                    {
                        Util.Info("onceモーション「" + motionName + "」の後「" + afterMotion_name + "」를 실행합니다。");
                        isLoop = false;
                    }
                    else { Util.Info("onceモーション「" + motionName + "」다음에 수행 할 동작을 찾을 수 없습니다"); }
                }
                if (similarMotionSec > 0)
                {
                    enable_similarMotion = true;
                    similarMotion_intervalSec = similarMotionSec == 0 ? Util.var20p(cfg.similarMotion_intervalSec) : similarMotionSec;
                    similarMotion_speed = motionSpeed;
                    similarMotion_attList.Clear();
                    similarMotion_attList.AddRange(ctrlAttList);
                    List<string> motionList = searchMotionList(motionbase, similarMotion_attList);
                    if (motionList.Count != 0) motionName = Util.pickOneOrEmptyString(motionList);
                }
                else
                {
                    enable_similarMotion = false;
                    similarMotion_attList.Clear();
                }
                if (!motionNameAllList.Contains(motionName))
                {
                    List<string> motionList = searchMotionList(motionbase, ctrlAttList);
                    if (motionList.Count != 0) motionName = Util.pickOneOrEmptyString(motionList);
                    else Util.Info(string.Format("「{0}」부터 시작 모션를 찾을 수 없습니다。", motionName));
                }
                //motionName = Util.forceEndsWith(motionName, ".anm");
                if (updateMotionNameBase) this.motionNameBase = motionbase;
                //if (motionNameOrNameBase == getCurrentMotionName())       //모션 같아도 속도 바꾸고 싶을 때이 있기 때문에、許容
                //{
                //    Util.info(string.Format("Maid {0} change_motion : モーションを変更しませんでした。変更前後のモーション名が同じため。", this.maidNo));//現在再生しているモーションを再生しようとすると動作が不連続になるため
                //    return motionNameOrNameBase;
                //}
                Util.animate(maid, motionName, isLoop: isLoop, fadeTime: fadeTime, speed: motionSpeed, addQue: addQue);
                return motionName;
            }
            /// <summary>
            /// 모션 목록 중에서 하나를 선택 실행
            /// 그러나 현재 실행중인 모션을 선택하지
            /// 기존에 플레이 중인 모션은 제외
            /// </summary>
            /// <param name="motionList"></param>
            /// <param name="isLoop"></param>
            /// <returns></returns>
            public string change_Motion(List<string> motionList, bool isLoop = true, bool addQue = false, float motionSpeed = -1, float fadeTime = -1, float similarMotionSec = -1, float afterSpeed = -1)
            {
                int currentIndex = motionList.IndexOf(getCurrentMotionName());   //찾을 수없는 경우-1
                int randomIndex = Util.randomInt(0, motionList.Count - 1, currentIndex);
                //  Util.info("change_Motion2 : " + motionList[randomIndex]);
                return change_Motion(motionList[randomIndex], isLoop: isLoop, addQue: addQue, motionSpeed: motionSpeed, fadeTime: fadeTime, similarMotionSec: similarMotionSec, afterSpeed: afterSpeed);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="motionList"></param>
            /// <param name="isLoop"></param>
            /// <param name="addQue"></param>
            /// <param name="motionSpeed"></param>
            /// <param name="fadeTime"></param>
            /// <param name="similarMotionSec"></param>
            /// <param name="afterSpeed"></param>
            /// <returns></returns>
            public string change_Motion(List<MotionInfo> motionList, bool isLoop = true, bool addQue = false, float motionSpeed = -1, float fadeTime = -1, float similarMotionSec = -1, float afterSpeed = -1)
            {
                List<string> list = new List<string>();
                foreach (MotionInfo mi in motionList)
                {
                    list.Add(mi.motionName);
                }
                return change_Motion(list, isLoop: isLoop, addQue: addQue, motionSpeed: motionSpeed, fadeTime: fadeTime, similarMotionSec: similarMotionSec, afterSpeed: afterSpeed);
            }
            private ICollection<string> prefabSet = new HashSet<string>();
            private void change_setPrefab(string f_strPrefab, string f_strName, string f_strDestBone, Vector3 f_vOffsetLocalPos, Vector3 f_vOffsetLocalRot)
            {
                maid.AddPrefab(f_strPrefab, f_strName, f_strDestBone, f_vOffsetLocalPos, f_vOffsetLocalRot);
                prefabSet.Add(f_strName);
            }
            private void change_delPrefab(string f_strName)
            {
                maid.DelPrefab(f_strName);
                prefabSet.Remove(f_strName);
            }
            public void change_removeAllPrefab() { foreach (string s in prefabSet) { change_delPrefab(s); } }
            public void change_toiki1(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pToiki", "toiki1", "Bip01 Head", new Vector3(0.042f, 0.076f, 0f), new Vector3(-90f, 90f, 0f));
                else change_delPrefab("toiki1");
            }
            public void change_toiki2(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pToiki", "toiki2", "Bip01 Head", new Vector3(0.042f, 0.076f, 0f), new Vector3(-90f, 90f, 0f));
                else change_delPrefab("toiki2");
            }
            public void change_aieki1(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pPistonEasy_cm3D2", "aieki1", "_IK_vagina", new Vector3(0f, 0f, 0.01f), new Vector3(0f, -180f, 90f));
                else change_delPrefab("aieki1");
            }
            public void change_aieki2(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pPistonNormal_cm3D2", "aieki2", "_IK_vagina", new Vector3(0f, 0f, 0.01f), new Vector3(0f, -180f, 90f));
                else change_delPrefab("aieki2");
            }
            public void change_aieki3(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pPistonHard_cm3D2", "aieki3", "_IK_vagina", new Vector3(0f, 0f, 0.01f), new Vector3(0f, -180f, 90f));
                else change_delPrefab("aieki3");
            }
            public void change_nyo(bool enable = true)
            {
                if (enable)
                {
                    string nyoSound = "SE011.ogg";
                    change_SE(nyoSound, false);
                    change_setPrefab("Particle/pNyou_cm3D2", "nyo", "_IK_vagina", new Vector3(0f, -0.047f, 0.011f), new Vector3(20.0f, -180.0f, 180.0f));
                }
                else { change_SE(""); change_delPrefab("nyo"); }
            }
            public void change_sio(bool enable = true)
            {
                if (enable) change_setPrefab("Particle/pSio2_cm3D2", "sio", "_IK_vagina", new Vector3(0f, 0f, -0.01f), new Vector3(0f, 180.0f, 0f));
                else change_delPrefab("sio");
            }
            Dictionary<string, string[]> slotSetDict = new Dictionary<string, string[]>()
            {
                {"all",new string[] {"wear", "mizugi", "onepiece", "bra", "skirt", "panz", "glove", "accUde", "stkg", "shoes", "accKubi", "accKubiwa"} },
                {"overwear",new string[] {"wear",  "onepiece",  "skirt",   "shoes", "accKubi", "accKubiwa"} },
                {"exceptacc",new string[] {"wear", "mizugi", "onepiece", "bra", "skirt", "panz",  "stkg"} },
            };
            public void change_slot(TBody.SlotID slotIDname, bool visible)
            {
                maid.body0.SetMask(slotIDname, visible);
            }
            public void change_slot(string slotname_or_mpnName_or_SlotCategory, bool visible)
            {
                string maySlotCategory = slotname_or_mpnName_or_SlotCategory;
                if (slotSetDict.ContainsKey(maySlotCategory.ToLower()))
                {
                    foreach (string s in slotSetDict[maySlotCategory]) { change_slot(s, visible); }
                    return;
                }
                try
                {
                    string mayMPNname = slotname_or_mpnName_or_SlotCategory;
                    MPN mpn = getMPN(mayMPNname);
                    if (mpn == MPN.acckami) //例外的にMPNとSlotIDの名前が一致せず、多対一の関係のため
                    {
                        change_slot(TBody.SlotID.accKami_1_, visible);
                        change_slot(TBody.SlotID.accKami_2_, visible);
                        change_slot(TBody.SlotID.accKami_3_, visible);
                        return;
                    }
                    else if (mpn == MPN.acckamisub)
                    {
                        change_slot(TBody.SlotID.accKamiSubL, visible);
                        change_slot(TBody.SlotID.accKamiSubR, visible);
                        return;
                    }
                    else
                    {
                        string maySlotname = slotname_or_mpnName_or_SlotCategory;
                        change_slot(getSlotID_orThrow(maySlotname), visible); return;
                    }
                }
                catch (Exception e) { Util.Info(e.Message); }
                Util.Info(string.Format("「{0}」という名前のSlotID/SlotCategoryは見つかりませんでした。", slotname_or_mpnName_or_SlotCategory));
            }
            //prop名によく使われる、MPNの短縮形の対応
            public static Dictionary<string, string> MPN_abbreviation_mapping = new Dictionary<string, string>()
            {
                {"kousokul", "kousoku_lower" },
                {"kousokuu", "kousoku_upper" },
                {"skrt", "skirt" },                 //dress_cmo_001_z5_skrt_i_
                {"shoe", "shoes" },     //dress_cmo_001_z4_shoe_i_
                {"underwea", "underwear" },     //_i_underwea_dressr014_z3
                {"pants", "panz" },     //dress019_z4_pants_i_
                {"onep", "onepiece" },     //dress005_z2_onep_i_
                {"tatoo", "acctatoo" },     //tatoo923_i_
            };
            public bool change_setProp(string propFilename, string mpnName = "", int f_nFileNameRID = 0)
            {
                if (propFilename == "") return false;
                MPN mpn = MPN.null_mpn;
                if (mpnName != "") mpn = getMPN(mpnName);
                if (mpnName == "")
                {
                    foreach (MPN id in Util.getMPN_reversedList())  //先頭にMPNが含まれるなら      accashi007_i_
                    {
                        string s = Util.getMpnName(id);
                        if (propFilename.ToLower().StartsWith(s.ToLower())) { mpnName = s; mpn = id; break; }
                    }
                }
                if (mpnName == "")
                {
                    //ファイル名の中間にMPNが含まれるなら   _i_underhair_folder_006
                    foreach (MPN id in Util.getMPN_reversedList())
                    {
                        string s = Util.getMpnName(id);
                        if (propFilename.ToLower().Contains(s.ToLower())) { mpnName = s; mpn = id; break; }
                    }
                }
                if (mpnName == "")
                {
                    //短縮系   kousokul_ashikasedownout_i_　　kousokul->kousoku_lower
                    foreach (KeyValuePair<string, string> kvp in MPN_abbreviation_mapping)
                    {
                        if (propFilename.ToLower().Contains(kvp.Key.ToLower())) { mpnName = kvp.Value; mpn = getMPN(mpnName); break; }
                    }
                }
                if (mpnName == "") { Util.Info(propFilename + "という名前のpropがどのMPN部位のものかわかりません。MPNを明示してください。"); return false; }
                return change_setProp(propFilename, mpn, f_nFileNameRID);
            }
            public bool change_setProp(string propFilename, MPN mpn, int f_nFileNameRID = 0)
            {
                if (propFilename == "") return false;
                foreach (MPN banned in new MPN[] { MPN.body, MPN.set_body, MPN.head, MPN.null_mpn })
                {
                    if (mpn == banned) { Util.Info("setProp:Scriplayから" + banned + "にpropをセットすることはできません。"); return false; }
                }
                propFilename = Util.forceEndsWith(propFilename, ".menu");
                maid.SetProp(mpn, propFilename, f_nFileNameRID, true, false); //tagは装着部位のslot名と同じものとする。
                propChanged = true;
                Util.Debug(Util.getMpnName(mpn) + "に" + propFilename + "というpropをセットしました");
                try
                {
                    change_slot(Util.getMpnName(mpn), true);
                }
                catch (Exception e) { Util.Info(e.Message); return true; }
                return true;
            }
            public void change_delProp(string propFilename)
            {
                MPN targetMPN = getMPN_fromPropFilename(propFilename);
                if (targetMPN == MPN.null_mpn) return;
                change_delProp(targetMPN);
            }
            public void change_delProp_byMPNName(string MPNName)
            {
                MPN m = getMPN(MPNName);
                if (m == MPN.null_mpn) return;
                change_delProp(m);
            }
            public void change_delProp(MPN mpn)
            {
                maid.DelProp(mpn, true);
                propChanged = true;
            }
            public TBody.SlotID getSlotID_fromPropFilename_orThrow(string propFilename)
            {
                MPN mpn = getMPN_fromPropFilename(propFilename);
                return getSlotID_orThrow(Util.getMpnName(mpn)); //MPNとSlotIDの変数名が同じことを利用してslotIDを取得
            }
            public MPN getMPN_fromPropFilename(string propFilename)
            {
                propFilename = Util.forceEndsWith(propFilename, ".menu");
                foreach (MPN m in Util.getMPN_reversedList())
                {
                    //Util.debug("getMPN_fromPropFilename : " + Util.getMpnName(m));  //TODO デバッグ終わったら除去
                    MaidProp mp = maid.GetProp(m);
                    if (mp.strTempFileName.ToLower() == propFilename.ToLower()) { return m; }
                }
                Util.Info(propFilename + "という名前のpropが設定されたMPNは見つかりませんでした。");
                return MPN.null_mpn;
            }
            public static TBody.SlotID getSlotID_orThrow(string slotName)
            {
                foreach (TBody.SlotID id in Enum.GetValues(typeof(TBody.SlotID)))
                {
                    if (slotName.ToLower().Equals(Util.getSlotName(id).ToLower())) { return id; }
                }
                throw new Exception(slotName + "という名前のSlotIDは見つかりませんでした。");
            }
            public static MPN getMPN(string mpnName)
            {
                foreach (MPN id in Util.getMPN_reversedList())
                {
                    if (mpnName.ToLower().Equals(Util.getMpnName(id).ToLower())) return id;
                }
                Util.Info(mpnName + "という名前のMPNは見つかりませんでした。");
                return MPN.null_mpn;
            }
            public static Dictionary<string, string> cloth_dict = new Dictionary<string, string>()
            {
                {"accashi", "足首"},
                {"acchana", "鼻"},
                {"acchat",  "帽子"},
                {"acchead", "ｱｲﾏｽｸ"},
                {"accheso", "へそ"},
                {"acckami", "前髪アクセサリ"},
                {"acckamisub",  "リボン"},
                {"acckubi", "ネックレス"},
                {"acckubiwa",   "チョーカー"},
                {"accmimi", "耳"},
                {"accnip",  "乳首"},
                {"accsenaka",   "背中"},
                {"accude",  "腕"},
                {"accxxx",  "前穴"},
                {"bra", "ブラジャー"},
                {"glove",   "手袋"},
                {"hairaho", "アホ毛"},
                {"hairf",   "前髪"},
                {"hairr",   "後髪"},
                {"hairs",   "横髪"},
                {"hairt",   "エクステ髪"},
                {"headset", "ヘッドドレス"},
                {"megane",  "メガネ"},
                {"mizugi",  "水着"},
                {"onepiece",    "ワンピース"},
                {"panz",    "パンツ"},
                {"shoes",   "靴"},
                {"skirt",   "ボトムス"},
                {"stkg",    "靴下"},
                {"wear",    "トップス"},
                {"Accshippo",   "しっぽ"},
            };
            private static List<MPN> prop_snapshot_excludeList = new List<MPN>()
            {
                MPN.body,             //body復元時に基本ポーズになってしまうため除外。
                MPN.haircolor,
                MPN.skin,
                MPN.acctatoo,
                MPN.hokuro,
                MPN.mayu,
                MPN.lip,
                MPN.eye,
                MPN.eye_hi,
                MPN.eye_hi_r,
                MPN.eyewhite,
                MPN.nose,
                MPN.facegloss,
            };
            //MaidPropの本元（strFileName）を記録しておく
            public void prop_snapshot()
            {
                foreach (MPN mpn in Util.getMPN_reversedList())
                {
                    if (prop_snapshot_excludeList.Contains(mpn)) continue;
                    MaidProp mp = maid.GetProp(mpn);
                    string mpnStr = Util.getMpnName(mpn);
                    prop_snapshotDict[mpnStr] = mp.strFileName;
                    Util.Debug(string.Format("{0}: {1} - {2},  temp:{3}", this.maid.status.fullNameJpStyle, mpnStr, mp.strFileName, mp.strTempFileName));
                }
            }
            //MaidPropのTemp（strTempFileName）に本元を復元して、見た目復活させる
            public void prop_restore()
            {
                foreach (KeyValuePair<string, string> kvp in prop_snapshotDict)
                {
                    change_setProp(kvp.Value, kvp.Key);
                }
            }
            private Dictionary<string, string> prop_snapshotDict = new Dictionary<string, string>();
            public void change_visible(bool visible) { maid.Visible = visible; }
            public void change_onceVoice(string voicename, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                List<string> VoiceList = new List<string>();
                VoiceList.Add(voicename);
                change_onceVoice(VoiceList, startSec: startSec, fadeinSec: fadeinSec, intervalSec: intervalSec);
            }
            public void change_onceVoice(List<string> VoiceList, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                if (isMan) { Util.Info("주인님의 음성은 변경할 수 없습니다"); return; }
                if (getPlayingVoiceState() == PlayingVoiceState.LoopVoice)
                {
                    loopVoiceBackuped = true;
                }
                _playVoice(VoiceList.ToArray(), false, startSec: startSec, fadeinSec: fadeinSec, intervalSec: intervalSec);
            }
            public void change_onceVoice(List<VoiceTable.VoiceInfo> VoiceInfoList)
            {
                if (isMan) { Util.Info("주인님의 음성은 변경할 수 없습니다"); return; }
                if (getPlayingVoiceState() == PlayingVoiceState.LoopVoice)
                {
                    loopVoiceBackuped = true;
                }
                _playVoice(VoiceInfoList, isLoop: false);
            }
            /// <summary>
            /// 繰り返し再生するボイスの変更
            /// </summary>
            /// <param name="VoiceList"></param>
            public void change_LoopVoice(List<string> VoiceList, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                if (isMan) { Util.Info("ご主人様の音声は変更できません"); return; }
                currentLoopVoice = _playVoice(VoiceList.ToArray(), isLoop: true, startSec: startSec, fadeinSec: fadeinSec, intervalSec: intervalSec);
            }
            public void change_LoopVoice(string voicename, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                List<string> list = new List<string>();
                list.Add(voicename);
                change_LoopVoice(list, startSec: startSec, fadeinSec: fadeinSec, intervalSec: intervalSec);
            }
            public void change_LoopVoice(List<VoiceTable.VoiceInfo> VoiceInfoList)
            {
                if (isMan) { Util.Info("주인님의 음성은 변경할 수 없습니다"); return; }
                currentLoopVoice = _playVoice(VoiceInfoList, isLoop: true);
            }
            public void change_stopVoice()
            {
                if (isMan) { Util.Info("ご主人様の音声は変更できません"); return; }
                maid.AudioMan.Stop();
            }
            /// <summary>
            /// ボイスリストからボイス１つを選んで再生
            /// </summary>
            /// <param name="voiceList"></param>
            /// <param name="maid">ボイスを再生するメイド</param>
            /// <param name="isLoop">ボイスをループするか</param>
            /// <param name="exclusionVoiceIndex">再生しないボイスNo.</param>
            /// <param name="forcedVoiceIndex">再生ボイスNo.を直接指定</param>
            /// <param name="startSec">音声ファイルの何秒目から再生するか</param>
            /// <param name="fadeinSec">何秒かけてフェードイン（音量小＞大）するか</param>
            /// <param name="intervalSec">再生時間</param>
            /// <returns>再生したボイスファイル名</returns>
            private string _playVoice(string playVoice, bool isLoop = true, int exclusionVoiceIndex = -1, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                playVoice = Util.forceEndsWith(playVoice, ".ogg");
                maid.AudioMan.LoadPlay(playVoice, f_fFadeTime: fadeinSec, f_bStreaming: false, f_bLoop: isLoop);
                maid.AudioMan.audiosource.time = startSec;
                this.voicePlayIntervalSec = intervalSec;
                string loopStr = isLoop ? "ループあり" : "ループなし";
                Util.Info(string.Format("음성을 재생：{0}, {1}", playVoice, loopStr));
                return playVoice;
            }
            private string _playVoice(string[] voiceList, bool isLoop = true, int exclusionVoiceIndex = -1, float startSec = 0f, float fadeinSec = 0f, float intervalSec = 0f)
            {
                if (voiceList.Length == 0)
                {
                    Util.Info("VoiceList가 비어 있습니다");
                    throw new ArgumentException("VoiceList가 비어 있습니다");
                }
                int voiceIndex;
                //特定数を除外して、再生するボイスNo.をランダムに生成
                voiceIndex = Util.randomInt(0, voiceList.Length - 1, exclusionVoiceIndex);
                voiceIndex = (int)Mathf.Clamp(voiceIndex, 0, voiceList.Length - 1);        //voiceIndexは配列の指数なので 0以上・サイズ-1以下    
                string playVoice = voiceList[voiceIndex];
                return _playVoice(playVoice, isLoop, exclusionVoiceIndex, startSec, fadeinSec, intervalSec);
            }
            private string _playVoice(List<VoiceTable.VoiceInfo> voiceInfoList, bool isLoop = true, int exclusionVoiceIndex = -1)
            {
                if (voiceInfoList.Count == 0)
                {
                    Util.Info("VoiceList가 비어 있습니다");
                    throw new ArgumentException("VoiceList가 비어 있습니다");
                }
                //特定数を除外して、再生するボイスNo.をランダムに生成
                int voiceIndex;
                voiceIndex = Util.randomInt(0, voiceInfoList.Count - 1, exclusionVoiceIndex);
                voiceIndex = (int)Mathf.Clamp(voiceIndex, 0, voiceInfoList.Count - 1);        //voiceIndexは配列の指数なので 0以上・サイズ-1以下    
                VoiceTable.VoiceInfo v = voiceInfoList[voiceIndex];
                return _playVoice(v.filename, isLoop, exclusionVoiceIndex, v.startSec, v.fadeinSec, v.intervalSec);
            }
            /// <summary>
            /// sTagに対して登録したシェイプキーアニメーション
            /// １つのsTagに対してアニメーションは１種類まで
            /// </summary>
            private Dictionary<string, ShapeKeyFader> m_shapeKeyFaderDict = new Dictionary<string, ShapeKeyFader>();
            public readonly int maidNo;
            /// <summary>
            /// Once/Loop Voice 再生時間（秒）
            /// </summary>
            private float voicePlayIntervalSec = 0f;
            private string motionNameBase = "";
            private bool propChanged = false;
            /// <summary>
            /// シェイプキー操作（VertexMorph_FromProcItem相当）
            /// 
            /// 各フレームの最後にFixBlendValuesを実行して変更を反映すること
            /// </summary>
            /// <param name="sTag"></param>
            /// <param name="morph_value"></param>
            /// <param name="fadeSec">指定値に到達するまでの時間</param>
            /// <returns>sTagの存在有無</returns>
            public bool change_shapekey(string sTag, float morph_value, float fadeSec = 0f)
            {
                if (sTag == null || sTag == "")
                    return false;
                Util.validate_morphValue(sTag, morph_value);
                Ret_Morph_Hash mh = _search_shapeKey(sTag);
                if (mh.morph == null) return false;
                TMorph morph = mh.morph;
                int hashTable_key = mh.hashKey;
                //更新リストに追加
                m_shapeKeyFaderDict[sTag] = new ShapeKeyFader(new ShapeKeyFader.Mode_Fade(morph.GetBlendValues(hashTable_key), morph_value, fadeSec), morph, sTag, hashTable_key, fadeSec);
                return true;
            }
            /// <summary>
            /// シェイプキー操作　（正弦波振動）
            /// 
            /// 各フレームの最後にFixBlendValuesを実行して変更を反映すること
            /// </summary>
            /// <param name="sTag"></param>
            /// <param name="max_morph_value"></param>
            /// <param name="min_morph_value"></param>
            /// <param name="cyclePeriodSec"></param>
            /// <param name="animationSec">アニメーション終了するまでの時間</param>
            /// <returns></returns>
            public bool change_shapekey_likeSin(string sTag, float max_morph_value, float min_morph_value, float cyclePeriodSec, float animationSec = -1f)
            {
                if (sTag == null || sTag == "")
                    return false;
                Util.validate_morphValue(sTag, max_morph_value);
                Util.validate_morphValue(sTag, min_morph_value);
                Ret_Morph_Hash mh = _search_shapeKey(sTag);
                if (mh.morph == null) return false;
                TMorph morph = mh.morph;
                int hashTable_key = mh.hashKey;
                //更新リストに追加
                m_shapeKeyFaderDict[sTag] = new ShapeKeyFader(new ShapeKeyFader.Mode_Sin(max_morph_value, min_morph_value, cyclePeriodSec), morph, sTag, hashTable_key, animationSec);
                return true;
            }
            public bool change_shapekey_likeTriangle(string sTag, float max_morph_value, float min_morph_value, float cyclePeriodSec, float animationSec = -1f)
            {
                if (sTag == null || sTag == "")
                    return false;
                Util.validate_morphValue(sTag, max_morph_value);
                Util.validate_morphValue(sTag, min_morph_value);
                Ret_Morph_Hash mh = _search_shapeKey(sTag);
                if (mh.morph == null) return false;
                TMorph morph = mh.morph;
                int hashTable_key = mh.hashKey;
                //更新リストに追加
                m_shapeKeyFaderDict[sTag] = new ShapeKeyFader(new ShapeKeyFader.Mode_Triangle(max_morph_value, min_morph_value, cyclePeriodSec), morph, sTag, hashTable_key, animationSec);
                return true;
            }
            public bool change_shapekey_likeKeiren(string sTag, float peak_morph_value, float base_morph_value, float cyclePeriodSec, float basewave_Abplitude = 0.1f, float animationSec = -1f)
            {
                if (sTag == null || sTag == "")
                    return false;
                Util.validate_morphValue(sTag, peak_morph_value);
                Util.validate_morphValue(sTag, base_morph_value);
                Ret_Morph_Hash mh = _search_shapeKey(sTag);
                if (mh.morph == null) return false;
                TMorph morph = mh.morph;
                int hashTable_key = mh.hashKey;
                //更新リストに追加
                m_shapeKeyFaderDict[sTag] = new ShapeKeyFader(new ShapeKeyFader.Mode_Keiren(peak_morph_value, base_morph_value, cyclePeriodSec, basewave_Abplitude), morph, sTag, hashTable_key, animationSec);
                return true;
            }
            //public bool change_shapekey_likePoints(string sTag, float max_morph_value, float min_morph_value, float cyclePeriodSec)
            //{
            //    m_NeedUpdateShapeKeys.Add(new ShapeKeyFader(new ShapeKeyFader.Mode_Points(pointsTable), morph, sTag, hashTable_key));
            //}
            /// <summary>
            /// このMaidのBodyの全モーフから該当のsTagがあるか探す
            /// </summary>
            /// <param name="sTag"></param>
            /// <returns></returns>
            private Ret_Morph_Hash _search_shapeKey(string sTag)
            {
                Ret_Morph_Hash ret = new Ret_Morph_Hash();
                TBody body = this.maid.body0;
                //このMaidのBodyの全モーフから該当のsTagがあるか探す
                foreach (TBodySkin skin in body.goSlot)
                {
                    TMorph morph = skin.morph;
                    if (morph == null) continue;
                    if (!morph.Contains(sTag)) continue;
                    ret.morph = morph;
                    ret.hashKey = (int)morph.hash[sTag];
                    break;
                }
                if (ret.morph == null) Util.Info(string.Format("{0}のボディにシェイプキー「{1}」が見つかりませんでした。", this.maid.name, sTag));
                return ret;
            }
            /// <summary>
            /// 複数戻り値を返すためのDTO
            /// </summary>
            class Ret_Morph_Hash
            {
                internal TMorph morph = null;
                internal int hashKey = -1;
            }
            public class ShapeKeyFader
            {
                public readonly Mode mode;
                public readonly TMorph targetMorph;
                public readonly string sTag;
                public readonly int hashTable_key;      //シェイプキー変更方法：morph.SetBlendValues(hashTable_key, morph_value);
                                                        /// <summary>
                                                        /// シェイプキーをアニメーションさせる時間
                                                        /// </summary>
                public float remainingSec = -1f;
                public ShapeKeyFader(Mode mode, TMorph targetMorph, string sTag, int hashTable_key, float remainingSec)
                {
                    this.mode = mode;
                    this.targetMorph = targetMorph;
                    this.sTag = sTag;
                    this.hashTable_key = hashTable_key;
                    this.remainingSec = remainingSec;
                }
                internal void update_morph()
                {
                    remainingSec -= Time.deltaTime;
                    targetMorph.SetBlendValues(hashTable_key, mode.calc_currentVal(remainingSec)); //TODO　update内でモーフ値反映
                }
                internal bool isFinished()
                {
                    return remainingSec <= 0f;
                }
                public abstract class Mode
                {
                    internal abstract float calc_currentVal(float currentSec);
                }
                public class Mode_Fade : Mode
                {
                    readonly float start_value;
                    readonly float target_value;
                    readonly float fadeSec;
                    public Mode_Fade(float start_value, float target_value, float fadeSec)
                    {
                        this.start_value = start_value;
                        this.target_value = target_value;
                        this.fadeSec = fadeSec;
                    }
                    internal override float calc_currentVal(float reftSec)
                    {
                        if (reftSec < 0f) return target_value;
                        return start_value + (target_value - start_value) * (1 - reftSec / fadeSec);
                    }
                }
                /// <summary>
                /// 正弦波
                /// </summary>
                public class Mode_Sin : Mode
                {
                    readonly float max_value;
                    readonly float min_value;
                    /// <summary>
                    /// 振動周期
                    /// </summary>
                    readonly float cyclePeriodSec;
                    public Mode_Sin(float max_value, float min_value, float cyclePeriodSec)
                    {
                        this.max_value = max_value;
                        this.min_value = min_value;
                        this.cyclePeriodSec = cyclePeriodSec;
                    }
                    internal override float calc_currentVal(float reftSec)
                    {
                        return min_value + (max_value - min_value) * ((float)Math.Sin(reftSec / cyclePeriodSec * 2 * Math.PI));
                    }
                }
                /// <summary>
                /// 三角波
                /// </summary>
                public class Mode_Triangle : Mode
                {
                    readonly float max_value;
                    readonly float min_value;
                    /// <summary>
                    /// 振動周期
                    /// </summary>
                    readonly float cyclePeriodSec;
                    public Mode_Triangle(float max_value, float min_value, float cyclePeriodSec)
                    {
                        this.max_value = max_value;
                        this.min_value = min_value;
                        this.cyclePeriodSec = cyclePeriodSec;
                    }
                    internal override float calc_currentVal(float reftSec)
                    {
                        return min_value + (max_value - min_value) * Math.Abs((reftSec + cyclePeriodSec / 2) % cyclePeriodSec - cyclePeriodSec / 2) / (cyclePeriodSec / 2);    //参考：https://stackoverflow.com/questions/1073606/is-there-a-one-line-function-that-generates-a-triangle-wave
                    }
                }
                /// <summary>
                /// 痙攣振動
                /// 
                /// 
                /// </summary>
                public class Mode_Keiren : Mode
                {
                    readonly float peak_value;
                    readonly float base_value;
                    readonly float basewave_Amplitude;
                    /// <summary>
                    /// 振動周期
                    /// </summary>
                    readonly float baseCyclePeriodSec;
                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="peak_value"></param>
                    /// <param name="base_value"></param>
                    /// <param name="cyclePeriodSec"></param>
                    /// <param name="basewave_Abplitude"></param>
                    public Mode_Keiren(float peak_value, float base_value, float cyclePeriodSec, float basewave_Abplitude = 0.1f)
                    {
                        this.peak_value = peak_value;
                        this.base_value = base_value;
                        this.baseCyclePeriodSec = cyclePeriodSec;
                        this.basewave_Amplitude = basewave_Abplitude;
                    }
                    float lastPhase = -1f;
                    //痙攣周期、１周期ごとにベース周期の0.5~1.5倍のランダムな値にする
                    float periodSec = 1f;
                    internal override float calc_currentVal(float reftSec)
                    {
                        float phase = reftSec % baseCyclePeriodSec;
                        if (lastPhase < phase) periodSec = (1f * Util.rand() - 0.5f + 1) * baseCyclePeriodSec;
                        lastPhase = phase;
                        float sin0p5t = (float)Math.Sin(reftSec / periodSec * 2 * Math.PI * 0.5);
                        //float sin1p3t = (float)Math.Sin(reftSec / periodSec * 2 * Math.PI * 1.3);   //うねり成分、波形のピーク形状があいまいになるので除去
                        float sin20t = (float)Math.Sin(reftSec / periodSec * 2 * Math.PI * 20);         //高周波成分
                        float background_wave = basewave_Amplitude * sin20t;
                        float pulse_wave = (float)Math.Pow(sin0p5t, 1024);      //sin波をn乗するので周期2倍になるから、周波数1/2のsin波を使う
                        return Mathf.Clamp(base_value + (peak_value - base_value) * (background_wave + pulse_wave), 0f, 1f);
                    }
                }
            }
            /// <summary>
            /// シェイプキー操作Fix
            /// 
            /// LateUpdate時に一度実行すること
            /// </summary>
            public void VertexMorph_FixBlendValues()
            {
                List<string> removeList = new List<string>();
                foreach (KeyValuePair<string, ShapeKeyFader> kv in m_shapeKeyFaderDict)
                {
                    string sTag = kv.Key;
                    ShapeKeyFader skf = kv.Value;
                    skf.update_morph();
                    if (skf.isFinished()) removeList.Add(sTag);
                    skf.targetMorph.FixBlendValues();
                }
                foreach (string s in removeList)
                {
                    m_shapeKeyFaderDict.Remove(s);
                }
            }
            public void setPersonal(string seikaku)
            {
                if (!PersonnalList.uniqueNameList.Contains(seikaku))
                {
                    Util.Info(string.Format("{0}に性格設定できませんでした。「{1}」という性格はありません。以下の候補から性格を入力してください。", this.maid.status.nickName, seikaku));
                    Util.Info(PersonnalList.uniqueNameList.ToString());
                }
                this.sPersonal = seikaku;
            }
        }
        /// <summary>
        /// 性格リスト
        /// </summary>
        public sealed class PersonnalList
        {
            public static readonly List<string> uniqueNameList = new List<string>(new string[] { "Pure", "Pride", "Cool", "Yandere", "Anesan", "Genki", "Sadist", "Muku", "Majime", "Rindere", "dummy_noSelected" });
            static readonly string[] viewName = new string[] { "純真", "ツンデレ", "クーデレ", "ヤンデレ", "姉ちゃん", "僕っ娘", "ドＳ", "無垢", "真面目", "凛デレ", "指定無" };
            public static string getUniqueName(int index)
            {
                if (index > uniqueNameList.Count() - 1) throw new ArgumentException(string.Format("性格が見つかりませんでした。　PersonalList : 指数：{0}", index));
                return uniqueNameList[index];
            }
            public static string getViewName(int index)
            {
                if (index > viewName.Count() - 1) throw new ArgumentException(string.Format("性格が見つかりませんでした。　PersonalList : 指数：{0}", index));
                return viewName[index];
            }
            public static int uniqueNameListLength()
            {
                return uniqueNameList.Count();
            }
            public static int viewNameListLength()
            {
                return viewName.Length;
            }
            internal static int uniqueNameIndexOf(string v)
            {
                return uniqueNameList.IndexOf(v);
            }
        }
        public static VoiceTable OnceVoiceTable = new VoiceTable("OnceVoice");
        public static VoiceTable LoopVoiceTable = new VoiceTable("LoopVoice");
        /// <summary>
        /// １回orループ発声するボイスのテーブル
        /// 「oncevoice_」or「loopvoice_」から始まる複数のcsvからボイス一覧を読込み、
        /// 条件に合うボイスをフィルタリングして取得
        /// </summary>
        public class VoiceTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static readonly List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Personal = new ColItem(1, "性格", "System.String");
                public static readonly ColItem Category = new ColItem(2, "カテゴリ", "System.String");
                public static readonly ColItem FileName = new ColItem(3, "ボイスファイル名", "System.String");
                public static readonly ColItem StartSec = new ColItem(4, "開始秒", "System.String");
                public static readonly ColItem IntervalSec = new ColItem(5, "継続秒", "System.String");
                public static readonly ColItem FadeinSec = new ColItem(6, "フェードイン秒", "System.String");
            }
            /// <summary>
            /// 複数のボイステーブルを保持するデータ構造（エクセルブック相当）
            /// 性格・カテゴリ名をテーブル名として、複数のテーブルを持つ
            /// </summary>
            private DataSet voiceDataSet = new DataSet();
            /// <summary>
            /// 全カテゴリ名一覧
            /// </summary>
            private HashSet<string> categorySet = new HashSet<string>();
            public readonly string voiceType;
            public VoiceTable(string voiceType)
            {
                this.voiceType = voiceType;
            }
            public void init()
            {
                voiceDataSet = new DataSet();
            }
            /// <summary>
            /// DataSetに新規シートを追加
            /// </summary>
            /// <param name="sheetName"></param>
            /// <returns></returns>
            private DataTable addNewDataTable(string sheetName)
            {
                DataTable ret = new DataTable(sheetName);
                voiceDataSet.Tables.Add(ret);
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    ret.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
                return ret;
            }
            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;
                    string category = row[ColItem.Category.colNo];
                    string sPersonal = row[ColItem.Personal.colNo];
                    string sheetName = getUniqueSheetName(sPersonal, category);
                    if (!voiceDataSet.Tables.Contains(sheetName))
                    {
                        categorySet.Add(sheetName);
                        addNewDataTable(sheetName);
                    }
                    DataRow dr = voiceDataSet.Tables[sheetName].NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    //カテゴリ名ごとにDataTableを追加
                    voiceDataSet.Tables[sheetName].Rows.Add(dr);
                }
            }
            /// <summary>
            /// 性格・カテゴリ文字列からシート名を生成
            /// （シート名の命名規則）
            /// </summary>
            /// <param name="sPersonal"></param>
            /// <param name="category"></param>
            /// <returns></returns>
            public string getUniqueSheetName(string sPersonal, string category)
            {
                return sPersonal + "_" + category;
            }
            public class VoiceInfo
            {
                public string filename;
                public float startSec;
                public float intervalSec;
                public float fadeinSec;
                public VoiceInfo(string filename, float startSec, float intervalSec, float fadeinSec)
                {
                    this.filename = filename;
                    this.startSec = startSec;
                    this.intervalSec = intervalSec;
                    this.fadeinSec = fadeinSec;
                }
            }
            private static readonly string HANNYOU_SEIKAKU = "-";
            /// <summary>
            /// 条件に一致するファイル名のリストを返す
            /// </summary>
            /// <param name="category"></param>
            /// <param name="hannyou_seikaku">性格に対応するボイスが見つからない場合、汎用性格用ボイスを探して返す</param>
            /// <returns></returns>
            public List<VoiceInfo> queryTable(string sPersonal, string category, bool hannyou_seikaku = false)
            {
                List<VoiceInfo> ret = new List<VoiceInfo>();
                string sheetName = getUniqueSheetName(sPersonal, category);
                if (!voiceDataSet.Tables.Contains(sheetName))
                {
                    Util.Info(string.Format("{0}테이블에서「{1}」라는 성격 카테고리를 찾을 수 없습니다", voiceType, sheetName));
                    if (hannyou_seikaku)
                    {
                        Util.Info(string.Format("{0}대신 범용 성격 테이블「{1}」에서 찾습니다", voiceType, getUniqueSheetName(HANNYOU_SEIKAKU, category)));
                        return queryTable(HANNYOU_SEIKAKU, category, hannyou_seikaku: false);
                    }
                    return ret;
                }
                //DataSetから指定カテゴリのテーブルを取得
                foreach (DataRow dr in voiceDataSet.Tables[sheetName].Rows)
                {
                    float startSec = 0f; float intervalSec = 0f; float fadeinSec = 0f;
                    startSec = Util.parseFloat(dr[ColItem.StartSec.ordinal].ToString());
                    intervalSec = Util.parseFloat(dr[ColItem.IntervalSec.ordinal].ToString());
                    fadeinSec = Util.parseFloat(dr[ColItem.FadeinSec.ordinal].ToString());
                    ret.Add(new VoiceInfo(dr[ColItem.FileName.ordinal].ToString(), startSec, intervalSec, fadeinSec));
                }
                if (ret.Count == 0)
                {
                    Util.Info(string.Format("{0}テーブルから「{1}」という名前の性格・カテゴリは見つかりませんでした", voiceType, sheetName));
                    if (hannyou_seikaku)
                    {
                        Util.Info(string.Format("{0}のかわりに汎用性格テーブル「{1}」から探します", voiceType, getUniqueSheetName(HANNYOU_SEIKAKU, category)));
                        return queryTable(HANNYOU_SEIKAKU, category, hannyou_seikaku: false);
                    }
                }
                return ret;
            }
        }
        public class MotionInfo
        {
            public string category = "";
            public bool FrontReverse;   //전후 반전하거나？  0:false
            public float AzimuthAngle;     //X 방향 회전 각도
            public float DeltaY;           //Y 방향 오프셋
            public string MaidState;
            public bool EnMotionChange;
            public string motionName = "";
        }
        /// <summary>
        /// モーションのテーブル
        /// csvからモーション一覧を読込み、
        /// 条件に合うモーションをフィルタリングして取得
        /// </summary>
        public class MotionTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Category = new ColItem(1, "カテゴリ", "System.String");
                public static readonly ColItem FrontReverse = new ColItem(2, "正面反転？", "System.Boolean");
                public static readonly ColItem AzimuthAngle = new ColItem(3, "横回転角度", "System.Double");
                public static readonly ColItem DeltaY = new ColItem(4, "Y軸位置", "System.Double");
                public static readonly ColItem MaidState = new ColItem(5, "メイド状態", "System.String");
                public static readonly ColItem EnMotionChange = new ColItem(6, "モーション変更許可", "System.Boolean");
                public static readonly ColItem MotionName = new ColItem(7, "モーションファイル名", "System.String");
            }
            private static DataTable motionTable = new DataTable("Motion");
            public static void init()
            {
                motionTable = new DataTable("Motion");
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    motionTable.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
            }
            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public static void parse(string[][] csvContent, string filename = "")
            {
                //string categoryname = filename.Replace(cfg.loopVoicePrefix, "").Replace(".csv", "").Replace(".CSV", "");
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;
                    DataRow dr = motionTable.NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    motionTable.Rows.Add(dr);
                }
            }
            /// <summary>
            /// 조건에 일치하는 모션 정보의 목록을 반환
            /// 모션 이름은베이스 부분 만 항목도있다。
            /// </summary>
            /// <param name="personal"></param>
            /// <param name="category"></param>
            /// <param name="maidState"></param>
            /// <param name="feelMin"></param>
            /// <param name="feelMax"></param>
            /// <returns></returns>
            public static List<MotionInfo> queryTable_motionNameBase(string category, string maidState = "-") // int feelMin = 0, int feelMax = 3)
            {
                // Select 메서드를 사용하여 데이터를 추출
                List<MotionInfo> ret = query(category, maidState);
                if (ret.Count == 0)
                {
                    //Util.info(string.Format("MaidState「{0}」의 Motion을 찾을 수 없었기 때문에 기본 MaidState의 모션을 찾습니다", maidState));
                    ret = query(category, "-", "MotionTable　다시 검색　MaidState「기본」");
                }
                //Util.debug(string.Format("Motionクエリ結果\r\n カテゴリ:{0},MaidState:{1}\r\n{2}",
                //    category, maidState, Util.list2Str(ret)));
                return ret;
            }
            private static List<MotionInfo> query(string category, string maidState = "-", string comment = "MotionTable 検索")
            {
                string query = createCondition(category, maidState);
                DataRow[] dRows = motionTable.Select(query);
                List<MotionInfo> ret = new List<MotionInfo>();
                foreach (DataRow dr in dRows)
                {
                    try
                    {
                        MotionInfo mi = new MotionInfo();
                        mi.category = dr[ColItem.Category.ordinal].ToString();
                        mi.motionName = dr[ColItem.MotionName.ordinal].ToString();
                        mi.DeltaY = float.Parse(dr[ColItem.DeltaY.ordinal].ToString());
                        mi.AzimuthAngle = float.Parse(dr[ColItem.AzimuthAngle.ordinal].ToString());
                        mi.EnMotionChange = bool.Parse(dr[ColItem.EnMotionChange.ordinal].ToString());
                        mi.FrontReverse = bool.Parse(dr[ColItem.FrontReverse.ordinal].ToString());
                        ret.Add(mi);
                    }
                    catch (Exception e)
                    {
                        Util.Debug(string.Format("MotionTable에서 읽기 실패:{0} \r\n 오류 내용 : {1}", dr.ToString(), e.StackTrace));
                    }
                }
                Util.Debug(string.Format("{0}\r\n  {1}\r\n  검색 결과\r\n  {2}", comment, query, Util.list2Str(ret)));
                return ret;
            }
            private static string createCondition(string category, string maidState = "-") // int feelMin = 0, int feelMax = 3)
            {
                StringBuilder condition = new StringBuilder();
                condition.Append(string.Format(" {0} = '{1}'", ColItem.Category.colName, category));
                if (maidState != "-")
                {
                    if (condition.Length != 0) condition.Append(" AND ");
                    condition.Append(string.Format(" {0} = '{1}'", ColItem.MaidState.colName, maidState));
                }
                return condition.ToString();
            }
            public static DataTable getTable()
            {
                return motionTable;
            }
            public static IEnumerable<string> getCategoryList()
            {
                HashSet<string> ret = new HashSet<string>();
                foreach (DataRow dr in motionTable.Rows)
                {
                    ret.Add((string)dr[ColItem.Category.colName]);
                }
                return ret;
            }
        }
        /// <summary>
        /// 表情のテーブル
        /// 「face_」から始まる複数のcsvから表情一覧を読込み。
        /// 条件に合う表情をフィルタリングして取得
        /// </summary>
        public class FaceTable
        {
            public class ColItem
            {
                private static int nextOrdinal = 0;
                public static List<ColItem> items = new List<ColItem>();
                //フィールド一覧
                public readonly string colName;
                public readonly string typeStr;
                public readonly int ordinal;            //このEnumのインスタンス順序
                public readonly int colNo;              //csvファイルの何列目に相当するか 0：２列目（1列目はCSV読み込み時に読み飛ばすため）
                public static int maxColNo = 0;
                //コンストラクタ
                private ColItem(int colNo, string colName, string typeStr)
                {
                    this.colName = colName;
                    this.typeStr = typeStr;
                    this.ordinal = nextOrdinal;
                    nextOrdinal++;
                    this.colNo = colNo;
                    items.Add(this);
                    if (maxColNo < colNo) maxColNo = colNo;
                }
                // 参照用インスタンス
                //public static readonly ColItem RecordName = new ColItem(0, "レコード名", "System.String");       //1列目はレコード名、読み込まない
                public static readonly ColItem Category = new ColItem(1, "カテゴリ", "System.String");
                public static readonly ColItem FaceName = new ColItem(2, "表情ファイル名", "System.String");
                public static readonly ColItem Hoho = new ColItem(3, "頬", "System.Int32");
                public static readonly ColItem Namida = new ColItem(4, "涙", "System.Int32");
                public static readonly ColItem Yodare = new ColItem(5, "よだれ", "System.Int32");
            }
            /// <summary>
            /// 複数の表情テーブルを保持するデータ構造（エクセルブック相当）
            /// カテゴリ名をテーブル名として、複数のテーブルを持つ
            /// </summary>
            private static DataSet faceDataSet = new DataSet();
            /// <summary>
            /// 全カテゴリ名一覧
            /// </summary>
            private static HashSet<string> categorySet = new HashSet<string>();
            public static void init()
            {
                faceDataSet = new DataSet();
            }
            /// <summary>
            /// faceDataSetに新規シートを追加
            /// </summary>
            /// <param name="sheetName"></param>
            /// <returns></returns>
            private static DataTable addNewDataTable(string sheetName)
            {
                DataTable ret = new DataTable(sheetName);
                faceDataSet.Tables.Add(ret);
                // カラム名の追加
                foreach (ColItem c in ColItem.items)
                {
                    ret.Columns.Add(c.colName, Type.GetType(c.typeStr));
                }
                return ret;
            }
            /// <summary>
            /// CSVから読み込んでテーブルへ追加
            /// </summary>
            /// <param name="csvContent"></param>
            /// <param name="filename"></param>
            public static void parse(string[][] csvContent, string filename = "")
            {
                foreach (string[] row in csvContent)
                {
                    if (row.Length - 1 < ColItem.maxColNo) continue;
                    string category = row[ColItem.Category.colNo];  //dr[ColItem.Category.colName].ToString();
                    if (!faceDataSet.Tables.Contains(category))
                    {
                        categorySet.Add(category);
                        addNewDataTable(category);
                    }
                    DataRow dr = faceDataSet.Tables[category].NewRow();
                    foreach (ColItem ovc in ColItem.items)
                    {
                        if (ovc.typeStr == "System.Boolean")
                        {
                            dr[ovc.colName] = (int.Parse(row[ovc.colNo]) != 0); //0ならfalseとして解釈
                        }
                        else
                        {
                            dr[ovc.colName] = row[ovc.colNo];
                        }
                    }
                    //カテゴリ名ごとにDataTableを追加
                    faceDataSet.Tables[category].Rows.Add(dr);
                }
            }
            /// <summary>
            /// 条件に一致するファイル名のリストを返す
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public static List<string> queryTable(string category)
            {
                List<string> ret = new List<string>();
                if (!faceDataSet.Tables.Contains(category))
                {
                    Util.Info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                    return ret;
                }
                //DataSetから指定カテゴリのテーブルを取得
                foreach (DataRow dr in faceDataSet.Tables[category].Rows)
                {
                    ret.Add(dr[ColItem.FaceName.ordinal].ToString());
                }
                if (ret.Count == 0)
                {
                    Util.Info(string.Format("表情テーブルから「{0}」という名前のカテゴリは見つかりませんでした", category));
                }
                return ret;
            }
        }
        /// <summary>
        /// Androidのトースト風メッセージ
        /// </summary>
        /// <param name="message"></param>
        public static void toast(string message, float waitSec = -1)
        {
            if (waitSec == -1) waitSec = ((float)ToastUtil.waitFrame) / 60;
            ToastUtil.Toast(ScriplayPlugin.instance, message, waitSec);
        }
        /// <summary>
        /// トースト生成用クラス
        /// 参考：https://qiita.com/maebaru/items/23e85a8f2f1ce69482b7
        /// </summary>
        public class ToastUtil : MonoBehaviour
        {
            public static Color imgColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            public static Color textColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            public static Vector2 startPos = new Vector2(0, -500); // 開始場所
            public static Vector2 endPos = new Vector2(0, -300); // 終了場所
            public static int fontSize = 20;
            public static int moveFrame = 10; // 浮き上がりの時間(フレーム)
            public static int waitFrame = (int)3 * 60; // 浮き上がり後の時間(フレーム)
            public static int pad = 100; // padding
            public static Sprite imgSprite;
            public static Font textFont;
            public static void Toast<T>(MonoBehaviour mb, T m, float waitSec = -1)
            {
                if (waitSec != -1) waitFrame = (int)(waitSec * 60);
                string msg = m.ToString();
                GameObject g = new GameObject("ToastCanbas");
                Canvas canvas = g.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; //最前
                g.AddComponent<CanvasScaler>();
                g.AddComponent<GraphicRaycaster>();
                GameObject g2 = new GameObject("Image");
                g2.transform.parent = g.transform;
                Image im = g2.AddComponent<Image>();
                if (imgSprite) im.sprite = imgSprite;
                im.color = imgColor;
                g2.GetComponent<RectTransform>().anchoredPosition = startPos;
                GameObject g3 = new GameObject("Text");
                g3.transform.parent = g2.transform;
                Text t = g3.AddComponent<Text>();
                g3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                t.alignment = TextAnchor.MiddleCenter;
                if (textFont)
                    t.font = textFont;
                else
                    t.font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                t.fontSize = fontSize;
                t.text = msg;
                t.enabled = true;
                t.color = textColor;
                g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight);
                g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight);//2回必要
                g2.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth + pad, t.preferredHeight + pad);
                mb.StartCoroutine(
                  DoToast(
                    g2.GetComponent<RectTransform>(), (endPos - startPos) * (1f / moveFrame), g
                  )
                );
            }
            static IEnumerator DoToast(RectTransform rec, Vector2 dif, GameObject g)
            {
                for (var i = 1; i <= moveFrame; i++) { rec.anchoredPosition += dif; yield return null; }
                for (var i = 1; i <= waitFrame; i++) yield return null;
                Destroy(g);
            }
        }
        public static GameObject createGO(string name)
        {
            return new GameObject(name);
        }
    }
    /// <summary>
    /// VR空間にUIを表示するクラス
    /// 文字の表示エリアと選択肢の表示エリアがあり、VRコントローラから選択肢を選ぶことができる
    /// Usage:
    /// VRUI.addSelection("選択肢１",（実行内容）);
    /// VRUI.clearSelection();
    /// 
    /// VRUI.setText("表示文字");
    /// VRUI.clearText();
    /// VRUI.addText("追加文字");
    /// 
    /// 
    /// Update(){
    ///     VRUI.Update();
    /// }
    /// 
    /// </summary>
    public static class VRUI
    {
        private static TextMesh showText = null;
        private static string _titleStr = "カメラモードで操作 ↑↓:カーソル移動　→:確定　←長押し:停止・再読込";
        private static string titleStr
        {
            get { return _titleStr; }
            set
            {
                selectionText_needUpdate = true;
                _titleStr = value;
            }
        }
        private static string _showAreaStr = "";
        private static string showAreaStr
        {
            get { return _showAreaStr; }
            set
            {
                selectionText_needUpdate = true;
                _showAreaStr = value;
            }
        }
        private static bool selectionText_needUpdate = true;
        public static List<bool> selectionList = new List<bool>();
        private static List<string> selectionTextList = new MyList<string>();
        class MyList<T> : List<T>
        {
            public new void Add(T item)
            {
                selectionText_needUpdate = true;
                base.Add(item);
            }
            public new void Clear()
            {
                selectionText_needUpdate = true;
                base.Clear();
            }
        };
        private static int _selectedItemIndex = 0;
        private static int selectedItemIndex
        {
            get { return _selectedItemIndex; }
            set
            {
                _selectedItemIndex = value;
                selectionText_needUpdate = true;
            }
        }
        //public static Dictionary<string, bool> selection = new Dictionary<string, bool>();
        //public static OvrMgr.OvrObject.Controller leftController = GameMain.Instance.OvrMgr.ovr_obj.left_controller;
        //public static OvrMgr.OvrObject.Controller rightController = GameMain.Instance.OvrMgr.ovr_obj.right_controller;
        public static VRVirtualController left;
        public static VRVirtualController right;
        static int tmp = 0;
        public static VRVirtualController.PadDirection getPressedDirection(VRVirtualController.ControllerType controllerType = VRVirtualController.ControllerType.NONE)
        {
            VRVirtualController.PadDirection l = left.getPressedDirection();
            VRVirtualController.PadDirection r = right.getPressedDirection();
            switch (controllerType)
            {
                case VRVirtualController.ControllerType.LEFT:
                    return l;
                case VRVirtualController.ControllerType.RIGHT:
                    return r;
                default:
                    if (l != VRVirtualController.PadDirection.NONE)
                    {
                        return l;
                    }
                    else
                    {
                        return r;
                    }
            }
        }
        private static Dictionary<VRVirtualController.PadDirection, float> pressedSecDict = new Dictionary<VRVirtualController.PadDirection, float>();
        public static float getPressedSec(VRVirtualController.PadDirection dir)
        {
            if (!pressedSecDict.ContainsKey(dir))
            {
                Util.Info(string.Format("get pressedSec : キー{0} がありません", dir.ToString()));
                return 0f;
            }
            return pressedSecDict[dir];
        }
        public static void Update()
        {
            if (!_isVREnabled) return;
            //tmp++;
            //if (tmp > 90)
            //{
            //    tmp = 0;
            //    //    //OvrMgr.OvrObject.Controller  leftController = GameMain.Instance.OvrMgr.ovr_obj.left_controller;
            //    //    //Util.debug(string.Format("VRUI Update, isVREnabled:{0}, leftController:{1}, handoyotogimode:{2},grip:{3},menue:{4},stickpad:{5},trigger:{6},l_click:{7},handcameramode:{8}",
            //    //    //    isVREnabled.ToString(), leftController.ToString(), leftController.controller.HandYotogiMode.ToString(), leftController.controller_buttons.GetPress(AVRControllerButtons.BTN.GRIP).ToString(),
            //    //    //    leftController.controller_buttons.GetPress(AVRControllerButtons.BTN.MENU).ToString(), leftController.controller_buttons.GetPress(AVRControllerButtons.BTN.STICK_PAD).ToString(),
            //    //    //    leftController.controller_buttons.GetPress(AVRControllerButtons.BTN.TRIGGER).ToString(), leftController.controller_buttons.GetPress(AVRControllerButtons.BTN.VIRTUAL_L_CLICK).ToString(),
            //    //    //    leftController.controller.HandCameraMode.ToString()));
            //    //    Util.debug(string.Format("l pressed : {0}, r pressed : {1}, left time:{2}", left.getPressedDirection().ToString(), right.getPressedDirection().ToString(), pressedSecDict[VRVirtualController.PadDirection.LEFT].ToString()));
            //    string show = "";
            //    foreach (bool b in selectionList)
            //    {
            //        show += b.ToString() + ", ";
            //    }
            //    Util.debug(string.Format(show));
            //}
            //VRコントローラのキー取得、カーソル移動・選択
            VRVirtualController.PadDirection lDir = VRVirtualController.PadDirection.NONE;
            VRVirtualController.PadDirection rDir = VRVirtualController.PadDirection.NONE;
            if (left.isCameraMode()) lDir = left.getPresseDownedDirection(); //getPressedDirection();
            if (right.isCameraMode()) rDir = right.getPresseDownedDirection(); //getPressedDirection();
            if (lDir == VRVirtualController.PadDirection.UP || rDir == VRVirtualController.PadDirection.UP)
            {
                selectedItemIndex--;
                if (selectedItemIndex < 0) selectedItemIndex = selectionTextList.Count - 1;
                selectText(selectedItemIndex);
            }
            else if (lDir == VRVirtualController.PadDirection.DOWN || rDir == VRVirtualController.PadDirection.DOWN)
            {
                selectedItemIndex++;
                if (selectedItemIndex > selectionTextList.Count - 1) selectedItemIndex = 0;
                selectText(selectedItemIndex);
            }
            else if (lDir == VRVirtualController.PadDirection.RIGHT || rDir == VRVirtualController.PadDirection.RIGHT)
            {
                enterSelection(selectedItemIndex);
                Util.Info(string.Format("VRUI Selection {0}番目が選択されました", selectedItemIndex.ToString()));
            }
            //VRコントローラ　押し続け時間更新
            if (left.getPressedDirection() == VRVirtualController.PadDirection.UP || right.getPressedDirection() == VRVirtualController.PadDirection.UP)
            {
                pressedSecDict[VRVirtualController.PadDirection.UP] += Time.deltaTime;
            }
            else
            {
                pressedSecDict[VRVirtualController.PadDirection.UP] = 0;
            }
            if (left.getPressedDirection() == VRVirtualController.PadDirection.DOWN || right.getPressedDirection() == VRVirtualController.PadDirection.DOWN)
            {
                pressedSecDict[VRVirtualController.PadDirection.DOWN] += Time.deltaTime;
            }
            else
            {
                pressedSecDict[VRVirtualController.PadDirection.DOWN] = 0;
            }
            if (left.getPressedDirection() == VRVirtualController.PadDirection.RIGHT || right.getPressedDirection() == VRVirtualController.PadDirection.RIGHT)
            {
                pressedSecDict[VRVirtualController.PadDirection.RIGHT] += Time.deltaTime;
            }
            else
            {
                pressedSecDict[VRVirtualController.PadDirection.RIGHT] = 0;
            }
            if (left.getPressedDirection() == VRVirtualController.PadDirection.LEFT || right.getPressedDirection() == VRVirtualController.PadDirection.LEFT)
            {
                pressedSecDict[VRVirtualController.PadDirection.LEFT] += Time.deltaTime;
            }
            else
            {
                pressedSecDict[VRVirtualController.PadDirection.LEFT] = 0;
            }
            //表示テキスト 更新
            if (selectionText_needUpdate)
            {
                selectionText_needUpdate = false;
                //タイトル
                showText.text = titleStr + "\r\n";
                //表示領域
                showText.text += showAreaStr + "\r\n";
                //選択肢
                int index = 0;
                foreach (string s in selectionTextList)
                {
                    if (index == selectedItemIndex)
                    {
                        showText.text += "＞ " + s + "\r\n";
                    }
                    else
                    {
                        showText.text += "　　" + s + "\r\n";
                    }
                    index++;
                }
            }
            //すべての文字をカメラの方を向かせる
            if (showText != null) showText.transform.LookAt(Camera.main.transform);
        }
        public static void setTitleText(string text)
        {
            if (!_isVREnabled) return;
            titleStr = text;
        }
        public static void setShowText(string text)
        {
            if (!_isVREnabled) return;
            showAreaStr = text;
        }
        public static void clearShowText()
        {
            if (!_isVREnabled) return;
            showAreaStr = "";
        }
        public static void addShowText(string text)
        {
            if (!_isVREnabled) return;
            showAreaStr += text + "\r\n";
        }
        //public static void setSelection(List<ScriplayContext.Selection> list)
        //{
        //    if (!isVREnabled) return;
        //    selectionTextList.Clear();
        //    selectionList.Clear();
        //    foreach (ScriplayContext.Selection s in list)
        //    {
        //        selectionTextList.Add(s.viewStr);
        //        selectionList.Add(false);
        //    }
        //    selectText(0);
        //}
        /// <summary>
        /// 選択肢リストの指定番に項目追加
        /// </summary>
        /// <param name="index"></param>
        /// <param name="s"></param>
        internal static void setSelection(int index, string selectionStr)
        {
            if (!_isVREnabled) return;
            //要素をセットできるようにlistのサイズを大きくする
            if (index == selectionList.Count)
            {
                selectionTextList.Add("");
                selectionList.Add(false);
            }
            else if (index > selectionList.Count)
            {
                for (int i = selectionList.Count - 1; i <= index; i++)
                {
                    selectionTextList.Add("");
                    selectionList.Add(false);
                }
            }
            selectionText_needUpdate = true;
            selectionTextList[index] = selectionStr;
        }
        //public static void addSelection(ScriplayContext.Selection selection)
        //{
        //    if (!isVREnabled) return;
        //    selectionTextList.Add(selection.viewStr);
        //    selectionList.Add(false);
        //    selectText(0);
        //}
        public static void enterSelection(int selectedIndex)
        {
            selectionList[selectedIndex] = true;
        }
        private static void selectText(int selectedIndex)
        {
            if (!_isVREnabled) return;
            if (selectedIndex < 0) return;
            if (selectedIndex > selectionTextList.Count - 1) return;
            selectedItemIndex = selectedIndex;
        }
        public static void clearSelection()
        {
            if (!_isVREnabled) return;
            selectionTextList.Clear();
            selectionList.Clear();
            selectionText_needUpdate = true;
        }
        private static int textMeshGO_counter = 0;
        private static TextMesh createTextMesh_overTablet(string showText, Color textColor, int fontsize = 15, float localX = 0f, float localY = 0f, float localZ = 0f, float scale = 0.01f)
        {
            if (!_isVREnabled)
            {
                throw new Exception("createTextMesh_overTablet　：　VR環境ではありません。");
            }
            //キャンバス生成＆設定
            GameObject textmesh_GO = new GameObject("FaceCanvas" + textMeshGO_counter.ToString());
            textMeshGO_counter++;
            TextMesh textmeshComponent = textmesh_GO.AddComponent<TextMesh>();
            textmesh_GO.AddComponent<CanvasRenderer>();
            textmeshComponent.text = showText;
            textmeshComponent.color = textColor;
            textmeshComponent.fontSize = fontsize;
            //座標をタブレットに紐づけ
            textmesh_GO.transform.parent = tablet.transform;
            Vector3 localPos = new Vector3(localX, localY, localZ);
            textmeshComponent.transform.localPosition = localPos;
            //文字を左から右へ読めるようにする
            textmeshComponent.transform.localScale = new Vector3(-scale, scale, scale);
            return textmeshComponent;
        }
        public static void init_onLevelWasLoaded()
        {
            //変数初期化
            pressedSecDict[VRVirtualController.PadDirection.UP] = 0;
            pressedSecDict[VRVirtualController.PadDirection.DOWN] = 0;
            pressedSecDict[VRVirtualController.PadDirection.LEFT] = 0;
            pressedSecDict[VRVirtualController.PadDirection.RIGHT] = 0;
            if (left == null) left = new VRVirtualController(VRVirtualController.ControllerType.LEFT);
            if (right == null) right = new VRVirtualController(VRVirtualController.ControllerType.RIGHT);
            left.init_controller();
            right.init_controller();
            // VR判別
            string gameDataPath = UnityEngine.Application.dataPath;
            _isVREnabled = gameDataPath.Contains("COM3D2OHVRx64") || gameDataPath.Contains("COM3D2VRx64") || Environment.CommandLine.ToLower().Contains("/vr");
            if (!_isVREnabled)
            {
                Util.Info("VR環境ではありません");
                return;
            }
            //タブレット取得
            if (tablet == null)
            {
                OvrTablet[] tabletList = (OvrTablet[])UnityEngine.Object.FindObjectsOfType(typeof(OvrTablet));
                if (tabletList.Length == 0)
                {
                    Util.Info("タブレットが存在しません");
                    return;
                }
                else if (tabletList.Length > 1)
                {
                    Util.Info("タブレットが複数存在します");
                    return;
                }
                else
                {
                    Util.Info("タブレットが見つかりました");
                    tablet = tabletList[0];
                }
            }
            //TextMesh生成
            if (showText == null) showText = createTextMesh_overTablet("", Color.black, localZ: -0.3f);
        }
        public static void OnLevelWasLoaded(int level)
        {
            Util.Debug("VRUI 初期化 OnLevelWasLoaded");
            init_onLevelWasLoaded();
        }
        internal static bool isSelected(int selectedIndex)
        {
            if (!_isVREnabled) return false;
            if (selectionList.Count - 1 < selectedIndex) return false;
            return selectionList[selectedIndex];
        }
        private static bool _isVREnabled;
        public static bool isVREnabled() { return _isVREnabled; }
        public static OvrTablet tablet;
        public class VRVirtualController
        {
            public OvrMgr.OvrObject.Controller controller;
            public enum PadDirection
            {
                LEFT, RIGHT, UP, DOWN, NONE
            }
            public enum ControllerType
            {
                LEFT, RIGHT, NONE
            }
            public readonly ControllerType controllerType;
            public VRVirtualController(ControllerType type)
            {
                controllerType = type;
                init_controller();
            }
            public void init_controller()
            {
                Util.Debug(string.Format("init_controller : VR enabled{0} ", _isVREnabled.ToString()));
                if (!_isVREnabled) return;
                switch (controllerType)
                {
                    case ControllerType.LEFT:
                        controller = GameMain.Instance.OvrMgr.ovr_obj.left_controller;
                        break;
                    case ControllerType.RIGHT:
                        controller = GameMain.Instance.OvrMgr.ovr_obj.right_controller;
                        break;
                }
                if (controller == null)
                {
                    Util.Info(string.Format("VRコントローラ（{0}）が見つかりませんでした", controllerType.ToString()));
                }
                else
                {
                    Util.Info(string.Format("VRコントローラ（{0}）が見つかりました", controllerType.ToString()));
                }
            }
            /// <summary>
            /// スティックパッドが押し込まれたフレームのみNone以外を返す
            /// </summary>
            /// <returns></returns>
            public PadDirection getPresseDownedDirection()
            {
                bool padPresseDowned = controller.controller_buttons.GetPressDown(AVRControllerButtons.BTN.STICK_PAD);
                if (!padPresseDowned) return PadDirection.NONE;
                var xy = getPressedXY();
                if (xy.Count == 0) return PadDirection.NONE;
                float x = xy["x"];
                float y = xy["y"];
                if (y > 0.7f)
                {
                    return PadDirection.UP;
                }
                else if (y < -0.7f)
                {
                    return PadDirection.DOWN;
                }
                else if (x < -0.7f)
                {
                    return PadDirection.LEFT;
                }
                else if (x > 0.7f)
                {
                    return PadDirection.RIGHT;
                }
                return PadDirection.NONE;
            }
            public PadDirection getPressedDirection()
            {
                var xy = getPressedXY();
                if (xy.Count == 0) return PadDirection.NONE;
                float x = xy["x"];
                float y = xy["y"];
                if (y > 0.7f)
                {
                    return PadDirection.UP;
                }
                else if (y < -0.7f)
                {
                    return PadDirection.DOWN;
                }
                else if (x < -0.7f)
                {
                    return PadDirection.LEFT;
                }
                else if (x > 0.7f)
                {
                    return PadDirection.RIGHT;
                }
                return PadDirection.NONE;
            }
            /// <summary>
            /// メソッド実行時のフレームでVRコントローラのパッドが押されていたら、パッドに触れている座標を取得してDictで返す
            /// </summary>
            /// <returns></returns>
            public Dictionary<string, float> getPressedXY()
            {
                Dictionary<string, float> ret = new Dictionary<string, float>();
                //Util.debug(string.Format("VRVirtural Controller {0}, getPressedXY() : exists controller - {1}",controllerType.ToString(), (controller!=null).ToString()));
                if (controller == null) return ret;
                bool padPressed = controller.controller_buttons.GetPress(AVRControllerButtons.BTN.STICK_PAD);
                //Util.debug(string.Format("is pressed : {0}", padPressed.ToString()));
                if (!padPressed) return ret;
                float x = controller.controller_buttons.GetAxis().x;
                float y = controller.controller_buttons.GetAxis().y;
                ret["x"] = x;
                ret["y"] = y;
                //Util.debug(string.Format("pressed xy : x:{0},y{1}", x.ToString(), y.ToString()));
                return ret;
            }
            internal bool isCameraMode()
            {
                return controller.controller.HandCameraMode;
            }
        }
    }
    public static class Util
    {
        static Util()
        {
            SortedDictionary<string, MPN> sd = new SortedDictionary<string, MPN>();
            Array a = Enum.GetValues(typeof(MPN));
            foreach (MPN m in a)
            {
                string key = string.Format("{0:D3}{1}", getMpnName(m).Length, getMpnName(m)); //008null_mpn mpn名はkeyの重複防止のため
                sd[key] = m;
            }
            MPN_ReversedList = sd.Values.ToList();
            MPN_ReversedList.Reverse();
        }
        /// <summary>
        /// 末尾から空白文字を除去して、postfixで終わっていなかったらpostfixを追加する
        /// ただし、空白文字の場合は何もしない
        /// postfixに空白文字を含む場合はうまく動作しない（メソッドの想定範囲外）
        /// </summary>
        /// <param name="target"></param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        public static string forceEndsWith(string target, string postfix)
        {
            if (target == "") return target;
            target = reg_spaceLikeStr.Replace(target, "");
            if (!target.EndsWith(postfix)) target += postfix;
            return target;
        }
        private static Regex reg_spaceLikeStr = new Regex(@"\s+$", RegexOptions.IgnoreCase);
        public static string getMpnName(MPN mpn) { return Enum.GetName(typeof(MPN), mpn); }
        public static string getSlotName(TBody.SlotID id) { return Enum.GetName(typeof(TBody.SlotID), id); }
        /// <summary>
        /// 파일 이름의 나열 기본값은 CSV 파일 만
        /// </summary>
        /// <param name="searchPath"></param>
        /// <param name="suffix">例　"csv"</param>
        /// <returns></returns>
        public static List<string> getFileFullpathList(string searchPath, string suffix, string prefix = "")
        {
            suffix = "*." + suffix;
            //폴더 확인
            if (!Directory.Exists(searchPath))
            {
                //없으면 폴더 만들기
                DirectoryInfo di = Directory.CreateDirectory(searchPath);
            }
            return new List<string>(Directory.GetFiles(searchPath, prefix + "*" + suffix));
        }
        /// <summary>
        /// コンソールにテキスト出力
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            //Console.WriteLine("I " + PluginMessage(message));
            UnityEngine.Debug.Log( PluginMessage("I", message));
        }
        /// <summary>
        /// コンソールにテキスト出力
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            //Console.WriteLine("<color=" + cfg.debugPrintColor + ">" + PluginMessage(message) + "</color>");
            //UnityEngine.Debug.Log("<color=" + ScriplayPlugin.cfg.debugPrintColor + ">" + PluginMessage(message) + "</color>");
            if (ScriplayPlugin.cfg.enable_debugLog_onConsole)
            {
                UnityEngine.Debug.Log(PluginMessage("D" , message));
                //Console.WriteLine("D " + Util.PluginMessage(message));
            }

        }
        private static string PluginMessage(string level,string originalMessage)
        {
            return string.Format("{0}.{1}:{2}", ScriplayPlugin.cfg.PluginName, level, originalMessage);
        }
        /// <summary>
        /// 指定範囲内から特定数を除外してランダムな整数を返す
        /// Usage:
        ///  Util.randomInt(0,list.Length-1,currentIndex);
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="excludeList"></param>
        /// <returns></returns>
        public static int randomInt(int min, int max, List<int> excludeList = null)
        {
            if (min >= max) return min;
            if (excludeList == null) excludeList = new List<int>();
            List<int> indexList = Enumerable.Range(min, max + 1).ToList(); //Enumerable.Range(min, max) min以上max「未満」の連番でリスト作成　.Where(item => item != exclusionVoiceIndex).ToArray(); //ラムダ式は使えないぽい
                                                                           //Util.debug("indexList:" + Util.list2Str(indexList) + ", excludeList:" + Util.list2Str(excludeList));
            foreach (int i in excludeList)
            {
                if (indexList.Count == 1) break;
                indexList.Remove(i);
            }
            int ret = indexList[random.Next(indexList.ToArray().Length)];   //random.Nextは指定値「未満」の、0以上の整数を返す
            Util.Debug(string.Format("randomInt min:{0}, max:{1}, selected:{2}, exclude:{3}, indexList:{4}", min, max, ret, Util.list2Str(excludeList), Util.list2Str(indexList)));
            return ret;
        }
        public static readonly System.Random random = new System.Random();
        /// <summary>
        /// 0~1のランダムな値
        /// </summary>
        /// <returns></returns>
        public static float rand() { return (float)random.NextDouble(); }
        /// <summary>
        /// Linq使えないため？、joinでエラー出る。代わりにコレクションを文字列化するメソッド
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string list2Str<T>(IEnumerable<T> collection, string separator = ",")   // where T: object
        {
            if (collection == null || collection.Count() == 0) return "(要素数　０）";
            StringBuilder str = new StringBuilder();
            foreach (T t in collection)
            {
                str.Append(t.ToString() + separator);
            }
            return str.ToString();
        }
        public static int randomInt(int min, int max, int excludeValue = -1)
        {
            var excludeList = new List<int>();
            if (0 <= excludeValue)
            {
                excludeList.Add(excludeValue);
            }
            return randomInt(min, max, excludeList);
        }
        /// <summary>
        /// ステータス表示用テキストを返す
        /// </summary>
        private static string[] SucoreText = new string[] { "☆ ☆ ☆", "★ ☆ ☆", "★ ★ ☆", "★ ★ ★" };
        //private static string[] SucoreText = new string[] { "_", "Ⅰ", "Ⅱ", "Ⅲ" };
        public static string getScoreText(int level)
        {
            level = (int)Mathf.Clamp(level, 0, 3);
            return SucoreText[level];
        }
        private static bool enSW = false;
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private static readonly int stopwatch_executeFrames = 60;
        private static int sw_frames = 0;
        public static void sw_start(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames++ > stopwatch_executeFrames)) return;
            sw.Start();
        }
        public static void sw_stop(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames > stopwatch_executeFrames)) return;
            sw_frames = 0;
            sw.Stop();
            sw.Reset();
        }
        public static void sw_showTime(string s = "")
        {
            if (!enSW) return;
            if (!(sw_frames > stopwatch_executeFrames)) return;
            sw.Stop();
            Util.Debug(string.Format("{0} 経過時間：{1} ms", s, sw.ElapsedMilliseconds));
            sw.Reset();
            sw.Start();
        }
        /// <summary>
        /// 만든 모션을 실행
        /// </summary>
        /// <param name="maid"></param>
        /// <param name="motionName"></param>
        /// <param name="isLoop"></param>
        /// <param name="fadeTime"></param>
        /// <param name="speed"></param>
        public static void animate(Maid maid, string motionName, bool isLoop, float fadeTime = 0.5f, float speed = 1f, bool addQue = false)
        {
            //string motionName= motionName1;

            try
            {
                // 원래 이 구현 부분은 별도 함수로 분리해야 맞을거 같긴 한데...
                FileInfo fileInfo = getFileFromList(motionName);
                //string strFile = UTY.gameProjectPath + "\\PhotoModeData\\MyPose\\" + Util.forceEndsWith(motionName1, ".anm");
                //FileInfo fileInfo = new FileInfo(strFile);
                Util.Debug("animate.5 : " + fileInfo.FullName);

                //로컬 파일 있는지 확인 있을때(true), 없으면(false)
                if (fileInfo.Exists)
                {
                    // 처리
                    motionName = fileInfo.FullName;
                    Util.Debug("animate.2 : " + motionName);
                    using (FileStream fileStream = new FileStream(motionName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] array = new byte[fileStream.Length];
                        fileStream.Read(array, 0, array.Length);
                        if (!addQue)
                        {
                            maid.body0.CrossFade(motionName, array, false, isLoop, false, fadeTime, 1f);
                            maid.body0.m_Animation[motionName].speed = speed;
                        }
                        else
                        {
                            maid.body0.CrossFade(motionName, array, false, isLoop, true, fadeTime, 1f);
                        }
                        Util.Debug(string.Format("모션 변경 {4}：{0}, loop:{1}, fade:{2}, speed:{3}", motionName, isLoop.ToString(), fadeTime, speed, maid.status.fullNameJpStyle));
                    }

                }
                else
                {
                    //후행 공백은 무시
                    motionName = Util.forceEndsWith(motionName, ".anm");
                    Util.Debug("animate.1 : " + motionName);

                    if (!addQue)
                    {
                        maid.CrossFadeAbsolute(motionName, false, isLoop, false, fadeTime, 1f);
                        maid.body0.m_Animation[motionName].speed = speed;
                    }
                    else
                    {
                        maid.CrossFade(motionName, false, isLoop, true, fadeTime, 1f);
                    }
                    Util.Debug(string.Format("모션 변경 {4}：{0}, loop:{1}, fade:{2}, speed:{3}", motionName, isLoop.ToString(), fadeTime, speed, maid.status.fullNameJpStyle));

                }
            }
            catch (Exception e)
            {
                Util.Info("모션 재생 실패2" + e.Message + "\r\n" + e.StackTrace);
            }
        }

        private static FileInfo getFileFromList(string motionName)
        {
            Util.Info("getFileFromList2:" + motionName + ":" + motionName.Length + ":" + motionName.EndsWith(@"\") + ":" + motionName.EndsWith("\\\r"));
            //string strFile= motionName;
            if (motionName.EndsWith(@"\") || motionName.EndsWith("\\\r"))
            {
                //System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(UTY.gameProjectPath + "\\PhotoModeData\\MyPose\\" + motionName.Substring(0, motionName.Length-1));
                //List<FileInfo> list = di.GetFiles("*.anm").ToList();
                //Util.info("getFileFromList2:" + list.ToString());

                // List<string> list = getFileFullpathList(UTY.gameProjectPath + "\\PhotoModeData\\MyPose\\" + motionName.Substring(0, motionName.Length - 1), "anm");
                List<string> list = getFileFullpathList(UTY.gameProjectPath + @"\PhotoModeData\MyPose\" + motionName, "anm");
                Util.Info("getFileFromList3:" + list[0]);
                //         public static string pickOneOrEmptyString(List<string> list, int excludeIndex = -1)
                if (list.Count == 0) return new FileInfo(motionName);

                //return list[random.Next(list.Count)];
                return new FileInfo(list[random.Next(list.Count)]);

                //foreach (System.IO.FileInfo File in di.GetFiles("*.anm"))
                //{
                //    if (File.Extension.ToLower().CompareTo(".anm") == 0)
                //    {
                //        String FullFileName = File.FullName;
                //        return new FileInfo(strFile);
                //    }
                //}
            }
            else
            {
                return new FileInfo(UTY.gameProjectPath + @"\PhotoModeData\MyPose\" + Util.forceEndsWith(motionName, ".anm"));
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// ±20%の範囲の値を返す
        /// Variation20Percent
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float var20p(float v)
        {
            return UnityEngine.Random.Range(v * 0.8f, v * 1.2f);
        }
        public static float var50p(float v)
        {
            return UnityEngine.Random.Range(v * 0.5f, v * 1.5f);
        }
        internal static float var50p(object sio_baseTime)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 목록에서 임의로 하나 선택
        /// </summary>
        /// <param name="list"></param>
        /// <param name="excludeIndex"></param>
        /// <returns></returns>
        public static string pickOneOrEmptyString(List<string> list, int excludeIndex = -1)
        //public static dynamic pickOneOrNull<T>(List<string> list, int excludeIndex = -1)    //プラグイン読み込み時にエラー
        {
            if (list.Count == 0) return "";
            return list[randomInt(0, list.Count - 1, excludeIndex)];
        }
        /// <summary>
        /// filepath에서 파일 내용을 읽기
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string[] readAllText(string filepath)
        {
            string[] csvTextAry;
            Util.Debug(string.Format("文字列読み込み開始 {0}", filepath));
            string csvContent = System.IO.File.ReadAllText(filepath);   //UTF-8のみ読み込み可能
            csvTextAry = csvContent.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (csvTextAry.Length < 2)
            {
                //改行コードが\nだった場合の保険
                csvTextAry = csvContent.Split(new string[] { "\n" }, StringSplitOptions.None);
            }
            Util.Debug(string.Format("文字列読み込み終了 {0}", filepath));
            return csvTextAry;
        }
        /// <summary>
        /// CSVファイル読み込み
        /// 1行目・１列目は読み飛ばし、カンマ区切り
        /// 2次元string配列で返す
        /// </summary>
        /// <param name="file">対象ファイルへのフルパス</param>
        /// <returns></returns>
        public static string[][] ReadCsvFile(string file, bool enSkipFirstCol = true)
        {
            string[] csvTextAry = readAllText(file);
            List<string[]> csvData = new List<string[]>();
            bool isReadedLabel = false;
            foreach (string m in csvTextAry)
            {
                List<string> lineData = new List<string>();
                //string m = sr.ReadLine();
                int i = 0;
                if (!isReadedLabel)
                {   //１行目はラベルなので読み飛ばす
                    isReadedLabel = true;
                    continue;
                }
                string[] values;
                if (enSkipFirstCol)
                {
                    // 읽어 들인 행에 대해 첫 번째 열을 날려 쉼표마다 나누어 배열에 저장
                    values = m.Split(',').Skip<string>(1).ToArray();
                }
                else
                {
                    values = m.Split(',').ToArray();
                }
                // 출력하기
                foreach (string value in values)
                {
                    if (value != "")
                    {
                        lineData.Add(value);
                    }
                    else if (i <= 3 && value == "")
                    {
                        lineData.Add("0");
                    }
                    ++i;
                }
                csvData.Add(lineData.ToArray());
            }
            Util.Debug(string.Format("문자 배열로 분할 종료 {0}", file));
            return csvData.ToArray();
        }
        public static void validate_morphValue(string sTag, float morph_value)
        {
            if (morph_value < 0 | 1 < morph_value)
                Util.Info(string.Format("「{0}」に指定した値「{1}」は無効です。０～１の値を指定してください", sTag, morph_value));
        }
        public static float parseFloat(string v, float defaultValue = 0)
        {
            try
            {
                return float.Parse(v);
            }
            catch
            {
                //にぎりつぶす
            }
            return defaultValue;
        }
        private static readonly List<MPN> MPN_ReversedList;
        internal static IEnumerable<MPN> getMPN_reversedList()
        {
            return MPN_ReversedList;
        }
        /// <summary>
        /// Unity 자원에서 확장 일치하는 파일을 검색하여 확장자없이 파일 이름의 목록을 반환한다.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        internal static ICollection<string> GetFilenameList_byExtension(string suffix, string[] excludeContentStrArray = null)
        {
            SortedDictionary<string, string> ret = new SortedDictionary<string, string>();
            if (excludeContentStrArray == null) excludeContentStrArray = new string[] { };
            Util.Debug("GetFilenameList_byExtension 除外対象：" + Util.list2Str(excludeContentStrArray));
            foreach (string file in GameUty.FileSystem.GetFileListAtExtension(suffix))      //몇 초 정도 걸릴
            {
                string filenameWithoutExt = Path.GetFileNameWithoutExtension(file);
                foreach (string s in excludeContentStrArray) { if (filenameWithoutExt.Contains(s)) goto GetFilenameList_byExtension_exitloop; }
                //ret.Add( filenameWithoutExt);
                ret[filenameWithoutExt] = filenameWithoutExt;
                GetFilenameList_byExtension_exitloop:;
            }
            return ret.Keys;
        }
        internal static string showPosAngle(Vector3 pos, Vector3 angle)
        {
            return string.Format("x:{0:f2} y:{1:f2} z:{2:f2} rx:{3:f1} ry:{4:f1} rz:{5:f1}", pos.x, pos.y, pos.z, angle.x, angle.y, angle.z);
        }
    }
    /// <summary>
    /// ScriplayContextX のスーパークラス
    /// 
    /// ScriplayContextバージョン追加時の対応手順：
    /// 　１．スクリプトバージョンに対応するScriptContextクラスを作成
    /// 　２．LATEST_VERSION　を更新
    /// 　３．readScriptFile()に対応バージョンの生成を記述
    /// </summary>
    public abstract class ScriplayContext
    {
        public static readonly int LATEST_VERSION = 2;
        /// <summary>
        /// スクリプト終了したか
        /// </summary>
        public bool scriptFinished
        {
            get { return this.scriptFinished_flag; }
            set
            {
                this.scriptFinished_flag = value;
                if (value) tearDown();
            }
        }
        public bool scriptFinished_flag = false;
        /// <summary>
        /// スクリプトのファイル名
        /// </summary>
        public readonly string scriptName = "";
        public static Regex reg_scriptInfo = new Regex(@"^@info\s+(.+)", RegexOptions.IgnoreCase);   //@info version=1 ...  スクリプトへの注釈
        //posAbsoluteで指定されるScriplay座標系。ワールド座標系に対するオフセット位置・回転で指定。
        //protected static Vector3 origin_pos = new Vector3();
        //protected static Vector3 origin_rot = new Vector3();
        //Scriplay座標系原点の位置・方向をGameObjectとして保持。参考：https://forum.unity.com/threads/making-a-new-transform-in-code.49277/#post-3776662
        private static GameObject scriplayOrigin_GO;
        private static Transform origin;
        private static GameObject tmpTarget_GO;
        public static Transform tmpTargetTransform;
        protected List<string> loadedFileList = new List<string>();
        protected List<int> startLineNoList = new List<int>();
        protected bool restoreProp_onTearDown = false;
        /// <summary>
        /// 現在実行しているファイル名と行番号をつけてログ出力
        /// </summary>
        /// <param name="message"></param>
        public void info(string message)
        {
            //search executing (loaded utility) file.
            int lineNoinFile = currentExecuteLine;
            string filename = "";
            int i = 0;
            foreach (int c in startLineNoList)
            {
                if (currentExecuteLine < c) break;
                lineNoinFile = currentExecuteLine - c;
                filename = loadedFileList[i];
                i++;
            }
            Util.Info(string.Format("{0} line{1}:{2}", filename, lineNoinFile, message));
        }
        private bool isOriginNull()
        {
            return (origin == null) || (origin.position == null) || (origin.eulerAngles == null);
        }
        public Vector3 convertPos_to_World(Vector3 scriplayPosition)
        {
            if (isOriginNull()) initOrigin();
            return origin.TransformPoint(scriplayPosition);
        }
        public Vector3 convertAngle_to_World(Vector3 scriplayAngle)
        {
            if (isOriginNull()) initOrigin();
            //return origin.TransformDirection(scriplayAngle);
            return origin.eulerAngles + scriplayAngle;
        }
        public Vector3 convertPos_to_Scriplay(Vector3 worldPosition)
        {
            if (isOriginNull()) initOrigin();
            return origin.InverseTransformPoint(worldPosition);
        }
        public Vector3 convertAngle_to_Scriplay(Vector3 worldAngle)
        {
            if (isOriginNull()) initOrigin();
            return origin.InverseTransformDirection(worldAngle);
        }
        public Vector3 getOriginPos()
        {
            if (isOriginNull()) initOrigin();
            return origin.position;
        }
        public Vector3 getOriginAngle()
        {
            if (isOriginNull()) initOrigin();
            return origin.eulerAngles;
        }
        public void setOriginPos(Vector3 pos)
        {
            if (isOriginNull()) initOrigin();
            origin.position = pos;
        }
        public void setOriginAngle(Vector3 angle)
        {
            if (isOriginNull()) initOrigin();
            origin.eulerAngles = angle;
        }
        public class PosRot
        {
            public readonly Vector3 pos;
            public readonly Vector3 rot;
            public PosRot(Vector3 pos, Vector3 rot)
            {
                this.pos = pos;
                this.rot = rot;
            }
        }
        /// <summary>
        /// ワールド座標系の位置・方向をScriplay座標系へ変換
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public PosRot toScriplay(Vector3 pos, Vector3 rot)
        {
            if (istmpTragetTransformNull()) initTmpTarget();
            //ワールド座標系で位置指定
            tmpTargetTransform.position = pos;
            tmpTargetTransform.eulerAngles = rot;
            //Scriplay座標系での値を返却
            PosRot ret = new PosRot(tmpTargetTransform.localPosition, tmpTargetTransform.localEulerAngles);
            return ret;
        }
        public PosRot toWorld(Vector3 pos, Vector3 rot)
        {
            if (istmpTragetTransformNull()) initTmpTarget();
            //Scriplay座標系で位置指定
            tmpTargetTransform.localPosition = pos;
            tmpTargetTransform.localEulerAngles = rot;
            //ワールド座標系での値を返却
            return new PosRot(tmpTargetTransform.position, tmpTargetTransform.eulerAngles);
        }
        private bool istmpTragetTransformNull()
        {
            return (tmpTargetTransform == null) || (tmpTargetTransform.position == null) || (tmpTargetTransform.eulerAngles == null);
        }
        private static void initTmpTarget()
        {
            tmpTarget_GO = ScriplayPlugin.createGO("tmpTarget");
            tmpTargetTransform = tmpTarget_GO.transform;
            tmpTargetTransform.parent = origin;
        }
        static ScriplayContext()
        {
            initOrigin();
        }
        private static void initOrigin()
        {
            scriplayOrigin_GO = ScriplayPlugin.createGO("scriplayOrigin");
            origin = scriplayOrigin_GO.transform;
            initTmpTarget();
        }
        protected ScriplayContext(string scriptName, bool finished = false, bool restoreProp_onTearDown = false)
        {
            this.scriptFinished = finished;
            this.scriptName = scriptName;
            this.restoreProp_onTearDown = restoreProp_onTearDown;
            if (!finished) ScriplayPlugin.initMaidList();
        }
        /// <summary>
        /// 対応するバージョンのScriptContextインスタンスを作成
        /// 特定の接頭辞のファイルをライブラリとして読み込み
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="scriptArray"></param>
        /// <returns></returns>
        public static ScriplayContext readScriptFile(string scriptName, string[] scriptArray, bool restoreProp_onTearDown = false)
        {
            List<string> list = new List<string>(scriptArray); list.Add("@exit"); scriptArray = list.ToArray(); //メインスクリプト終了後、後ろに結合したライブラリを実行しないように@exitをはさんでおく。
            int version = -1;
            foreach (string s in scriptArray)
            {
                if (reg_scriptInfo.IsMatch(s))
                {
                    version = int.Parse(parseParameter(reg_scriptInfo, s)["version"]);
                    Util.Info("スクリプトバージョンを検出しました : " + s);
                    break;
                }
            }
            if (version == -1)
            {
                Util.Info("スクリプトにバージョンの記述がありませんでした。最新版スクリプトとして読み込みます。");
                version = LATEST_VERSION;
            }
            if (version < 1 | LATEST_VERSION < version)
            {
                Util.Info("不明なバージョンです。最新版スクリプトとして読み込みます。　：　version " + version);
                version = LATEST_VERSION;
            }
            List<string> allScriptList = new List<string>();
            allScriptList.AddRange(scriptArray);
            List<string> _loadedFileList = new List<string>();
            List<int> _startLineNoList = new List<int>();
            _startLineNoList.Add(scriptArray.Length);
            foreach (string s in Util.getFileFullpathList(cfg.scriptsPath, suffix: "md", prefix: cfg.libFilePrefix))
            {
                FileInfo fi1 = new FileInfo(s);
                string[] array = Util.readAllText(s);
                _loadedFileList.Add(fi1.Name);
                _startLineNoList.Add(_startLineNoList.Last() + array.Length);
                allScriptList.AddRange(array);
                Util.Info(string.Format("「{0}」をライブラリとして読み込みました", fi1.Name));
            }
            ScriplayContext ret;
            switch (version)
            {
                case 1:
                    ret = ScriplayContextVer01.createScriplayContext(scriptName, allScriptList.ToArray());
                    break;
                case 2:
                    ret = ScriplayContextVer02.createScriplayContext(scriptName, allScriptList.ToArray(), restoreProp_onTearDown: restoreProp_onTearDown);
                    break;
                default:
                    return ScriplayContextVer01.None;
            }
            ret.loadedFileList.AddRange(_loadedFileList);
            ret.startLineNoList.AddRange(_startLineNoList);
            return ret;
        }
        public static ScriplayContext readScriptFile(string filePath)
        {
            Util.Info(string.Format("スクリプトファイル読み込み： {0}", filePath));
            FileInfo fi1 = new FileInfo(filePath);
            string[] contentArray = Util.readAllText(filePath);
            return readScriptFile(fi1.Name, contentArray);
        }
        /// <summary>
        /// コマンドのパラメータ文字列を解釈して辞書を返す
        /// ex.
        /// @command key1=value1...
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="lineStr"></param>
        /// <returns></returns>
        private static Dictionary<string, string> parseParameter(Regex reg, string lineStr)
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
        private static Dictionary<string, string> parseParameter(string paramStr)
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
                    Util.Info(string.Format("line{0} : パラメータを読み込めませんでした。「key=value」形式になっていますか？"));
                    continue;
                }
                ret.Add(kv[0], kv[1]);
            }
            return ret;
        }
        private static Regex parseParameter_regex = new Regex(@"\s+");
        private static Regex parseParameter_regex_header = new Regex(@"^\s+");
        private static Regex parseParameter_regex_footer = new Regex(@"\s+$");
        protected abstract void tearDown();
        public abstract void Update();
        /// <summary>
        /// 選択肢　選択できる項目
        /// </summary>
        public List<Selection> selection_selectionList = new List<Selection>();
        /// <summary>
        /// 表示領域に表示するテキスト
        /// </summary>
        private string _showText = "";
        public string showText
        {
            get
            {
                return _showText;
            }
            set
            {
                _showText = value;
                needUpdate_showText = true;
            }
        }
        public bool needUpdate_showText = false;
        public bool needUpdate_selection = false;
        /// <summary>
        /// 選択肢　選択された項目
        /// </summary>
        public Selection selection_selectedItem = Selection.None;
        /// <summary>
        /// 実行中の行の番号
        /// </summary>
        public int currentExecuteLine = -1;
        public class Selection
        {
            //Nullオブジェクト
            public static readonly Selection None = new Selection("選択肢なし", "", CommandType.GOTO);
            //フィールド一覧
            public readonly string viewStr;
            public readonly string paramLabel;
            public readonly CommandType itemType;
            public enum CommandType
            {
                GOTO, CALL, EXEC
            }
            //コンストラクタ
            public Selection(string viewStr, string paramLabel, CommandType type)
            {
                this.viewStr = viewStr;
                this.paramLabel = paramLabel;
                this.itemType = type;
            }
        }
    }
}
