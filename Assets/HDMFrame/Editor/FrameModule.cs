using System.Collections;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Runtime.InteropServices;

public static class FrameModule
{
    /// <summary>
    /// 执行环境导入
    /// </summary>
    /// <param name="importInfo"> 导入信息 </param>
    public static void ImportFrame(ImportInfo importInfo)
    {
        if (Directory.Exists($"{Environment.CurrentDirectory}/{importInfo.frameFilePath}"))
        {
            if (GUILayout.Button(importInfo.removeButtonName, GUILayout.ExpandWidth(true)))
            {
                if (importInfo.dependFilePath != null && importInfo.dependFilePath.Length > 0)
                {
                    foreach (var item in importInfo.dependFilePath)
                    {
                        ClearFolder($"{Environment.CurrentDirectory}/{item}");
                    }
                }
                if (importInfo.removeContent != null && importInfo.removeContent.Length > 0)
                {
                    foreach (var item in importInfo.removeContent)
                    {
                        item();
                    }
                }
                ClearFolder($"{Environment.CurrentDirectory}/{importInfo.frameFilePath}");
                AssetDatabase.Refresh();
            }
        }
        else
        {
            bool finish = true;
            if (importInfo.dependFilePath != null && importInfo.dependFilePath.Length > 0)
            {
                foreach (var item in importInfo.dependFilePath)
                {
                    if (!Directory.Exists($"{Environment.CurrentDirectory}/{item}"))
                    {
                        finish = false;
                    }
                }
            }
            if (GUILayout.Button(importInfo.importButtonName, GUILayout.ExpandWidth(true)))
            {
                if (!finish)
                {
                    EditorUtility.DisplayDialog("环境提示！", "请先完成核心环境的导入！", "确认");
                }
                else
                {
                    if (importInfo.importContent != null && importInfo.importContent.Length > 0)
                    {
                        foreach (var item in importInfo.importContent)
                        {
                            item();
                        }
                    }
                    AssetDatabase.ImportPackage(importInfo.importPackedPath, false);
                }
            }
            if (!finish)
            {
                if (importInfo.dependContent != null && importInfo.dependContent.Length > 0)
                {
                    foreach (var item in importInfo.dependContent)
                    {
                        GUILayout.Space(10);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.BeginHorizontal();
                            {
                                item();
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
        GUILayout.Space(10);
    }


    /// <summary>
    /// 添加宏编译指令
    /// </summary>
    /// <param name="macroName"> 需要添加的宏名称 </param>
    public static void AddMacro(string macroName)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (buildTargetGroup == BuildTargetGroup.Unknown) return;

        string[] macro = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
        List<string> macroList = new List<string>(macro);

        foreach (var item in macro)
        {
            if (item == macroName) return;
        }

        macroList.Add(macroName);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", macroList.ToArray()));

        CompilationPipeline.RequestScriptCompilation();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 移除宏编译指令
    /// </summary>
    /// <param name="macroName"> 需要移除的宏名称 </param>
    public static void RemoveMacro(string macroName)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (buildTargetGroup == BuildTargetGroup.Unknown) return;

        string[] macro = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
        List<string> macroList = new List<string>(macro);

        foreach (var item in macro)
        {
            if (item == macroName)
                macroList.Remove(item);
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", macroList.ToArray()));

        CompilationPipeline.RequestScriptCompilation();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 清理指定文件下的内容
    /// </summary>
    /// <param name="path"></param>
    public static void ClearFolder(string path)
    {
        try
        {
            Directory.Delete(path, true);
            File.Delete(path + ".meta");
            AssetDatabase.Refresh();
        }
        catch (Exception)
        {
            if (EditorUtility.DisplayDialog("异常提醒!", "文件异常占用删除失败，请尝试重启Unity后删除", "确认重启","稍后重启")) 
            {
                EditorApplication.OpenProject(Application.dataPath.Replace("Assets", string.Empty));
            }
        }
    }

}
