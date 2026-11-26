using GravityLab.Audio;
using GravityLab.Gadgets;
using GravityLab.Player;
using GravityLab.Rendering;
using GravityLab.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace GravityLab.Core
{
    /// <summary>
    /// The only object in the scene. Builds everything at runtime: settings, lighting, audio, player with gravity gun,
    /// menu camera, UI and the game manager.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            SaveSystem.Load();
            Time.fixedDeltaTime = 1f / 60f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 2;
            Layers.Configure();

            Tweener.Init(gameObject);
            MatLib.Init();
            UISprites.Init();
            gameObject.AddComponent<AudioListener>();
            gameObject.AddComponent<AudioManager>();
            DisplaySettings.ApplyAll();
            SetupLighting();
            FxManager.Init(transform);

            PlayerController player = CreatePlayer();
            GunViewModel viewModel = GunViewModel.Create(player.Camera.transform);
            GravityGun gun = player.gameObject.AddComponent<GravityGun>();
            gun.Init(player, viewModel);
            viewModel.Bind(gun, player);

            MenuCamera menuCamera = CreateCamera("MenuCamera").gameObject.AddComponent<MenuCamera>();

            GameManager game = gameObject.AddComponent<GameManager>();
            UIManager ui = UIManager.Create(game, transform);
            game.Init(player, gun, menuCamera, ui);
        }

        static PlayerController CreatePlayer()
        {
            GameObject go = new GameObject("Player");
            go.AddComponent<CharacterController>();
            PlayerController player = go.AddComponent<PlayerController>();
            player.Init(CreateCamera("PlayerCamera"));
            go.SetActive(false);
            return player;
        }

        static Camera CreateCamera(string name)
        {
            GameObject go = new GameObject(name);
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = SaveSystem.Data.FieldOfView;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            return cam;
        }

        static void SetupLighting()
        {
            GameObject sun = new GameObject("KeyLight");
            sun.transform.rotation = Quaternion.Euler(62f, -35f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.93f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.35f;
            RenderSettings.sun = light;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.74f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.6f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.44f, 0.49f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.83f, 0.89f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 120f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
