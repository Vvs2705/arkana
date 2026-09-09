using UnityEditor;
using UnityEngine;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Monta a cena principal POR CODIGO (como o Godot fazia com Main.tscn instanciando Island + player + bots + HUD).
    /// Cena montada por codigo = zero binario de cena para mesclar e a montagem e' testavel.
    /// Preenchido na raia CENA; ate' la' e' um stub que so' cria a cena vazia.
    /// </summary>
    public static class MainSceneBuilder
    {
        [MenuItem("Arkana/Montar cena Main")]
        public static void Build()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            System.IO.Directory.CreateDirectory("Assets/_Arkana/Scenes");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, Build_ScenePath());
            AssetDatabase.SaveAssets();
            Debug.Log("MainSceneBuilder: cena stub salva em " + Build_ScenePath());
        }

        static string Build_ScenePath() => Arkana.EditorTools.Build.ScenePath;
    }
}
