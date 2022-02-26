using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReferenceEditor
{
    public static class Utilities
    {
        internal static Image CreateImage(string imagePath)
        {
            return new Image
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(imagePath),
                scaleMode = ScaleMode.ScaleToFit
            };
        }
        
        internal static Label CreateLabel(string text)
        {
            var label = new Label
            {
                focusable = false,
                text = text,
            };
        
            return label;
        }
        
        internal static TextField CreateTextField(string label)
        {
            var field = new TextField
            {
                multiline = false,
                label = label
            };
        
            return field;
        }
    
        internal static Button CreateButton(string buttonText, Action onClicked)
        {
            var button = new Button
            {
                text = buttonText,
            };

            button.clicked += onClicked;
            return button;
        }
    }
}