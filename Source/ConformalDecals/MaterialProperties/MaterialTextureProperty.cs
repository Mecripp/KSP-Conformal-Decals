using System;
using UnityEngine;

namespace ConformalDecals.MaterialProperties {
    public class MaterialTextureProperty : MaterialProperty {
        [SerializeField] public bool isNormal;
        [SerializeField] public bool isMain;
        [SerializeField] public bool autoScale;
        [SerializeField] public bool autoTile;

        [SerializeField] private string    _textureUrl;
        [SerializeField] private Texture2D _texture;

        [SerializeField] private bool _hasTile;
        [SerializeField] private Rect _tileRect;

        [SerializeField] private Vector2 _scale = Vector2.one;
        [SerializeField] private Vector2 _textureOffset;
        [SerializeField] private Vector2 _textureScale = Vector2.one;

        public Texture2D Texture => _texture;

        public string TextureUrl {
            get => _textureUrl;
            set {
                _texture = LoadTexture(value, isNormal);
                _textureUrl = value;
            }
        }

        public int Width => _texture.width;
        public int Height => _texture.height;

        public int MaskedWidth => _hasTile ? (int) _tileRect.width : _texture.width;
        public int MaskedHeight => _hasTile ? (int) _tileRect.height : _texture.height;

        public Vector2 Dimensions => new Vector2(_texture.width, _texture.height);
        public Vector2 MaskedDimensions => _hasTile ? _tileRect.size : Dimensions;

        public float AspectRatio => MaskedHeight / (float) MaskedWidth;

        public override void ParseNode(ConfigNode node) {
            base.ParseNode(node);

            isNormal = ParsePropertyBool(node, "isNormalMap", true, (PropertyName == "_BumpMap") || (PropertyName == "_DecalBumpMap") || isNormal);
            isMain = ParsePropertyBool(node, "isMain", true, isMain);
            autoScale = ParsePropertyBool(node, "autoScale", true, autoScale);
            autoTile = ParsePropertyBool(node, "autoTile", true, autoTile);

            var textureUrl = node.GetValue("textureUrl");

            if (string.IsNullOrEmpty(textureUrl)) {
                if (string.IsNullOrEmpty(_textureUrl)) {
                    TextureUrl = "";
                }
            }
            else {
                TextureUrl = node.GetValue("textureUrl");
            }

            if (node.HasValue("tile") && !autoTile) {
                SetTile(ParsePropertyRect(node, "tile", true, _tileRect));
            }
        }

        public override void Modify(Material material) {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (_texture == null) {
                _texture = Texture2D.whiteTexture;
                throw new NullReferenceException("texture is null, but should not be");
            }

            material.SetTexture(_propertyID, _texture);
            material.SetTextureOffset(_propertyID, _textureOffset);
            material.SetTextureScale(_propertyID, _textureScale * _scale);
        }

        public void SetScale(Vector2 scale) {
            _scale = scale;
        }

        public void SetTile(Rect tile) {
            SetTile(tile, Dimensions);
        }

        public void SetTile(Rect tile, Vector2 mainTexDimensions) {
            var scale = tile.size;
            var offset = tile.position;

            // invert y axis to deal with DXT image orientation
            offset.y = mainTexDimensions.y - offset.y - tile.height;

            // fit to given dimensions
            scale /= mainTexDimensions;
            offset /= mainTexDimensions;
            _tileRect = tile;
            _hasTile = true;
            _textureScale = scale;
            _textureOffset = offset;
        }

        private static Texture2D LoadTexture(string textureUrl, bool isNormal) {
            Debug.Log($"loading texture '{textureUrl}', isNormalMap = {isNormal}");
            if ((string.IsNullOrEmpty(textureUrl) && isNormal) || textureUrl == "Bump") {
                return Texture2D.normalTexture;
            }

            if ((string.IsNullOrEmpty(textureUrl) && !isNormal) || textureUrl == "White") {
                return Texture2D.whiteTexture;
            }

            if (textureUrl == "Black") {
                return Texture2D.blackTexture;
            }

            var texture = GameDatabase.Instance.GetTexture(textureUrl, isNormal);
            if (texture == null) throw new Exception($"Cannot get texture '{textureUrl}', isNormalMap = {isNormal}");
            return texture;
        }
    }
}