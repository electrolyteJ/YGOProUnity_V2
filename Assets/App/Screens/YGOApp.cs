using System;
using System.Collections.Generic;
using UnityEngine;
using App.Core;
using RuntimeDirectory = FileUtil.RuntimeDirectory;
/**
 *  ┌─────────────────────────────────────────────────────┐
    │                  场景加载                            │
    └─────────────────┬───────────────────────────────────┘
                      │
                      ▼
             ┌────────────────┐
             │   Awake()      │ ⭐ 第一个执行（当前已实现）
             │  初始化单例     │
             │  获取组件       │
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnEnable()    │ 对象启用时调用
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │   Start()      │ ⭐ 第一帧之前（当前已实现）
             │  初始化逻辑     │
             └───────┬────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │      每帧循环执行：          │
        │                            │
        │  ┌──────────────────┐     │
        │  │  Update()        │ ⭐ 每帧调用（当前已实现）
        │  │  处理输入        │     │
        │  │  游戏逻辑        │     │
        │  └────────┬─────────┘     │
        │           │                │
        │           ▼                │
        │  ┌──────────────────┐     │
        │  │  LateUpdate()    │     │
        │  │  摄像机跟随      │     │
        │  │  平滑移动        │     │
        │  └────────┬─────────┘     │
        │           │                │
        │           ▼                │
        │  ┌──────────────────┐     │
        │  │  OnGUI()         │     │
        │  │  GUI 渲染         │     │
        │  └──────────────────┘     │
        └────────────┬───────────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │   固定间隔（物理）：         │
        │  FixedUpdate()             │ 用于物理计算
        └────────────┬───────────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnDisable()   │ 对象禁用时
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnDestroy()   │ 对象销毁时
             └────────────────┘
 */
public class YGOApp : MonoBehaviour
{
      private const string CommandShellFileName = "commamd.shell";

