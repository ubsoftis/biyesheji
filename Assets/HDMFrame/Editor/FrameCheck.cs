//========================================================================
// *作者：海盗猫  主页：ilPlay.com
// *脚本：FrameCheck
// *©2021 MYJL . All rights reserved.
//* ======================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace YinSDK
{
    public class FrameCheck : EditorWindow
    {
        public static EditorWindow thisWindow;

        [MenuItem("HDM框架/环境检测", false)]
        public static void CreatWin()
        {
            thisWindow = EditorWindow.GetWindow<FrameCheck>();
            thisWindow.titleContent = new GUIContent("资源管理系统", "开发工具集合");
            thisWindow.minSize = new Vector2(300, 400);
            thisWindow.maxSize = new Vector2(300, 400);
            thisWindow.ShowPopup();
        }

        /// <summary>
        /// 生命周期
        /// </summary>
        public void OnEnable()
        {
            thisWindow ??= this;
        }

        public void OnGUI()
        {
            GUILayout.Space(10);
            FrameModule.ImportFrame(uiFrame);
            FrameModule.ImportFrame(gameFrame);
            FrameModule.ImportFrame(logSystem);
            FrameModule.ImportFrame(xluaFrema);
            
        }


        /// <summary>
        /// UI框架导入环境
        /// </summary>
        public static ImportInfo uiFrame = new ImportInfo()
        {
            importButtonName = "导入UI框架",
            removeButtonName = "移除UI框架",
            importPackedPath = FrameInfo.UT_PACKED_PATH,
            frameFilePath = FrameInfo.UI_FILE_PATH,

            dependFilePath = new string[]
            {
                FrameInfo.DOTWEEN_FILE_PATH,
            },
            removeContent = new Action[]
            {
                ()=>{ FrameModule.RemoveMacro(FrameInfo.DOTWEEN_MACRO); },
            },
            dependContent = new Action[]
            {
                ()=>
                {
                    GUILayout.Space(20);
                    GUILayout.Label("DOTween环境", GUILayout.ExpandWidth(true));
                    GUILayout.Space(20);
                    if (GUILayout.Button("安装"))
                    {
                        FrameModule.AddMacro(FrameInfo.DOTWEEN_MACRO);
                        AssetDatabase.ImportPackage(FrameInfo.DOTWEEN_PACKED_PATH, false);
                    }
                },
            }
        };


        /// <summary>
        /// 游戏管理器导入环境
        /// </summary>
        public static ImportInfo gameFrame = new ImportInfo()
        {
            importButtonName = "导入游戏管理器",
            removeButtonName = "移除游戏管理器",
            importPackedPath = FrameInfo.GAMEMANAGE_PACKED_PATH,
            frameFilePath = FrameInfo.GAMEMANAGE_FILE_PATH,
            importContent = new Action[]
            {
                ()=>{FrameModule.AddMacro(FrameInfo.GAMEMANAGE_MACRO);},
            },
            removeContent = new Action[]
            {
                ()=>{FrameModule.RemoveMacro(FrameInfo.GAMEMANAGE_MACRO);},
            },
        };


        /// <summary>
        /// 游戏管理器导入环境
        /// </summary>
        public static ImportInfo logSystem = new ImportInfo()
        {
            importButtonName = "导入日志系统",
            removeButtonName = "移除日志系统",
            importPackedPath = FrameInfo.LOG_PACKED_PATH,
            frameFilePath = FrameInfo.LOG_FILE_PATH,

            removeContent = new Action[]
            {
                ()=>{FrameModule.RemoveMacro(FrameInfo.LOG_MACRO);},
            },

            importContent = new Action[]
            {
                ()=>{FrameModule.AddMacro(FrameInfo.LOG_MACRO);},
            },
        };

        /// <summary>
        /// 游戏管理器导入环境
        /// </summary>
        public static ImportInfo xluaFrema = new ImportInfo()
        {
            importButtonName = "导入XLUA框架",
            removeButtonName = "移除XLUA框架",
            importPackedPath = FrameInfo.XLUA_PACKED_PATH,
            frameFilePath = FrameInfo.XLUA_FILE_PATH,
            removeContent = new Action[]
            {
                ()=>{FrameModule.ClearFolder(FrameInfo.XLUAPLUGIN_FILE_PATH);},
            },
        };


    }
}



