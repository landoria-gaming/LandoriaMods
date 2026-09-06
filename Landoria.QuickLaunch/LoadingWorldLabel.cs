using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Landoria.QuickLaunch
{
    [HarmonyPatch(typeof(Hud), "UpdateBlackScreen")]
    internal static class LoadingWorldLabel
    {
        private const string LabelName = "QuickLaunchWorldName";
        private const float DisplaySeconds = 6f;
        private static TMP_Text _label;
        private static Hud _hud;
        private static bool _initialLoadingFinished;
        private static float? _firstShownAt;

        private static void Postfix(Hud __instance, Player player)
        {
            ZNet network = ZNet.instance;
            if (_hud != __instance)
            {
                _hud = __instance;
                _initialLoadingFinished = false;
                _firstShownAt = null;
                _label = null;
            }
            bool loading = __instance.m_loadingScreen.gameObject.activeInHierarchy;
            if (player != null && !loading) _initialLoadingFinished = true;
            bool visible = QuickLaunchPlugin.IsAutomaticLoading && !_initialLoadingFinished &&
                (!_firstShownAt.HasValue || Time.realtimeSinceStartup - _firstShownAt.Value < DisplaySeconds) &&
                network != null && Game.instance != null &&
                !Game.instance.IsShuttingDown() &&
                loading;
            string worldName = null;
            if (visible)
            {
                worldName = network.IsServer()
                    ? network.GetWorldName() : QuickLaunchPlugin.ConnectingServerName;
            }
            if (string.IsNullOrWhiteSpace(worldName))
            {
                if (_label != null) _label.gameObject.SetActive(false);
                return;
            }

            if (_label == null) _label = CreateLabel(__instance);
            if (_label == null) return;
            if (!_firstShownAt.HasValue) _firstShownAt = Time.realtimeSinceStartup;
            string text = GetConnectionText(worldName);
            if (_label.text != text) _label.text = text;
            _label.gameObject.SetActive(true);
        }

        private static string GetConnectionText(string destination)
        {
            string characterName = Game.instance.GetPlayerProfile()?.GetName();
            return string.IsNullOrWhiteSpace(characterName)
                ? "Connecting to " + destination
                : $"Connecting to {destination} with {characterName}";
        }

        private static TMP_Text CreateLabel(Hud hud)
        {
            TMP_Text loadingText = hud.m_loadingProgress.GetComponentInChildren<TMP_Text>(true);
            if (loadingText == null) return null;
            var labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(hud.m_loadingScreen.transform, false);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.font = loadingText.font;
            label.fontSharedMaterial = loadingText.fontSharedMaterial;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Normal;
            label.color = new Color(1f, 1f, 1f, 0.4f);
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.richText = false;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(500f, 48f);
            return label;
        }
    }
}
