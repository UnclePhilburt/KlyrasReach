using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using DistantLands.Cozy.Data;
using System;

namespace DistantLands.Cozy.EditorScripts
{
    [CustomEditor(typeof(CozyHorizonModule))]
    public class CozyHorizonModuleEditor : CozyModuleEditor
    {

        CozyHorizonModule module;
        public override ModuleCategory Category => ModuleCategory.atmosphere;
        public override string ModuleTitle => "Horizon";
        public override string ModuleSubtitle => "Sky Layers Module";
        public override string ModuleTooltip => "Add textures, cubemaps, and panoramas to your skybox.";

        Button widget;
        VisualElement root;

        PropertyField layersField => root.Q<PropertyField>("horizonProfile");
        VisualElement settingsContainer => root.Q<VisualElement>("settings-container");

        void OnEnable()
        {
            if (!target)
                return;

            module = (CozyHorizonModule)target;
        }

        public override Button DisplayWidget()
        {
            widget = SmallWidget();
            Label status = widget.Q<Label>("dynamic-status");
            status.style.fontSize = 8;

            if (module.horizonProfile)
                status.text = $"{module.horizonProfile.name}";
            else
                status.text = "No Profile Set";

            return widget;

        }

        public override VisualElement DisplayUI()
        {
            root = new VisualElement();

            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.distantlands.cozy.horizon/Editor/UI/UXML/horizon-module-editor.uxml"
            );

            root.Bind(serializedObject);

            asset.CloneTree(root);

            layersField.RegisterValueChangeCallback(x =>
            {
                module.UpdateSkyLayers();
            });

            List<PropertyField> properties = settingsContainer.Query<PropertyField>().ToList();

            foreach (PropertyField property in properties)
            {
                property.RegisterValueChangeCallback(x =>
                {
                    module.UpdateSkyLayers();
                });
            }

            return root;

        }

        public override void OpenDocumentationURL()
        {
            Application.OpenURL("https://distant-lands.gitbook.io/cozy-stylized-weather-documentation/how-it-works/modules/horizon-module");
        }


    }
}