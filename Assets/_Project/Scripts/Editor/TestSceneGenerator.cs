#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BarrelRacing.Runtime.Race;

namespace BarrelRacing.Editor
{
    public static class TestSceneGenerator
    {
        [MenuItem("Barrel Rivals/Setup Playable Test Scene")]
        public static void GenerateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & Environment
            GameObject sun = new GameObject("Directional Light");
            Light lightComponent = sun.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Arena Dirt Floor (60m x 80m)
            GameObject arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
            arena.name = "Arena_DirtFloor";
            arena.transform.position = Vector3.zero;
            arena.transform.localScale = new Vector3(6f, 1f, 8f);
            
            Material dirtMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            dirtMat.color = new Color(0.55f, 0.38f, 0.22f); // Warm Rodeo Dirt
            arena.GetComponent<Renderer>().sharedMaterial = dirtMat;

            // 3. WPRA Regulation Barrels (3 Barrels)
            Vector3[] barrelPositions = new Vector3[]
            {
                new Vector3(-8.84f, 1.0f, 18.28f), // Barrel 1 (Left)
                new Vector3(8.84f, 1.0f, 18.28f),  // Barrel 2 (Right)
                new Vector3(0.0f, 1.0f, 32.0f)     // Barrel 3 (Top/Center)
            };

            Color[] barrelColors = new Color[] { Color.red, Color.blue, Color.yellow };

            for (int i = 0; i < 3; i++)
            {
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = $"Barrel_{i + 1}";
                barrel.transform.position = barrelPositions[i];
                barrel.transform.localScale = new Vector3(0.9f, 1.0f, 0.9f);

                Material barrelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                barrelMat.color = barrelColors[i];
                barrel.GetComponent<Renderer>().sharedMaterial = barrelMat;
            }

            // 4. Start / Finish Gate Markers
            GameObject startGateL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            startGateL.name = "Gate_Post_Left";
            startGateL.transform.position = new Vector3(-6f, 1.5f, 0f);
            startGateL.transform.localScale = new Vector3(0.3f, 3f, 0.3f);

            GameObject startGateR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            startGateR.name = "Gate_Post_Right";
            startGateR.transform.position = new Vector3(6f, 1.5f, 0f);
            startGateR.transform.localScale = new Vector3(0.3f, 3f, 0.3f);

            // 5. Horse & Rider Player GameObject
            GameObject horse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horse.name = "Horse_Player";
            horse.transform.position = new Vector3(0f, 1.0f, -5f);
            horse.transform.rotation = Quaternion.identity;

            Material horseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            horseMat.color = new Color(0.25f, 0.15f, 0.08f); // Chestnut Coat
            horse.GetComponent<Renderer>().sharedMaterial = horseMat;

            // Attach Runtime Simulation Components
            horse.AddComponent<InputManager>();
            var anim = horse.AddComponent<HorseAnimationSM>();
            var audioDir = horse.AddComponent<AudioDirector>();
            var vfxDir = horse.AddComponent<VFXDirector>();

            // 6. Camera Director Setup
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.fieldOfView = 68f;
            cameraObj.AddComponent<AudioListener>();
            var camDirector = cameraObj.AddComponent<CameraDirector>();

            // 7. UI Canvas & HUD
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Timer Text
            GameObject timerObj = new GameObject("HUD_Timer");
            timerObj.transform.SetParent(canvasObj.transform, false);
            Text timerText = timerObj.AddComponent<Text>();
            timerText.font = defaultFont;
            timerText.fontSize = 36;
            timerText.alignment = TextAnchor.UpperCenter;
            timerText.color = Color.white;
            timerText.text = "0.00s";
            RectTransform timerRect = timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0, -20);
            timerRect.sizeDelta = new Vector2(300, 50);

            // Round Info Text
            GameObject roundObj = new GameObject("HUD_Round");
            roundObj.transform.SetParent(canvasObj.transform, false);
            Text roundText = roundObj.AddComponent<Text>();
            roundText.font = defaultFont;
            roundText.fontSize = 24;
            roundText.alignment = TextAnchor.UpperLeft;
            roundText.color = Color.yellow;
            roundText.text = "ROUND 1 / 3";
            RectTransform roundRect = roundText.rectTransform;
            roundRect.anchorMin = new Vector2(0f, 1f);
            roundRect.anchorMax = new Vector2(0f, 1f);
            roundRect.pivot = new Vector2(0f, 1f);
            roundRect.anchoredPosition = new Vector2(20, -20);
            roundRect.sizeDelta = new Vector2(300, 40);

            // Penalty Text
            GameObject penaltyObj = new GameObject("HUD_Penalty");
            penaltyObj.transform.SetParent(canvasObj.transform, false);
            Text penaltyText = penaltyObj.AddComponent<Text>();
            penaltyText.font = defaultFont;
            penaltyText.fontSize = 22;
            penaltyText.alignment = TextAnchor.UpperRight;
            penaltyText.color = Color.green;
            penaltyText.text = "NO PENALTIES";
            RectTransform penaltyRect = penaltyText.rectTransform;
            penaltyRect.anchorMin = new Vector2(1f, 1f);
            penaltyRect.anchorMax = new Vector2(1f, 1f);
            penaltyRect.pivot = new Vector2(1f, 1f);
            penaltyRect.anchoredPosition = new Vector2(-20, -20);
            penaltyRect.sizeDelta = new Vector2(300, 40);

            // Speedometer Text
            GameObject speedObj = new GameObject("HUD_Speed");
            speedObj.transform.SetParent(canvasObj.transform, false);
            Text speedText = speedObj.AddComponent<Text>();
            speedText.font = defaultFont;
            speedText.fontSize = 28;
            speedText.alignment = TextAnchor.LowerRight;
            speedText.color = Color.cyan;
            speedText.text = "0 MPH";
            RectTransform speedRect = speedText.rectTransform;
            speedRect.anchorMin = new Vector2(1f, 0f);
            speedRect.anchorMax = new Vector2(1f, 0f);
            speedRect.pivot = new Vector2(1f, 0f);
            speedRect.anchoredPosition = new Vector2(-20, 20);
            speedRect.sizeDelta = new Vector2(200, 40);

            // Attach RaceHUD
            GameObject hudObj = new GameObject("RaceHUD_Manager");
            hudObj.transform.SetParent(canvasObj.transform, false);
            RaceHUD raceHud = hudObj.AddComponent<RaceHUD>();

            // 8. Match Orchestrator
            GameObject managerObj = new GameObject("MatchOrchestrator");
            managerObj.AddComponent<MatchOrchestrator>();
            managerObj.AddComponent<MatchFlowController>();

            // Save Scene
            string scenePath = "Assets/_Project/Scenes/Arena_TestTrack.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.OpenScene(scenePath);

            Selection.activeGameObject = horse;
            Debug.Log($"<color=green>SUCCESS:</color> Generated 3D Test Scene at {scenePath}");
        }
    }
}
#endif
