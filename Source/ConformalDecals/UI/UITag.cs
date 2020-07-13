using UnityEngine;

namespace ConformalDecals.UI {
    public class UITag : MonoBehaviour {
        public enum UIType {
            None,
            Window,
            Box,
            Button,
            ButtonToggle,
            Slider,
            Dropdown,
            Label,
            Header
        }

        [SerializeField] public UIType type = UIType.None;
    }
}