using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MonetGarden
{
    public class GardenExperience : MonoBehaviour
    {
        public enum ExperienceState { Selection, Exploration, Transition, Painting }
        public GardenDesktopController player;
        public GardenViewpoint viewpoint;
        public GardenArtwork[] artworks;
        public GameObject dynamicEnvironment;
        public RenderPipelineAsset prototypePipeline;
        public Shader painterlyShader;
        public float transitionSeconds = 8f;
        public float explorationDistance = 0.4f;
        public bool requireConfirmation = true;
        public ExperienceState State { get; private set; } = ExperienceState.Selection;
        public float Progress { get; private set; }
        public bool HasExplored { get; private set; }
        public GardenArtwork SelectedArtwork { get; private set; }
        public static GardenExperience Active { get; private set; }
        public bool EffectRendered { get; set; }
        RenderPipelineAsset previousPipeline;
        bool ownsPipeline;
        Texture2D globe;
        GUIStyle titleStyle, bodyStyle, smallStyle;

        void Start()
        {
            if (!player || !viewpoint) { Debug.LogError("Monet: missing player/viewpoint references.", this); enabled = false; return; }
            Active = this;
            previousPipeline = QualitySettings.renderPipeline;
            if (prototypePipeline) { QualitySettings.renderPipeline = prototypePipeline; ownsPipeline = true; }
            player.SetInput(false);
            if (artworks != null && artworks.Length > 0) SelectedArtwork = artworks[0];
            globe = GardenGlobe.CreateTexture(320);
        }

        public void EnterGarden(int index = 0)
        {
            if (artworks == null || index < 0 || index >= artworks.Length || !artworks[index]) return;
            SelectedArtwork = artworks[index];
            if (!string.IsNullOrEmpty(SelectedArtwork.scenePath) &&
                SelectedArtwork.scenePath != SceneManager.GetActiveScene().path)
            {
                if (Application.CanStreamedLevelBeLoaded(SelectedArtwork.scenePath)) SceneManager.LoadScene(SelectedArtwork.scenePath);
                else Debug.LogWarning("Monet: this catalogue scene is not included in Build Settings.", this);
                return;
            }
            ResetExperience(false);
        }

        public void ResetExperience(bool returnToMap)
        {
            Progress = 0; HasExplored = false; EffectRendered = false;
            viewpoint.ResetDetection();
            player.ResetToSpawn();
            State = returnToMap ? ExperienceState.Selection : ExperienceState.Exploration;
            player.SetInput(!returnToMap);
            if (dynamicEnvironment)
            {
                dynamicEnvironment.SetActive(true);
                foreach (var motion in dynamicEnvironment.GetComponentsInChildren<GardenAmbientMotion>()) motion.ResetMotion();
                foreach (var actor in dynamicEnvironment.GetComponentsInChildren<GardenActorPath>()) actor.ResetPath();
            }
        }

        public bool BeginTransition()
        {
            if (State != ExperienceState.Exploration || !HasExplored || !viewpoint.Ready) return false;
            State = ExperienceState.Transition;
            player.SetInput(false); // Keep the view chosen by the visitor; never teleport or force camera rotation.
            Progress = 0;
            return true;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetExperience(false);
            if (Input.GetKeyDown(KeyCode.M)) ResetExperience(true);
            if (Input.GetKeyDown(KeyCode.G) && dynamicEnvironment && State != ExperienceState.Selection)
                dynamicEnvironment.SetActive(!dynamicEnvironment.activeSelf);
            if (State == ExperienceState.Selection && Input.GetKeyDown(KeyCode.Return)) EnterGarden();
            if (State == ExperienceState.Exploration)
            {
                var delta = player.transform.position - player.SpawnPosition;
                if (new Vector2(delta.x, delta.z).magnitude >= explorationDistance) HasExplored = true;
                viewpoint.Tick(player.viewCamera, Time.deltaTime, HasExplored);
                if (viewpoint.Ready && (!requireConfirmation || Input.GetKeyDown(KeyCode.E))) BeginTransition();
            }
            else if (State == ExperienceState.Transition)
            {
                Progress = Mathf.Clamp01(Progress + Time.deltaTime / Mathf.Max(0.5f, transitionSeconds));
                if (Progress >= 1) State = ExperienceState.Painting;
            }
        }

        void CreateStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, wordWrap = true };
            titleStyle.normal.textColor = new Color(0.94f, 0.91f, 0.8f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            bodyStyle.normal.textColor = new Color(0.85f, 0.88f, 0.85f);
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 12 };
            smallStyle.normal.textColor = new Color(0.65f, 0.72f, 0.68f);
        }

        void OnGUI()
        {
            if (!player) return;
            CreateStyles();
            // Work in a reference-sized canvas so controls fit a small Game view as well as a desktop display.
            Matrix4x4 saved = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            if (scale <= 0) return;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            if (State == ExperienceState.Selection)
            {
                GUI.color = new Color(0.05f, 0.11f, 0.09f, 0.94f);
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(84, 70, 900, 54), "MONET GARDEN", titleStyle);
                GUI.Label(new Rect(86, 127, 900, 36), "From viewing a painting to entering the painting", bodyStyle);
                if (globe) GUI.DrawTexture(new Rect(86, 224, 360, 360), globe, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(116, 594, 310, 25), "SCHEMATIC WORLD / GIVERNY 49.075 N", smallStyle);
                GUI.Label(new Rect(515, 234, 550, 45), "01  /  GIVERNY, FRANCE", titleStyle);
                GUI.Label(new Rect(517, 294, 540, 110), "Walk through a speculative reconstruction of Monet's water garden. Find a composition, hold your gaze, then transform the view into painterly colour and brush-like marks.", bodyStyle);
                GUI.Label(new Rect(517, 417, 560, 60), "The reconstruction and viewpoint are interpretive. Original artwork: " +
                    (SelectedArtwork && SelectedArtwork.HasDocumentedOriginal ? SelectedArtwork.title + " (" + SelectedArtwork.year + ")" : "awaiting a selected image and source."), smallStyle);
                if (GUI.Button(new Rect(517, 516, 326, 55), "ENTER GIVERNY  /  Enter")) EnterGarden();
                GUI.Label(new Rect(517, 588, 550, 28), "One active destination. Catalogue supports additional scenes.", smallStyle);
            }
            else
            {
                GUI.color = new Color(0.04f, 0.1f, 0.08f, 0.8f);
                GUI.DrawTexture(new Rect(26, 24, 385, 188), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(44, 36, 365, 36), "MONET / " + State.ToString().ToUpperInvariant(), bodyStyle);
                string hint;
                if (State == ExperienceState.Exploration)
                    hint = !HasExplored ? "Explore the path, then return to the starting composition." :
                        viewpoint.Ready ? "Composition found. Press E to transform." :
                        $"Find the golden viewpoint\nDistance {viewpoint.Distance:F2} / {viewpoint.radius:F2}\nAngle {viewpoint.YawError:F1} deg / Hold {viewpoint.HoldProgress * 100:F0}%";
                else if (State == ExperienceState.Transition) hint = $"Colour -> softened detail -> brush-like marks\nTransition {Progress * 100:F0}%";
                else hint = SelectedArtwork && SelectedArtwork.HasDocumentedOriginal ? SelectedArtwork.title + " / " + SelectedArtwork.year :
                    "Painterly study complete.\nOriginal painting image is still required.";
                GUI.Label(new Rect(44, 79, 347, 91), hint, bodyStyle);
                GUI.Label(new Rect(44, 179, 348, 23), "Interpretive reconstruction / proposed viewpoint", smallStyle);
                if (State == ExperienceState.Exploration && HasExplored)
                {
                    Vector3 target = player.viewCamera.WorldToScreenPoint(viewpoint.transform.position - Vector3.up * 0.75f);
                    Vector2 p = new Vector2((target.x - (Screen.width - 1280 * scale) / 2) / scale,
                        (Screen.height - target.y - (Screen.height - 720 * scale) / 2) / scale);
                    if (target.z > 0 && p.x > 0 && p.x < 1280 && p.y > 0 && p.y < 680)
                    { GUI.color = new Color(1, 0.82f, 0.4f); GUI.Box(new Rect(p.x - 18, p.y - 18, 36, 36), "+"); GUI.color = Color.white; }
                }
                GUI.Label(new Rect(26, 680, 1040, 28), "WASD walk / Shift run / click look / Esc release / E transform / R reset / M map / G motion", smallStyle);
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    if (GUI.Button(new Rect(1096, 24, 155, 40), "RESET / R")) ResetExperience(false);
                    if (GUI.Button(new Rect(1096, 72, 155, 40), "WORLD / M")) ResetExperience(true);
                }
                if (State == ExperienceState.Painting && !EffectRendered)
                    GUI.Label(new Rect(430, 28, 600, 40), "Painterly renderer unavailable: check the prototype URP renderer.", bodyStyle);
            }
            GUI.matrix = saved;
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (ownsPipeline && QualitySettings.renderPipeline == prototypePipeline) QualitySettings.renderPipeline = previousPipeline;
            if (globe) Destroy(globe);
            if (player) player.ReleaseCursor();
        }
    }
}
