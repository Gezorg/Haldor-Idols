using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaldorIdols
{
    /// <summary>
    /// A small tab bar drawn above the trader window, built from the shop's own
    /// button sprites, colours and font so it looks native.
    /// </summary>
    internal sealed class ShopTabsView
    {
        private const float TabGap = 4f;
        private const float PanelGap = 8f;
        private const float ScreenMargin = 4f;
        private const float MinTabWidth = 110f;
        private const float MaxTabWidth = 220f;
        private const float MinTabHeight = 32f;
        private const float MaxTabHeight = 56f;

        private static readonly Color SelectedNormal = new Color(1f, 0.77f, 0.31f, 1f);
        private static readonly Color SelectedHighlight = new Color(1f, 0.88f, 0.50f, 1f);

        private readonly RectTransform _panel;
        private readonly RectTransform _host;
        private readonly Canvas _canvas;
        private readonly RectTransform _root;
        private readonly Button[] _buttons = new Button[ShopTabsController.TabCount];
        private readonly TMP_Text[] _labels = new TMP_Text[ShopTabsController.TabCount];
        private readonly ColorBlock _nativeColors;
        private readonly Vector2 _tabSize;
        private readonly Vector3[] _corners = new Vector3[4];

        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Vector2 _lastPanelSize;
        private bool _loggedLayout;

        internal bool IsAlive => _root != null;

        internal static ShopTabsView Create(StoreGui gui)
        {
            if (gui == null || gui.m_rootPanel == null)
                throw new InvalidOperationException("StoreGui root panel is missing.");

            RectTransform panel = gui.m_rootPanel.transform as RectTransform;
            if (panel == null)
                throw new InvalidOperationException("StoreGui root panel is not a RectTransform.");

            Button sell = gui.m_sellButton != null ? gui.m_sellButton : gui.m_buyButton;
            if (sell == null)
                throw new InvalidOperationException("StoreGui has no native button to copy.");

            RectTransform holder = sell.transform.parent as RectTransform;
            if (holder == null)
                throw new InvalidOperationException("Native button holder is missing.");

            return new ShopTabsView(gui, panel, sell, holder);
        }

        private ShopTabsView(StoreGui gui, RectTransform panel, Button nativeButton, RectTransform holder)
        {
            _panel = panel;
            _host = panel.parent as RectTransform ?? panel;
            _canvas = panel.GetComponentInParent<Canvas>();
            _nativeColors = nativeButton.colors;

            Rect holderRect = holder.rect;
            float width = holderRect.width > 1f ? holderRect.width : 90f;
            float height = holderRect.height > 1f ? holderRect.height : 36f;
            _tabSize = new Vector2(Mathf.Clamp(width, MinTabWidth, MaxTabWidth),
                                   Mathf.Clamp(height, MinTabHeight, MaxTabHeight));

            var rootGo = new GameObject("HaldorIdols_Tabs", typeof(RectTransform));
            _root = (RectTransform)rootGo.transform;
            _root.SetParent(_host, false);
            _root.SetAsLastSibling();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            _root.pivot = new Vector2(0.5f, 0f);
            _root.sizeDelta = new Vector2(TotalWidth(), _tabSize.y);

            TMP_Text textSource = ResolveTextSource(gui, nativeButton);
            Image sourceImage = holder.GetComponent<Image>() ?? nativeButton.GetComponent<Image>();
            ButtonSfx sfxSource = nativeButton.GetComponent<ButtonSfx>() ?? gui.m_buyButton?.GetComponent<ButtonSfx>();

            for (int i = 0; i < ShopTabsController.TabCount; i++)
                CreateTab(i, sourceImage, textSource, sfxSource);

            UpdateLayout();
        }

        private float TotalWidth()
        {
            return ShopTabsController.TabCount * _tabSize.x +
                   (ShopTabsController.TabCount - 1) * TabGap;
        }

        private void CreateTab(int index, Image sourceImage, TMP_Text textSource, ButtonSfx sfxSource)
        {
            var go = new GameObject("Tab_" + index, typeof(RectTransform), typeof(Image), typeof(Button));

            var rect = (RectTransform)go.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(index * (_tabSize.x + TabGap), 0f);
            rect.offsetMax = new Vector2(index * (_tabSize.x + TabGap) + _tabSize.x, 0f);

            var background = go.GetComponent<Image>();
            if (sourceImage != null)
                CopyImage(sourceImage, background);
            else
                background.color = new Color(0.16f, 0.14f, 0.11f, 1f);

            background.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.colors = _nativeColors;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            int tab = index;
            button.onClick.AddListener(() => ShopTabsController.Select(tab));
            _buttons[index] = button;

            if (sfxSource != null)
            {
                var sfx = go.AddComponent<ButtonSfx>();
                sfx.m_sfxPrefab = sfxSource.m_sfxPrefab;
                sfx.m_sfxPrefabVibrationOnly = sfxSource.m_sfxPrefabVibrationOnly;
                sfx.m_selectSfxPrefab = sfxSource.m_selectSfxPrefab;
                sfx.m_selectSfxPrefabVibrationOnly = sfxSource.m_selectSfxPrefabVibrationOnly;
                sfx.m_enterSfxPrefab = sfxSource.m_enterSfxPrefab;
                sfx.m_enterSfxPrefabVibrationOnly = sfxSource.m_enterSfxPrefabVibrationOnly;
            }

            _labels[index] = CreateLabel(rect, textSource);
        }

        private static TMP_Text CreateLabel(RectTransform parent, TMP_Text source)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.SetActive(false);

            var text = go.AddComponent<TextMeshProUGUI>();
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);

            if (source != null && source.font != null)
            {
                text.font = source.font;
                text.fontSharedMaterial = source.fontSharedMaterial;
            }

            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = source != null ? Mathf.Max(12f, source.fontSize) : 18f;
            text.raycastTarget = false;
            text.text = string.Empty;

            go.SetActive(true);
            return text;
        }

        private static TMP_Text ResolveTextSource(StoreGui gui, Button nativeButton)
        {
            if (gui.m_listElement != null)
            {
                Transform name = gui.m_listElement.transform.Find("name");
                if (name != null)
                {
                    TMP_Text text = name.GetComponent<TMP_Text>();
                    if (text != null) return text;
                }
            }

            if (gui.m_coinText != null) return gui.m_coinText;

            return nativeButton.GetComponentInChildren<TMP_Text>(true);
        }

        private static void CopyImage(Image source, Image target)
        {
            target.sprite = source.sprite;
            target.type = source.type;
            target.color = source.color;
            target.material = source.material;
            target.preserveAspect = source.preserveAspect;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            target.fillCenter = source.fillCenter;
            target.fillMethod = source.fillMethod;
            target.fillOrigin = source.fillOrigin;
            target.fillAmount = source.fillAmount;
            target.fillClockwise = source.fillClockwise;
            target.useSpriteMesh = source.useSpriteMesh;
        }

        internal void Refresh()
        {
            if (!IsAlive) return;

            int active = ShopTabsController.ActiveTab;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_labels[i] != null)
                    _labels[i].text = TabsLocalization.Translate(ShopTabsController.Token(i));

                ColorBlock tint = _nativeColors;
                if (i == active)
                {
                    tint.normalColor = SelectedNormal;
                    tint.highlightedColor = SelectedHighlight;
                    tint.selectedColor = SelectedNormal;
                    tint.pressedColor = SelectedHighlight;
                }

                _buttons[i].colors = tint;
            }

            UpdateLayout();
        }

        internal void Tick()
        {
            if (!IsAlive) return;

            // StoreGui.Update also runs while the window is hidden; world corners
            // of an inactive panel are not meaningful, so skip those frames.
            if (_panel == null || !_panel.gameObject.activeInHierarchy) return;

            // The shop panel is resized by the game when the UI scale, the
            // resolution or the inventory layout changes; the bar has to follow.
            Vector2 panelSize = _panel.rect.size;
            if (Screen.width == _lastScreenWidth &&
                Screen.height == _lastScreenHeight &&
                panelSize == _lastPanelSize)
            {
                return;
            }

            UpdateLayout();
        }

        internal void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        private void UpdateLayout()
        {
            if (!IsAlive || _panel == null || _host == null) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastPanelSize = _panel.rect.size;

            _root.sizeDelta = new Vector2(TotalWidth(), _tabSize.y);

            // Place the bar directly above the trader window, centred on it.
            _panel.GetWorldCorners(_corners);
            Vector3 topCenterWorld = (_corners[1] + _corners[2]) * 0.5f;
            Vector3 topCenterLocal = _host.InverseTransformPoint(topCenterWorld);

            float localGap = PanelGap / Mathf.Max(0.0001f, Mathf.Abs(_host.lossyScale.y));
            _root.anchoredPosition = new Vector2(
                topCenterLocal.x - _host.rect.xMin,
                topCenterLocal.y - _host.rect.yMin + localGap);

            if (!_loggedLayout)
            {
                _loggedLayout = true;
                Plugin.Log.LogInfo("Shop tabs placed: panel " + _panel.rect.width.ToString("0") + "x" +
                                   _panel.rect.height.ToString("0") + ", tab " + _tabSize.x.ToString("0") + "x" +
                                   _tabSize.y.ToString("0") + ", host '" + _host.name + "', pos " +
                                   _root.anchoredPosition);
            }

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            _root.GetWorldCorners(_corners);
            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);

            Vector2 shift = Vector2.zero;
            if (topRight.y > Screen.height - ScreenMargin)
                shift.y -= topRight.y - (Screen.height - ScreenMargin);
            if (bottomLeft.y < ScreenMargin)
                shift.y += ScreenMargin - bottomLeft.y;
            if (bottomLeft.x < ScreenMargin)
                shift.x += ScreenMargin - bottomLeft.x;
            if (topRight.x > Screen.width - ScreenMargin)
                shift.x -= topRight.x - (Screen.width - ScreenMargin);

            if (shift == Vector2.zero) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _host, bottomLeft, camera, out Vector2 before) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _host, bottomLeft + shift, camera, out Vector2 after))
            {
                _root.anchoredPosition += after - before;
            }
        }

        internal void Dispose()
        {
            if (_root == null) return;

            _root.gameObject.SetActive(false);
            Object.Destroy(_root.gameObject);
        }
    }
}