    #region Resources
    public Camera main_camera;
    public facer face;
    public Light light;
    public AudioSource audio;
    public AudioClip zhankai;
    public GameObject mod_ui_2d;
    public GameObject mod_ui_3d;
    public GameObject mod_winExplode;
    public GameObject mod_loseExplode;
    public GameObject mod_audio_effect;
    public GameObject mod_ocgcore_card;
    public GameObject mod_ocgcore_card_cloude;
    public GameObject mod_ocgcore_card_number_shower;
    public GameObject mod_ocgcore_card_figure_line;
    public GameObject mod_ocgcore_hidden_button;
    public GameObject mod_ocgcore_coin;
    public GameObject mod_ocgcore_dice;
    public GameObject mod_simple_quad;
    public GameObject mod_simple_ngui_background_texture;
    public GameObject mod_simple_ngui_text;
    public GameObject mod_ocgcore_number;
    public GameObject mod_ocgcore_decoration_chain_selecting;
    public GameObject mod_ocgcore_decoration_card_selected;
    public GameObject mod_ocgcore_decoration_card_selecting;
    public GameObject mod_ocgcore_decoration_card_active;
    public GameObject mod_ocgcore_decoration_spsummon;
    public GameObject mod_ocgcore_decoration_thunder;
    public GameObject mod_ocgcore_decoration_trap_activated;
    public GameObject mod_ocgcore_decoration_magic_activated;
    public GameObject mod_ocgcore_decoration_magic_zhuangbei;
    public GameObject mod_ocgcore_decoration_removed;
    public GameObject mod_ocgcore_decoration_tograve;
    public GameObject mod_ocgcore_decoration_card_setted;
    public GameObject mod_ocgcore_blood;
    public GameObject mod_ocgcore_blood_screen;
    public GameObject mod_ocgcore_bs_atk_decoration;
    public GameObject mod_ocgcore_bs_atk_line_earth;
    public GameObject mod_ocgcore_bs_atk_line_water;
    public GameObject mod_ocgcore_bs_atk_line_fire;
    public GameObject mod_ocgcore_bs_atk_line_wind;
    public GameObject mod_ocgcore_bs_atk_line_dark;
    public GameObject mod_ocgcore_bs_atk_line_light;
    public GameObject mod_ocgcore_cs_chaining;
    public GameObject mod_ocgcore_cs_end;
    public GameObject mod_ocgcore_cs_bomb;
    public GameObject mod_ocgcore_cs_negated;
    public GameObject mod_ocgcore_cs_mon_earth;
    public GameObject mod_ocgcore_cs_mon_water;
    public GameObject mod_ocgcore_cs_mon_fire;
    public GameObject mod_ocgcore_cs_mon_wind;
    public GameObject mod_ocgcore_cs_mon_light;
    public GameObject mod_ocgcore_cs_mon_dark;
    public GameObject mod_ocgcore_ss_summon_earth;
    public GameObject mod_ocgcore_ss_summon_water;
    public GameObject mod_ocgcore_ss_summon_fire;
    public GameObject mod_ocgcore_ss_summon_wind;
    public GameObject mod_ocgcore_ss_summon_dark;
    public GameObject mod_ocgcore_ss_summon_light;
    public GameObject mod_ocgcore_ol_earth;
    public GameObject mod_ocgcore_ol_water;
    public GameObject mod_ocgcore_ol_fire;
    public GameObject mod_ocgcore_ol_wind;
    public GameObject mod_ocgcore_ol_dark;
    public GameObject mod_ocgcore_ol_light;
    public GameObject mod_ocgcore_ss_spsummon_normal;
    public GameObject mod_ocgcore_ss_spsummon_ronghe;
    public GameObject mod_ocgcore_ss_spsummon_tongtiao;
    public GameObject mod_ocgcore_ss_spsummon_yishi;
    public GameObject mod_ocgcore_ss_spsummon_link;
    public GameObject mod_ocgcore_ss_p_idle_effect;
    public GameObject mod_ocgcore_ss_p_sum_effect;
    public GameObject mod_ocgcore_ss_dark_hole;
    public GameObject mod_ocgcore_ss_link_mark;
    public GameObject new_ui_menu;
    public GameObject new_ui_setting;
    public GameObject new_ui_book;
    public GameObject new_ui_selectServer;
    public GameObject new_ui_gameInfo;
    public GameObject new_ui_cardDescription;
    public GameObject new_ui_search;
    public GameObject new_ui_searchDetailed;
    public GameObject new_ui_cardOnSearchList;
    public GameObject new_bar_changeSide;
    public GameObject new_bar_duel;
    public GameObject new_bar_room;
    public GameObject new_bar_editDeck;
    public GameObject new_bar_watchDuel;
    public GameObject new_bar_watchRecord;
    public GameObject new_mod_cardInDeckManager;
    public GameObject new_mod_tableInDeckManager;
    public GameObject new_ui_handShower;
    public GameObject new_ui_textMesh;
    public GameObject new_ui_superButton;
    public GameObject new_ui_superButtonTransparent;
    public GameObject new_ui_aiRoom;
    public GameObject new_ocgcore_field;
    public GameObject new_ocgcore_chainCircle;
    public GameObject new_ocgcore_wait;
    public GameObject new_mouse;
    public GameObject remaster_deckManager;
    public GameObject remaster_replayManager;
    public GameObject remaster_puzzleManager;
    public GameObject remaster_tagRoom;
    public GameObject remaster_room;
    public GameObject ES_1;
    public GameObject ES_2;
    public GameObject ES_2Force;
    public GameObject ES_3cancle;
    public GameObject ES_Single_multiple_window;
    public GameObject ES_Single_option;
    public GameObject ES_multiple_option;
    public GameObject ES_input;
    public GameObject ES_position;
    public GameObject ES_position3;
    public GameObject ES_Tp;
    public GameObject ES_Face;
    public GameObject ES_FS;
    public GameObject Pro1_CardShower;
    public GameObject Pro1_superCardShower;
    public GameObject Pro1_superCardShowerA;
    public GameObject New_arrow;
    public GameObject New_selectKuang;
    public GameObject New_chainKuang;
    public GameObject New_phase;
    public GameObject New_decker;
    public GameObject New_winCaculator;
    public GameObject New_winCaculatorRecord;
    public GameObject New_ocgcore_placeSelector;
    #endregion

    #region Initializement


    internal static string GetCommandShellPath()
    {
        return FileUtil.GetProjectRootFilePath(CommandShellFileName);
    }

    internal static void EnsureCommandShellExists()
    {
        RuntimeTextFile.EnsureFileExists(GetCommandShellPath());
    }

