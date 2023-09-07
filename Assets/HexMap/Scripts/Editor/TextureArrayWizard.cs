using UnityEditor;
using UnityEngine;

namespace HexMap.Scripts
{
    public class TextureArrayWizard : ScriptableWizard
    {
        public Texture2D[] textures;

        [MenuItem("Window/Create Texture Array")]
        private static void CreateWizard()
        {
            DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
        }

        // 点击Create按钮时调用
        private void OnWizardCreate()
        {
            if (textures.Length == 0) return;
            // 打开保存对话框，分别指定标题、默认文件名、默认扩展名、保存文件的路径
            // 返回的是用户选择的路径，string 类型
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Texture Array", 
                "Texture Array", 
                "asset", 
                "SaveTexture Array");
            if (path.Length == 0) return;
            Texture2D t = textures[0];
            Texture2DArray textureArray = new Texture2DArray(
                t.width, t.height, textures.Length, t.format, t.mipmapCount > 1);
            textureArray.anisoLevel = t.anisoLevel;
            textureArray.filterMode = t.filterMode;
            textureArray.wrapMode = t.wrapMode;
            for (int i = 0; i < textures.Length; i++)
            {
                for (int m = 0; m < t.mipmapCount; m++)
                {
                    Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
                }
            }
            AssetDatabase.CreateAsset(textureArray, path);
        }
    }
}