using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImportInfo
{
    /// <summary>
    /// 框架文件目录
    /// </summary>
    public string frameFilePath;

    /// <summary>
    /// 导入按钮名称
    /// </summary>
    public string importButtonName;

    /// <summary>
    /// 移除按钮名称
    /// </summary>
    public string removeButtonName;

    /// <summary>
    /// 导入包路径
    /// </summary>
    public string importPackedPath;

    /// <summary>
    /// 依赖包体文件路径
    /// </summary>
    public string[] dependFilePath;

    /// <summary>
    /// 依赖的补充内容
    /// </summary>
    public Action[] dependContent;

    /// <summary>
    /// 移除的回调内容
    /// </summary>
    public Action[] removeContent;

    /// <summary>
    /// 导入的回调内容
    /// </summary>
    public Action[] importContent;
}


public static class FrameInfo
{
    /// <summary>
    /// 宏指令名称
    /// </summary>
    public const string LOG_MACRO = "HDMFRAME_LOG";
    public const string DOTWEEN_MACRO = "HDMFRAME_DOTWEEN";
    public const string ADDRESSABLE_MACRO = "HDMFRAME_ADDRESSABLE";
    public const string ENCRYPT_MACRO = "HDMFRAME_ENCRYPT";
    public const string GAMEMANAGE_MACRO = "HDMFRAME_GAMEMANAGE";
    public const string NET_MACRO = "HDMFRAME_NET";
    public const string XLUA_MACRO = "HDMFRAME_XLUA";

    /// <summary>
    /// 安装包路径
    /// </summary>
    public const string UT_PACKED_PATH = "Assets/HDMFrame/Packages/UIFrame.unitypackage";
    public const string DOTWEEN_PACKED_PATH = "Assets/HDMFrame/Packages/Dotween.unitypackage";
    public const string LOG_PACKED_PATH = "Assets/HDMFrame/Packages/LogSystem.unitypackage";
    public const string GAMEMANAGE_PACKED_PATH = "Assets/HDMFrame/Packages/GameManage.unitypackage";
    public const string XLUA_PACKED_PATH = "Assets/HDMFrame/Packages/XluaFrame.unitypackage";

    /// <summary>
    /// 安装文件路径
    /// </summary>
    public const string UI_FILE_PATH = "Assets/HDMFrame/Frame_UI";
    public const string DOTWEEN_FILE_PATH = "Assets/Plugins/Demigiant";
    public const string LOG_FILE_PATH = "Assets/HDMFrame/System_Log";
    public const string GAMEMANAGE_FILE_PATH = "Assets/HDMFrame/Frame_GameManage";
    public const string XLUA_FILE_PATH = "Assets/HDMFrame/XLua_Frame";
    public const string XLUAPLUGIN_FILE_PATH = "Assets/Plugins/Xlua";
}
