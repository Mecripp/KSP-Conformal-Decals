using System;
using UnityEngine;

namespace ConformalDecals.MaterialModifiers {
    public abstract class MaterialProperty {
        public string PropertyName { get; }

        protected readonly int _propertyID;


        protected MaterialProperty(ConfigNode node) {
            PropertyName = node.GetValue("name");

            if (PropertyName == null)
                throw new FormatException("name not found, cannot create material modifier");

            if (PropertyName == string.Empty)
                throw new FormatException("name is empty, cannot create material modifier");

            _propertyID = Shader.PropertyToID(PropertyName);
        }

        public abstract void Modify(Material material);

        private delegate bool TryParseDelegate<T>(string valueString, out T value);

        protected bool ParsePropertyBool(ConfigNode node, string valueName, bool isOptional = false, bool defaultValue = false) {
            return ParsePropertyValue(node, valueName, bool.TryParse, isOptional, defaultValue);
        }

        protected float ParsePropertyFloat(ConfigNode node, string valueName, bool isOptional = false, float defaultValue = 0.0f) {
            return ParsePropertyValue(node, valueName, float.TryParse, isOptional, defaultValue);
        }

        protected int ParsePropertyInt(ConfigNode node, string valueName, bool isOptional = false, int defaultValue = 0) {
            return ParsePropertyValue(node, valueName, int.TryParse, isOptional, defaultValue);
        }

        protected Color ParsePropertyColor(ConfigNode node, string valueName, bool isOptional = false, Color defaultValue = default) {
            return ParsePropertyValue(node, valueName, ParseExtensions.TryParseColor, isOptional, defaultValue);
        }

        protected Rect ParsePropertyRect(ConfigNode node, string valueName, bool isOptional = false, Rect defaultValue = default) {
            return ParsePropertyValue(node, valueName, ParseExtensions.TryParseRect, isOptional, defaultValue);
        }

        protected Vector2 ParsePropertyVector2(ConfigNode node, string valueName, bool isOptional = false, Vector2 defaultValue = default) {
            return ParsePropertyValue(node, valueName, ParseExtensions.TryParseVector2, isOptional, defaultValue);
        }

        private T ParsePropertyValue<T>(ConfigNode node, string valueName, TryParseDelegate<T> tryParse, bool isOptional = false, T defaultValue = default) {
            string valueString = node.GetValue(valueName);

            if (isOptional) {
                if (string.IsNullOrEmpty(valueString)) return defaultValue;
            }
            else {
                if (valueString == null)
                    throw new FormatException($"Missing {typeof(T)} value for {valueName} in property '{PropertyName}'");

                if (valueString == string.Empty)
                    throw new FormatException($"Empty {typeof(T)} value for {valueName} in property '{PropertyName}'");
            }

            if (tryParse(valueString, out var value)) {
                return value;
            }

            if (isOptional) {
                return defaultValue;
            }

            else {
                throw new FormatException($"Improperly formatted {typeof(T)} value for {valueName} in property '{PropertyName}' : '{valueString}");
            }
        }
    }
}