    internal static string ReadCommandShell()
    {
        using (StreamReader reader = new StreamReader(GetCommandShellPath(), Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    internal static void ClearCommandShell()
    {
        WriteCommandShellContents("");
    }

    internal static void DeleteCommandShell()
    {
        FileInfo shellFile = new FileInfo(GetCommandShellPath());
        if (shellFile.Exists)
        {
            shellFile.Delete();
        }
    }

    private static void WriteCommandShell(string command)
    {
        WriteCommandShellContents(command);
        YGOApp.exitOnReturn = true;
    }

    private static void WriteCommandShellContents(string command)
    {
        EnsureCommandShellExists();
        using (StreamWriter writer = new StreamWriter(GetCommandShellPath(), false, Encoding.UTF8))
        {
            writer.Write(command);
        }
    }



    void loadResources()
    {

        Util.loadResource(mod_audio_effect);
        Util.loadResource(mod_ocgcore_card);
        Util.loadResource(mod_ocgcore_card_cloude);
        Util.loadResource(mod_ocgcore_card_number_shower);
        Util.loadResource(mod_ocgcore_card_figure_line);
        Util.loadResource(mod_ocgcore_hidden_button);
        Util.loadResource(mod_ocgcore_coin);
        Util.loadResource(mod_ocgcore_dice);

        Util.loadResource(mod_ocgcore_decoration_chain_selecting);
        Util.loadResource(mod_ocgcore_decoration_card_selected);
        Util.loadResource(mod_ocgcore_decoration_card_selecting);
        Util.loadResource(mod_ocgcore_decoration_card_active);
        Util.loadResource(mod_ocgcore_decoration_spsummon);
        Util.loadResource(mod_ocgcore_decoration_thunder);
        Util.loadResource(mod_ocgcore_cs_mon_earth);
        Util.loadResource(mod_ocgcore_cs_mon_water);
        Util.loadResource(mod_ocgcore_cs_mon_fire);
        Util.loadResource(mod_ocgcore_cs_mon_wind);
        Util.loadResource(mod_ocgcore_cs_mon_light);
        Util.loadResource(mod_ocgcore_cs_mon_dark);
        Util.loadResource(mod_ocgcore_decoration_trap_activated);
        Util.loadResource(mod_ocgcore_decoration_magic_activated);
        Util.loadResource(mod_ocgcore_decoration_magic_zhuangbei);

        Util.loadResource(mod_ocgcore_decoration_removed);
        Util.loadResource(mod_ocgcore_decoration_tograve);
        Util.loadResource(mod_ocgcore_decoration_card_setted);
        Util.loadResource(mod_ocgcore_blood);
        Util.loadResource(mod_ocgcore_blood_screen);


        Util.loadResource(mod_ocgcore_bs_atk_decoration);
        Util.loadResource(mod_ocgcore_bs_atk_line_earth);
        Util.loadResource(mod_ocgcore_bs_atk_line_water);
        Util.loadResource(mod_ocgcore_bs_atk_line_fire);
        Util.loadResource(mod_ocgcore_bs_atk_line_wind);
        Util.loadResource(mod_ocgcore_bs_atk_line_dark);
        Util.loadResource(mod_ocgcore_bs_atk_line_light);

        Util.loadResource(mod_ocgcore_cs_chaining);
        Util.loadResource(mod_ocgcore_cs_end);
        Util.loadResource(mod_ocgcore_cs_bomb);
        Util.loadResource(mod_ocgcore_cs_negated);

        Util.loadResource(mod_ocgcore_ss_summon_earth);
        Util.loadResource(mod_ocgcore_ss_summon_water);
        Util.loadResource(mod_ocgcore_ss_summon_fire);
        Util.loadResource(mod_ocgcore_ss_summon_wind);
        Util.loadResource(mod_ocgcore_ss_summon_dark);
        Util.loadResource(mod_ocgcore_ss_summon_light);

        Util.loadResource(mod_ocgcore_ol_earth);
        Util.loadResource(mod_ocgcore_ol_water);
        Util.loadResource(mod_ocgcore_ol_fire);
        Util.loadResource(mod_ocgcore_ol_wind);
        Util.loadResource(mod_ocgcore_ol_dark);
        Util.loadResource(mod_ocgcore_ol_light);

        Util.loadResource(mod_ocgcore_ss_spsummon_normal);
        Util.loadResource(mod_ocgcore_ss_spsummon_ronghe);
        Util.loadResource(mod_ocgcore_ss_spsummon_tongtiao);
        Util.loadResource(mod_ocgcore_ss_spsummon_link);
        Util.loadResource(mod_ocgcore_ss_spsummon_yishi);
        Util.loadResource(mod_ocgcore_ss_p_idle_effect);
        Util.loadResource(mod_ocgcore_ss_p_sum_effect);
        Util.loadResource(mod_ocgcore_ss_dark_hole);
        Util.loadResource(mod_ocgcore_ss_link_mark);
    }

    public static float transparency = 0;

    //public static bool YGOPro1 = true;

    public static float getVerticalTransparency()
    {
        if (I().setting.setting.closeUp.value == false)
        {
            return 0;
        }
        return transparency;
    }

    public static GameObject ui_back_ground_2d = null;
    public static Camera camera_back_ground_2d = null;
    public static GameObject ui_container_3d = null;
    public static Camera camera_container_3d = null;
    public static Camera camera_game_main = null;
    public static GameObject ui_windows_2d = null;
    public static Camera camera_windows_2d = null;
    public static GameObject ui_main_2d = null;
    public static Camera camera_main_2d = null;
    public static GameObject ui_main_3d = null;
    public static Camera camera_main_3d = null;

    public static Vector3 cameraPosition = new Vector3(0, 23, -23);
    public static Vector3 cameraRotation = new Vector3(60, 0, 0);
    public static bool cameraFacing = false;

    public static float verticleScale = 5f;

    void initialize()
    {

        Util.go(1, () =>
        {
            UIHelper.iniFaces();
            initializeALLcameras();
            fixALLcamerasPreFrame();
            backGroundPic = new BackGroundPic();
            servants.Add(backGroundPic);
            backGroundPic.fixScreenProblem();
        });
        Util.go(300, () =>
        {
            InterString.initialize(FileUtil.GetFilePath(RuntimeDirectory.Config, "translation.conf"));
            GameTextureManager.initialize();
            Config.initialize(FileUtil.GetFilePath(RuntimeDirectory.Config, "config.conf"));
            FileUtil.EnsureDirectory(RuntimeDirectory.Expansions);
            FileUtil.EnsureDirectory(RuntimeDirectory.Replay);
            FileUtil.EnsureDirectory(RuntimeDirectory.Deck);
            var fileInfos = new FileInfo[0];
            if (FileUtil.DirectoryExists(RuntimeDirectory.Expansions))
            {
                fileInfos = FileUtil.GetFiles(RuntimeDirectory.Expansions);
                foreach (FileInfo file in fileInfos)
                {
                    if (file.Name.ToLower().EndsWith(".ypk"))
                    {
                        GameZipManager.Zips.Add(new Ionic.Zip.ZipFile(FileUtil.GetFilePath(RuntimeDirectory.Expansions, file.Name)));
                    }
                    if (file.Name.ToLower().EndsWith(".conf"))
                    {
                        GameStringManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Expansions, file.Name));
                    }
                    if (file.Name.ToLower().EndsWith(".cdb"))
                    {
                        YGOSharp.CardsManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Expansions, file.Name));
                    }
                }
            }

            if (FileUtil.DirectoryExists(RuntimeDirectory.Cdb))
            {
                fileInfos = FileUtil.GetFiles(RuntimeDirectory.Cdb);
                foreach (FileInfo file in fileInfos)
                {
                    if (file.Name.ToLower().EndsWith(".conf"))
                    {
                        GameStringManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Cdb, file.Name));
                    }
                    if (file.Name.ToLower().EndsWith(".cdb"))
                    {
                        YGOSharp.CardsManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Cdb, file.Name));
                    }
                }
            }

            if (FileUtil.DirectoryExists(RuntimeDirectory.Diy))
            {
                fileInfos = FileUtil.GetFiles(RuntimeDirectory.Diy);
                foreach (FileInfo file in fileInfos)
                {
                    if (file.Name.ToLower().EndsWith(".conf"))
                    {
                        GameStringManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Diy, file.Name));
                    }
                    if (file.Name.ToLower().EndsWith(".cdb"))
                    {
                        YGOSharp.CardsManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Diy, file.Name), true);
                    }
                }
            }

            if (FileUtil.DirectoryExists(RuntimeDirectory.Data))
            {
                fileInfos = FileUtil.GetFiles(RuntimeDirectory.Data);
                foreach (FileInfo file in fileInfos)
                {
                    if (file.Name.ToLower().EndsWith(".zip"))
                    {
                        GameZipManager.Zips.Add(new Ionic.Zip.ZipFile(FileUtil.GetFilePath(RuntimeDirectory.Data, file.Name)));
                    }
                }
            }

            foreach (ZipFile zip in GameZipManager.Zips)
            {
                if (zip.Name.ToLower().EndsWith("script.zip"))
                    continue;
                foreach (string file in zip.EntryFileNames)
                {
                    if (file.ToLower().EndsWith(".conf"))
                    {
                        MemoryStream ms = new MemoryStream();
                        ZipEntry e = zip[file];
                        e.Extract(ms);
                        GameStringManager.initializeContent(Encoding.UTF8.GetString(ms.ToArray()));
                    }
                    if (file.ToLower().EndsWith(".cdb"))
                    {
                        ZipEntry e = zip[file];
                        string tempfile = Path.Combine(Path.GetTempPath(), file);
                        e.Extract(Path.GetTempPath(), ExtractExistingFileAction.OverwriteSilently);
                        YGOSharp.CardsManager.initialize(tempfile, true);
                        File.Delete(tempfile);
                    }
                }
            }

            GameStringManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Config, "strings.conf"));
            YGOSharp.BanlistManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Config, "lflist.conf"));

            YGOSharp.CardsManager.updateSetNames();

            if (FileUtil.DirectoryExists(RuntimeDirectory.Pack))
            {
                fileInfos = FileUtil.GetFiles(RuntimeDirectory.Pack);
                foreach (FileInfo file in fileInfos)
                {
                    if (file.Name.ToLower().EndsWith(".db"))
                    {
                        YGOSharp.PacksManager.initialize(FileUtil.GetFilePath(RuntimeDirectory.Pack, file.Name));
                    }
                }
                YGOSharp.PacksManager.initializeSec();
            }

            initializeALLservants();
            loadResources();
            readParams();
        });

    }

    void readParams()
    {
        var args = Environment.GetCommandLineArgs();
        string nick = null;
        string host = null;
        string port = null;
        string password = null;
        string deck = null;
        string replay = null;
        string puzzle = null;
        bool join = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == "-n" && args.Length > i + 1)
            {
                nick = args[++i];
                if (nick.Contains(" "))
                    nick = "\"" + nick + "\"";
            }
            if (args[i].ToLower() == "-h" && args.Length > i + 1)
            {
                host = args[++i];
            }
            if (args[i].ToLower() == "-p" && args.Length > i + 1)
            {
                port = args[++i];
            }
            if (args[i].ToLower() == "-w" && args.Length > i + 1)
            {
                password = args[++i];
                if (password.Contains(" "))
                    password = "\"" + password + "\"";
            }
            if (args[i].ToLower() == "-d" && args.Length > i + 1)
            {
                deck = args[++i];
                if (deck.Contains(" "))
                    deck = "\"" + deck + "\"";
            }
            if (args[i].ToLower() == "-r" && args.Length > i + 1)
            {
                replay = args[++i];
                if (replay.Contains(" "))
                    replay = "\"" + replay + "\"";
            }
            if (args[i].ToLower() == "-s" && args.Length > i + 1)
            {
                puzzle = args[++i];
                if (puzzle.Contains(" "))
                    puzzle = "\"" + puzzle + "\"";
            }
            if (args[i].ToLower() == "-j")
            {
                join = true;
                Config.Set("deckInUse", deck);
            }
        }
        if (join)
        {
            WriteCommandShell("online " + nick + " " + host + " " + port + " 0x233 " + password);
        }
        else if (deck != null)
        {
            WriteCommandShell("edit " + deck);
        }
        else if (replay != null)
        {
            WriteCommandShell("replay " + replay);
        }
        else if (puzzle != null)
        {
            WriteCommandShell("puzzle " + puzzle);
        }
    }

    public GameObject mouseParticle;

    static int lastChargeTime = 0;
    public static void charge()
    {
        if (Util.TimePassed() - lastChargeTime > 5 * 60 * 1000)
        {
            lastChargeTime = Util.TimePassed();
            try
            {
                GameTextureManager.clearAll();
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
    }

    #endregion

    #region Tools

    public static GameObject pointedGameObject = null;

    public static Collider pointedCollider = null;

    public static bool InputGetMouseButtonDown_0;

    public static bool InputGetMouseButton_0;

    public static bool InputGetMouseButtonUp_0;

    public static bool InputGetMouseButtonDown_1;

    public static bool InputGetMouseButtonUp_1;

    public static bool InputEnterDown = false;

    public static float wheelValue = 0;

    int rayFilter = 0;

    public void initializeALLcameras()
    {
        for (int i = 0; i < 32; i++)
        {
            if (i == 15)
            {
                continue;
            }
            rayFilter |= (int)Math.Pow(2, i);
        }

        if (camera_game_main == null)
        {
            camera_game_main = this.main_camera;
        }
        camera_game_main.transform.position = new Vector3(0, 23, -23);
        camera_game_main.transform.eulerAngles = new Vector3(60, 0, 0);
        camera_game_main.transform.localScale = new Vector3(1, 1, 1);
        camera_game_main.rect = new Rect(0, 0, 1, 1);
        camera_game_main.depth = 0;
        camera_game_main.gameObject.layer = 0;
        camera_game_main.clearFlags = CameraClearFlags.Depth;

        if (ui_back_ground_2d == null)
        {
            ui_back_ground_2d = create(mod_ui_2d);
            camera_back_ground_2d = ui_back_ground_2d.transform.Find("Camera").GetComponent<Camera>();
        }
        camera_back_ground_2d.depth = -2;
        ui_back_ground_2d.layer = 8;
        ui_back_ground_2d.transform.Find("Camera").gameObject.layer = 8;
        camera_back_ground_2d.cullingMask = (int)Mathf.Pow(2, 8);
        camera_back_ground_2d.clearFlags = CameraClearFlags.Depth;

        if (ui_container_3d == null)
        {
            ui_container_3d = create(mod_ui_3d);
            camera_container_3d = ui_container_3d.transform.Find("Camera").GetComponent<Camera>();
        }
        camera_container_3d.depth = -1;
        ui_container_3d.layer = 9;
        ui_container_3d.transform.Find("Camera").gameObject.layer = 9;
        camera_container_3d.cullingMask = (int)Mathf.Pow(2, 9);
        camera_container_3d.fieldOfView = 75;
        camera_container_3d.rect = camera_game_main.rect;
        camera_container_3d.transform.position = new Vector3(0, 23, -23);
        camera_container_3d.transform.eulerAngles = new Vector3(60, 0, 0);
        camera_container_3d.transform.localScale = new Vector3(1, 1, 1);
        camera_container_3d.rect = new Rect(0, 0, 1, 1);
        camera_container_3d.clearFlags = CameraClearFlags.Depth;



        if (ui_main_2d == null)
        {
            ui_main_2d = create(mod_ui_2d);
            camera_main_2d = ui_main_2d.transform.Find("Camera").GetComponent<Camera>();
        }
        camera_main_2d.depth = 3;
        ui_main_2d.layer = 11;
        ui_main_2d.transform.Find("Camera").gameObject.layer = 11;
        camera_main_2d.cullingMask = (int)Mathf.Pow(2, 11);
        camera_main_2d.clearFlags = CameraClearFlags.Depth;


        if (ui_windows_2d == null)
        {
            ui_windows_2d = create(mod_ui_2d);
            camera_windows_2d = ui_windows_2d.transform.Find("Camera").GetComponent<Camera>();
        }
        camera_windows_2d.depth = 2;
        ui_windows_2d.layer = 19;
        ui_windows_2d.transform.Find("Camera").gameObject.layer = 19;
        camera_windows_2d.cullingMask = (int)Mathf.Pow(2, 19);
        camera_windows_2d.clearFlags = CameraClearFlags.Depth;


        if (ui_main_3d == null)
        {
            ui_main_3d = create(mod_ui_3d);
            camera_main_3d = ui_main_3d.transform.Find("Camera").GetComponent<Camera>();
        }
        camera_main_3d.depth = 1;
        ui_main_3d.layer = 10;
        ui_main_3d.transform.Find("Camera").gameObject.layer = 10;
        camera_main_3d.cullingMask = (int)Mathf.Pow(2, 10);
        camera_main_3d.fieldOfView = 75;
        camera_main_3d.rect = new Rect(0, 0, 1, 1);
        camera_main_3d.transform.position = new Vector3(0, 23, -23);
        camera_main_3d.transform.eulerAngles = new Vector3(60, 0, 0);
        camera_main_3d.transform.localScale = new Vector3(1, 1, 1);
        camera_main_3d.clearFlags = CameraClearFlags.Depth;




        camera_main_3d.transform.localPosition = camera_game_main.transform.position;
        camera_container_3d.transform.localPosition = camera_game_main.transform.position;

        camera_main_3d.transform.localEulerAngles = camera_game_main.transform.localEulerAngles;
        camera_container_3d.transform.localEulerAngles = camera_game_main.transform.localEulerAngles;

        camera_main_3d.fieldOfView = camera_game_main.fieldOfView;
        camera_container_3d.fieldOfView = camera_game_main.fieldOfView;

        camera_main_3d.rect = camera_game_main.rect;
        camera_container_3d.rect = camera_game_main.rect;
    }

    public static float deltaTime = 1f / 120f;

    public void fixALLcamerasPreFrame()
    {
        deltaTime = Time.deltaTime;
        if (deltaTime > 1f / 40f)
        {
            deltaTime = 1f / 40f;
        }
        if (camera_game_main != null)
        {
            camera_game_main.transform.position += (cameraPosition - camera_game_main.transform.position) * deltaTime * 3.5f;
            camera_container_3d.transform.localPosition = camera_game_main.transform.position;
            if (cameraFacing == false)
            {
                camera_game_main.transform.localEulerAngles += (cameraRotation - camera_game_main.transform.localEulerAngles) * deltaTime * 3.5f;
            }
            else
            {
                camera_game_main.transform.LookAt(Vector3.zero);
            }
            camera_container_3d.transform.localEulerAngles = camera_game_main.transform.localEulerAngles;
            camera_container_3d.fieldOfView = camera_game_main.fieldOfView;
            camera_container_3d.rect = camera_game_main.rect;
        }
    }

    public void fixScreenProblems()
    {
        for (int i = 0; i < servants.Count; i++)
        {
            servants[i].fixScreenProblem();
        }
    }

    public GameObject create(
        GameObject mod,
        Vector3 position = default(Vector3),
        Vector3 rotation = default(Vector3),
        bool fade = false,
        GameObject father = null,
        bool allParamsInWorld = true,
        Vector3 wantScale = default(Vector3)
        )
    {
        Vector3 scale = mod.transform.localScale;
        if (wantScale != default(Vector3))
        {
            scale = wantScale;
        }
        GameObject return_value = (GameObject)MonoBehaviour.Instantiate(mod);
        if (position != default(Vector3))
        {
            return_value.transform.position = position;
        }
        else
        {
            return_value.transform.position = Vector3.zero;
        }
        if (rotation != default(Vector3))
        {
            return_value.transform.eulerAngles = rotation;
        }
        else
        {
            return_value.transform.eulerAngles = Vector3.zero;
        }
        if (father != null)
        {
            return_value.transform.SetParent(father.transform, false);
            return_value.layer = father.layer;
            if (allParamsInWorld == true)
            {
                return_value.transform.position = position;
                return_value.transform.localScale = scale;
                return_value.transform.eulerAngles = rotation;
            }
            else
            {
                return_value.transform.localPosition = position;
                return_value.transform.localScale = scale;
                return_value.transform.localEulerAngles = rotation;
            }
        }
        else
        {
            return_value.layer = 0;
        }
        Transform[] Transforms = return_value.GetComponentsInChildren<Transform>();
        foreach (Transform child in Transforms)
        {
            child.gameObject.layer = return_value.layer;
        }
        if (fade == true)
        {
            return_value.transform.localScale = Vector3.zero;
            iTween.ScaleToE(return_value, scale, 0.3f);
        }
        return return_value;
    }

    public void destroy(GameObject obj, float time = 0, bool fade = false, bool instantNull = false)
    {
        try
        {
            if (obj != null)
            {
                if (fade)
                {
                    iTween.ScaleTo(obj, Vector3.zero, 0.4f);
                    MonoBehaviour.Destroy(obj, 0.6f);
                }
                else
                {
                    if (time != 0) MonoBehaviour.Destroy(obj, time);
                    else MonoBehaviour.Destroy(obj);
                }
                if (instantNull)
                {
                    obj = null;
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
    }

    //public static void shiftCameraPan(Camera camera, bool enabled)
    //{
    //    cameraPaning = enabled;
    //    PanWithMouse panWithMouse = camera.gameObject.GetComponent<PanWithMouse>();
    //    if (panWithMouse == null)
    //    {
    //        panWithMouse = camera.gameObject.AddComponent<PanWithMouse>();
    //    }
    //    panWithMouse.enabled = enabled;
    //    if (enabled == false)
    //    {
    //        iTween.RotateTo(camera.gameObject, new Vector3(60, 0, 0), 0.6f);
    //    }
    //}

    public static void reMoveCam(float xINscreen)
    {
        float all = (float)Screen.width / 2f;
        float it = xINscreen - (float)Screen.width / 2f;
        float val = it / all;
        camera_game_main.rect = new Rect(val, 0, 1, 1);
        camera_container_3d.rect = camera_game_main.rect;
        camera_main_3d.rect = camera_game_main.rect;
    }

    public static void ShiftUIenabled(GameObject ui, bool enabled)
    {
        var all = ui.GetComponentsInChildren<BoxCollider>();
        for (int i = 0; i < all.Length; i++)
        {
            all[i].enabled = enabled;
        }
    }

    #endregion

    #region Servants

    List<Servant> servants = new List<Servant>();

    public Servant backGroundPic;
    public Menu menu;
    public Setting setting;
    public selectDeck selectDeck;
    public selectReplay selectReplay;
    public Room room;
    public CardDescription cardDescription;
    public DeckManager deckManager;
    public Ocgcore ocgcore;
    public SelectServer selectServer;
    public Book book;
    public puzzleMode puzzleMode;
    public AIRoom aiRoom;

    void initializeALLservants()
    {
        menu = new Menu();
        servants.Add(menu);
        setting = new Setting();
        servants.Add(setting);
        selectDeck = new selectDeck();
        servants.Add(selectDeck);
        room = new Room();
        servants.Add(room);
        cardDescription = new CardDescription();
        deckManager = new DeckManager();
        servants.Add(deckManager);
        ocgcore = new Ocgcore();
        servants.Add(ocgcore);
        selectServer = new SelectServer();
        servants.Add(selectServer);
        book = new Book();
        servants.Add(book);
        selectReplay = new selectReplay();
        servants.Add(selectReplay);
        puzzleMode = new puzzleMode();
        servants.Add(puzzleMode);
        aiRoom = new AIRoom();
        servants.Add(aiRoom);
    }

    public void shiftToServant(Servant to)
    {
        if (to != backGroundPic && backGroundPic.isShowed)
        {
            backGroundPic.hide();
        }
        if (to != menu && menu.isShowed)
        {
            menu.hide();
        }
        if (to != setting && setting.isShowed)
        {
            setting.hide();
        }
        if (to != selectDeck && selectDeck.isShowed)
        {
            selectDeck.hide();
        }
        if (to != room && room.isShowed)
        {
            room.hide();
        }
        if (to != deckManager && deckManager.isShowed)
        {
            deckManager.hide();
        }
        if (to != ocgcore && ocgcore.isShowed)
        {
            ocgcore.hide();
        }
        if (to != selectServer && selectServer.isShowed)
        {
            selectServer.hide();
        }
        if (to != selectReplay && selectReplay.isShowed)
        {
            selectReplay.hide();
        }
        if (to != puzzleMode && puzzleMode.isShowed)
        {
            puzzleMode.hide();
        }
        if (to != aiRoom && aiRoom.isShowed)
        {
            aiRoom.hide();
        }

        if (to == backGroundPic && backGroundPic.isShowed == false) backGroundPic.show();
        if (to == menu && menu.isShowed == false) menu.show();
        if (to == setting && setting.isShowed == false) setting.show();
        if (to == selectDeck && selectDeck.isShowed == false) selectDeck.show();
        if (to == room && room.isShowed == false) room.show();
        if (to == deckManager && deckManager.isShowed == false) deckManager.show();
        if (to == ocgcore && ocgcore.isShowed == false) ocgcore.show();
        if (to == selectServer && selectServer.isShowed == false) selectServer.show();
        if (to == selectReplay && selectReplay.isShowed == false) selectReplay.show();
        if (to == puzzleMode && puzzleMode.isShowed == false) puzzleMode.show();
        if (to == aiRoom && aiRoom.isShowed == false) aiRoom.show();

    }

    #endregion

    #region MonoBehaviors

    void Start()
    {
        Debug.Log("=== Start() 开始 ===");
        
        InitializeStartup(owner); // Prepare
        initialize(); // initialize 
        ScheduleGameStart(owner);   // Complete
        Debug.Log("=== Start() 完成 ===");
    }
    private static void InitializeStartup(global::Program owner)
    {
        Debug.Log("[InitializeStartup] 开始初始化启动配置...");
    
        // 设置分辨率
        if (Screen.width < 100 || Screen.height < 100)
        {
            Screen.SetResolution(1300, 700, false);
        }
    
        // 设置帧率
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;
    
        Debug.Log("[InitializeStartup] 启动配置完成");
    }
    
    private static void ScheduleGameStart(global::Program owner)
    {
        Debug.Log("[ScheduleGameStart] 调度游戏启动...");
        Debug.Log("[ScheduleGameStart] 游戏启动调度完成");
    }

    int preWid = 0;

    int preheight = 0;

    public static float _padScroll = 0;

    void OnGUI()
    {
        if (Event.current.type == EventType.ScrollWheel)
            _padScroll = -Event.current.delta.y / 100;
        else
            _padScroll = 0;
    }

    void Update()
    {
       
        if (preWid != Screen.width || preheight != Screen.height)
        {
            Resources.UnloadUnusedAssets();
            onRESIZED();
        }
        fixALLcamerasPreFrame();
        wheelValue = UICamera.GetAxis("Mouse ScrollWheel") * 50;
        pointedGameObject = null;
        pointedCollider = null;
        Ray line = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(line, out hit, (float)1000, rayFilter))
        {
            pointedGameObject = hit.collider.gameObject;
            pointedCollider = hit.collider;
        }
        GameObject hoverobject = UICamera.Raycast(Input.mousePosition) ? UICamera.lastHit.collider.gameObject : null;
        if (hoverobject != null)
        {
            if (hoverobject.layer == 11 || pointedGameObject == null)
            {
                pointedGameObject = hoverobject;
                pointedCollider = UICamera.lastHit.collider;
            }
        }
        InputGetMouseButtonDown_0 = Input.GetMouseButtonDown(0);
        InputGetMouseButtonUp_0 = Input.GetMouseButtonUp(0);
        InputGetMouseButtonDown_1 = Input.GetMouseButtonDown(1);
        InputGetMouseButtonUp_1 = Input.GetMouseButtonUp(1);
        InputEnterDown = Input.GetKeyDown(KeyCode.Return);
        InputGetMouseButton_0 = Input.GetMouseButton(0);
        for (int i = 0; i < servants.Count; i++)
        {
            servants[i].Update();
        }
        TcpHelper.preFrameFunction();
        Util.clear();

    }

    private void onRESIZED()
    {
        preWid = Screen.width;
        preheight = Screen.height;
        //if (setting != null)
        //    setting.setScreenSizeValue();
        Util.notGo(fixScreenProblems);
        Util.go(500, fixScreenProblems);
    }

    public static void DEBUGLOG(object o)
    {
#if UNITY_EDITOR
        Debug.Log(o);
#endif
    }

    public static void PrintToChat(object o)
    {
        try
        {
            this.cardDescription.mLog(o.ToString());
        }
        catch
        {
            DEBUGLOG(o);
        }
    }

    void gameStart()
    {
        if (UIHelper.shouldMaximize())
        {
            UIHelper.MaximizeWindow();
        }
        backGroundPic.show();
        shiftToServant(menu);
    }

    internal void RunGameStartFromBridge()
    {
        gameStart();
    }

    public static bool Running = true;

    public static bool MonsterCloud = false;
    public static float fieldSize = 1;
    public static bool longField = false;

    public static bool noAccess = false;

    public static bool exitOnReturn = false;

    void OnApplicationQuit()
    {
        TcpHelper.SaveRecord();
        cardDescription.save();
        setting.saveWhenQuit();
        for (int i = 0; i < servants.Count; i++)
        {
            servants[i].OnQuit();
        }
        Running = false;
        try
        {
            TcpHelper.tcpClient.Close();
        }
        catch (System.Exception e)
        {
            //adeUnityEngine.Debug.Log(e);
        }
        Menu.deleteShell();
        foreach (ZipFile zip in GameZipManager.Zips)
        {
            zip.Dispose();
        }
        aiRoom.killServerProcess();
    }

    public void quit()
    {
        OnApplicationQuit();
    }

    #endregion

    public static void gugugu()
    {
        PrintToChat(InterString.Get("非常抱歉，因为技术原因，此功能暂时无法使用。请关注官方网站获取更多消息。"));
    }
}
