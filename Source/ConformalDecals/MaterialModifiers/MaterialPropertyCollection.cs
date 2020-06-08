using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UniLinq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ConformalDecals.MaterialModifiers {
    public class MaterialPropertyCollection : ScriptableObject, ISerializationCallbackReceiver {
        [SerializeField] private Shader                  _shader;
        [SerializeField] private MaterialTextureProperty _mainTexture;
        [SerializeField] private string[]                _serializedNames;
        [SerializeField] private MaterialProperty[]      _serializedProperties;

        private Dictionary<string, MaterialProperty> _materialProperties;

        private Material _decalMaterial;
        private Material _previewMaterial;

        public Shader DecalShader => _shader;

        public Material DecalMaterial {
            get {
                if (_decalMaterial == null) {
                    _decalMaterial = new Material(_shader);

                    _decalMaterial.SetInt(DecalPropertyIDs._Cull, (int) CullMode.Off);
                }

                return _decalMaterial;
            }
        }

        public Material PreviewMaterial {
            get {
                if (_previewMaterial == null) {
                    _previewMaterial = new Material(_shader);

                    _previewMaterial.EnableKeyword("DECAL_PREVIEW");
                    _previewMaterial.SetInt(DecalPropertyIDs._Cull, (int) CullMode.Back);
                }

                return _previewMaterial;
            }
        }

        public MaterialTextureProperty MainTexture {
            get => _mainTexture;
            set {
                if (!_materialProperties.ContainsValue(value))
                    throw new ArgumentException($"Texture property {value.name} is not part of this property collection.");

                _mainTexture = value;
            }
        }

        public float AspectRatio => MainTexture == null ? 1 : MainTexture.AspectRatio;

        public void OnBeforeSerialize() {
            Debug.Log($"Serializing MaterialPropertyCollection {this.GetInstanceID()}");
            if (_materialProperties == null) throw new SerializationException("Tried to serialize an uninitialized MaterialPropertyCollection");

            _serializedNames = _materialProperties.Keys.ToArray();
            _serializedProperties = _materialProperties.Values.ToArray();
        }

        public void OnAfterDeserialize() {
            Debug.Log($"Deserializing MaterialPropertyCollection {this.GetInstanceID()}");
            if (_serializedNames == null) throw new SerializationException("ID array is null");
            if (_serializedProperties == null) throw new SerializationException("Property array is null");
            if (_serializedProperties.Length != _serializedNames.Length) throw new SerializationException("Material property arrays are different lengths.");

            _materialProperties ??= new Dictionary<string, MaterialProperty>();

            for (var i = 0; i < _serializedNames.Length; i++) {
                var property = MaterialProperty.Instantiate(_serializedProperties[i]);
                _materialProperties.Add(_serializedNames[i], property);

                if (property is MaterialTextureProperty textureProperty && textureProperty.isMain) {
                    _mainTexture = textureProperty;
                }
            }
        }

        public void Awake() {
            Debug.Log($"MaterialPropertyCollection {this.GetInstanceID()} onAwake");
            _materialProperties ??= new Dictionary<string, MaterialProperty>();
        }

        public void OnDestroy() {
            if (_decalMaterial != null) Destroy(_decalMaterial);
            if (_previewMaterial != null) Destroy(_previewMaterial);

            foreach (var entry in _materialProperties) {
                Destroy(entry.Value);
            }
        }

        public void AddProperty(MaterialProperty property) {
            if (property == null) throw new ArgumentNullException(nameof(property));

            _materialProperties.Add(property.name, property);

            if (property is MaterialTextureProperty textureProperty) {
                if (textureProperty.isMain) _mainTexture = textureProperty;
            }
        }

        public T AddProperty<T>(string propertyName) where T : MaterialProperty {
            if (_materialProperties.ContainsKey(propertyName)) throw new ArgumentException("property with that name already exists!");
            var newProperty = MaterialProperty.CreateInstance<T>();
            newProperty.PropertyName = propertyName;
            _materialProperties.Add(propertyName, newProperty);

            return newProperty;
        }

        public T GetProperty<T>(string propertyName) where T : MaterialProperty {
            if (_materialProperties.ContainsKey(propertyName) && _materialProperties[propertyName] is T property) {
                return property;
            }
            else {
                return null;
            }
        }

        public T AddOrGetProperty<T>(string propertyName) where T : MaterialProperty {
            if (_materialProperties.ContainsKey(propertyName) && _materialProperties[propertyName] is T property) {
                return property;
            }
            else {
                return AddProperty<T>(propertyName);
            }
        }

        public MaterialTextureProperty AddTextureProperty(string propertyName, bool isMain = false) {
            var newProperty = AddProperty<MaterialTextureProperty>(propertyName);
            if (isMain) _mainTexture = newProperty;

            return newProperty;
        }

        public MaterialTextureProperty GetTextureProperty(string propertyName) {
            return GetProperty<MaterialTextureProperty>(propertyName);
        }

        public MaterialTextureProperty AddOrGetTextureProperty(string propertyName, bool isMain = false) {
            var newProperty = AddOrGetProperty<MaterialTextureProperty>(propertyName);
            if (isMain) _mainTexture = newProperty;

            return newProperty;
        }

        public T ParseProperty<T>(ConfigNode node) where T : MaterialProperty {
            var propertyName = node.GetValue("name");
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("node has no name");

            var newProperty = AddOrGetProperty<T>(propertyName);
            newProperty.ParseNode(node);

            if (newProperty is MaterialTextureProperty textureProperty && textureProperty.isMain) {
                _mainTexture = textureProperty;
            }

            return newProperty;
        }

        public void SetShader(string shaderName) {
            if (string.IsNullOrEmpty(shaderName)) {
                if (_shader == null) {
                    Debug.Log("Using default decal shader");
                    shaderName = "ConformalDecals/Paint/Diffuse";
                }
                else {
                    return;
                }
            }

            var shader = Shabby.Shabby.FindShader(shaderName);

            if (shader == null) throw new FormatException($"Unable to find specified shader '{shaderName}'");

            _shader = shader;
            _decalMaterial = null;
            _previewMaterial = null;
        }

        public void SetRenderQueue(int queue) {
            DecalMaterial.renderQueue = queue;
        }

        public void UpdateScale(Vector2 scale) {
            foreach (var entry in _materialProperties) {
                if (entry.Value is MaterialTextureProperty textureProperty && textureProperty.autoScale) {
                    textureProperty.SetScale(scale);
                }
            }
        }

        public void UpdateTile(Rect tile) {
            if (_mainTexture == null) throw new InvalidOperationException("UpdateTile called but no main texture is specified!");
            var mainTexSize = _mainTexture.Dimensions;

            Debug.Log($"Main texture is {_mainTexture.PropertyName} and its size is {mainTexSize}");

            foreach (var entry in _materialProperties) {
                if (entry.Value is MaterialTextureProperty textureProperty && textureProperty.autoTile) {
                    textureProperty.SetTile(tile, mainTexSize);
                }
            }
        }

        public void UpdateTile(int index, Vector2 tileSize) {
            int tileCountX = (int) (_mainTexture.Width / tileSize.x);

            int x = index % tileCountX;
            int y = index / tileCountX;

            var tile = new Rect(x * tileSize.x, y * tileSize.y, tileSize.x, tileSize.y);

            UpdateTile(tile);
        }

        public void SetOpacity(float opacity) {
            DecalMaterial.SetFloat(DecalPropertyIDs._DecalOpacity, opacity);
            PreviewMaterial.SetFloat(DecalPropertyIDs._DecalOpacity, opacity);
        }

        public void SetCutoff(float cutoff) {
            DecalMaterial.SetFloat(DecalPropertyIDs._Cutoff, cutoff);
            PreviewMaterial.SetFloat(DecalPropertyIDs._Cutoff, cutoff);
        }

        public void UpdateMaterials() {
            UpdateMaterial(DecalMaterial);
            UpdateMaterial(PreviewMaterial);
        }

        public void UpdateMaterial(Material material) {
            if (material == null) throw new ArgumentNullException(nameof(material));

            foreach (var entry in _materialProperties) {
                entry.Value.Modify(material);
            }
        }
    }
}