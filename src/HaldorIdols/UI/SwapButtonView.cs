using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaldorIdols
{
    /// <summary>
    /// The "Swap" button in the shop footer. It is a clone of the native Sell
    /// button and takes its place in the Idols tab, where selling valuables is
    /// not the action the player is looking for.
    /// </summary>
    internal sealed class SwapButtonView
    {
        private readonly StoreGui _gui;
        private readonly GameObject _root;
        private readonly Button _button;
        private readonly TMP_Text _label;
        private readonly UITooltip _tooltip;

        internal bool IsAlive => _root != null;

        internal static SwapButtonView Create(StoreGui gui)
        {
            if (gui == null || gui.m_sellButton == null) return null;

            Button native = gui.m_sellButton;
            GameObject clone = Object.Instantiate(native.gameObject, native.transform.parent);
            clone.name = "HaldorIdols_Swap";

            // Take the native button's place in the footer, so a layout group
            // keeps the same order while Sell is hidden.
            clone.transform.SetSiblingIndex(native.transform.GetSiblingIndex());

            var view = new SwapButtonView(gui, clone);
            view.Initialize();
            return view;
        }

        private SwapButtonView(StoreGui gui, GameObject root)
        {
            _gui = gui;
            _root = root;
            _button = root.GetComponent<Button>();
            _label = root.GetComponentInChildren<TMP_Text>(true);
            _tooltip = root.GetComponent<UITooltip>() ?? root.GetComponentInChildren<UITooltip>(true);
        }

        private void Initialize()
        {
            if (_button != null)
            {
                // The scene wires Sell through a persistent onClick listener,
                // which RemoveAllListeners does not remove. Replacing the event
                // drops that listener, so clicking Swap never sells anything.
                _button.onClick = new Button.ButtonClickedEvent();
                _button.onClick.AddListener(OnClick);
            }

            if (_label != null)
                _label.text = TabsLocalization.Translate(TabsLocalization.SwapToken);

            _root.SetActive(false);
        }

        private void OnClick()
        {
            if (_gui == null) return;

            SwapService.SwapSelected(_gui);
            UpdateState();
        }

        internal void SetVisible(bool visible)
        {
            if (_root == null || _root.activeSelf == visible) return;

            _root.SetActive(visible);
        }

        internal void UpdateState()
        {
            if (_root == null) return;

            bool visible = ShopTabsController.UiAvailable
                           && ShopTabsController.ActiveTab == ShopTabsController.TabIdols
                           && SwapService.ExchangeEnabled;

            SetVisible(visible);
            if (!visible) return;

            // Keep the caption ours even if the native button carries a
            // component that re-applies the vanilla "Sell" text.
            string swapLabel = TabsLocalization.Translate(TabsLocalization.SwapToken);
            if (_label != null && _label.text != swapLabel)
                _label.text = swapLabel;

            bool canSwap = SwapService.CanSwap(_gui, out _, out _);

            if (_button != null)
                _button.interactable = canSwap;

            if (_tooltip != null)
                _tooltip.m_text = TabsLocalization.Translate(TabsLocalization.SwapActionToken);
        }

        internal void Dispose()
        {
            if (_root != null) Object.Destroy(_root);
        }
    }
}
