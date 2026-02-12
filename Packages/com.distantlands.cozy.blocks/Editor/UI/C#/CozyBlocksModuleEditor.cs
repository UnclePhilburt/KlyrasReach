using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using DistantLands.Cozy.Data;

namespace DistantLands.Cozy.EditorScripts
{
    [CustomEditor(typeof(BlocksModule))]
    public class CozyBlocksModuleEditor : CozyBiomeModuleEditor
    {

        BlocksModule module;
        public override ModuleCategory Category => ModuleCategory.atmosphere;
        public override string ModuleTitle => "Blocks";
        public override string ModuleSubtitle => "Extended Atmosphere Module";
        public override string ModuleTooltip => "Manage your weather with more control.";

        public VisualElement SelectionContainer => root.Q<VisualElement>("selection-container");
        public VisualElement ProfileContainer => root.Q<VisualElement>("profile-container");
        public Button ClearButton => root.Q<Button>("clear-selection");
        public ObjectField TestBlock => root.Q<ObjectField>("test-block");


        Button widget;
        VisualElement root;

        void OnEnable()
        {
            if (!target)
                return;

            module = (BlocksModule)target;
        }

        public override Button DisplayWidget()
        {
            widget = SmallWidget();
            Label status = widget.Q<Label>("dynamic-status");
            status.style.fontSize = 8;

            if (module.currentBlock)
                status.text = module.currentBlock.name;
            else
                status.text = "";

            return widget;

        }

        public override VisualElement DisplayUI()
        {
            root = new VisualElement();

            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.distantlands.cozy.blocks/Editor/UI/UXML/blocks-module-editor.uxml"
            );

            asset.CloneTree(root);

            CozyProfileField<BlockProfile> blockProfile = new CozyProfileField<BlockProfile>(serializedObject.FindProperty("blockProfile"));
            SelectionContainer.Add(blockProfile);

            CozyProfileField<AtmosphereProfile> defaultSettings = new CozyProfileField<AtmosphereProfile>(serializedObject.FindProperty("defaultSettings"));
            SelectionContainer.Add(defaultSettings);

            TestBlock.BindProperty(serializedObject.FindProperty("testColorBlock"));
            ClearButton.RegisterCallback((ClickEvent evt) =>
            {
                module.testColorBlock = null;
            });

            InspectorElement inspector = new InspectorElement(module.blockProfile);
            inspector.AddToClassList("p-0");
            ProfileContainer.Add(inspector);

            return root;

        }

        public override VisualElement DisplayBiomeUI()
        {
            root = new VisualElement();

            Toggle useSingleBlock = new Toggle();
            useSingleBlock.BindProperty(serializedObject.FindProperty("useSingleBlock"));

            CozyProfileField<BlocksBlendable> singleBlock = new CozyProfileField<BlocksBlendable>(serializedObject.FindProperty("testColorBlock"));
            root.Add(singleBlock);

            CozyProfileField<BlockProfile> blockProfile = new CozyProfileField<BlockProfile>(serializedObject.FindProperty("blockProfile"));
            root.Add(blockProfile);

            useSingleBlock.RegisterValueChangedCallback((evt) =>
            {
                singleBlock.SetEnabled(evt.newValue);
                blockProfile.SetEnabled(!evt.newValue);
            });


            return root;

        }

        public override void OpenDocumentationURL()
        {
            Application.OpenURL("https://distant-lands.gitbook.io/cozy-stylized-weather-documentation/how-it-works/modules/blocks-module");
        }


    }
}