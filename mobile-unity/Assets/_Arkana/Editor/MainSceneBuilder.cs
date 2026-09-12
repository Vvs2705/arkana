using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Arkana.World;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Monta a cena principal POR CODIGO (como o Godot fazia com Main.tscn instanciando Island + player + bots + HUD).
    /// Cena montada por codigo = zero binario de cena para mesclar e a montagem e' testavel.
    /// A cena tem TRES objetos: Main (o ciclo de vida), a luz (com Sol) e uma Main Camera desligada que so' o menu usa
    /// — a camera de verdade nasce com o Player. Tudo o mais nasce em runtime (Arkana.Main).
    /// Idempotente: recriada do zero a cada execucao (nada a "atualizar", nada duplica).
    /// </summary>
    public static class MainSceneBuilder
    {
        [MenuItem("Arkana/Montar cena Main")]
        public static void Build()
        {
            string path = Build_ScenePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("Main").AddComponent<Main>();   // Awake so' roda em Play: aqui e' um componente inerte

            var luz = new GameObject("Directional Light").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(30f, -28f, 0f);   // 30 graus como o Godot (Sol.Awake respeita rotacao ja' posta)
            luz.gameObject.AddComponent<Sol>();

            // NEVOA LIGADA NA CENA SALVA. Com Fog Modes em Automatic o build so' leva a variante de nevoa que alguma cena
            // usa; a Ilha liga a nevoa em runtime, tarde demais — sem isto o APK sairia sem nevoa nenhuma, inclusive na
            // emenda mar-ceu (achado da raia MUNDO-VISUAL, 11/09). A cor e as distancias de verdade vem da Ilha.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Arkana.Menu.Estilo.Noite;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            cam.enabled = false;   // Camera.main ignora camera desligada: na partida quem manda e' a do Player
            camGo.transform.position = new Vector3(0f, 3f, -5f);

            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("MainSceneBuilder: cena salva em " + path);
        }

        static string Build_ScenePath() => Arkana.EditorTools.Build.ScenePath;
    }
}
