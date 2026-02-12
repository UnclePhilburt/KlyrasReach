using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using DistantLands.Cozy.Data;

namespace DistantLands.Cozy.EditorScripts
{
    [CustomEditor(typeof(CozyHorizonProfile))]
    [CanEditMultipleObjects]
    public class HorizonProfileEditor : Editor
    {

        VisualElement root;
        CozyHorizonModule linkedModule;

        public override VisualElement CreateInspectorGUI()
        {
            if (!linkedModule)
                if (CozyWeather.instance.GetModule(out CozyHorizonModule module))
                    linkedModule = module;


            root = new VisualElement();

            Label label = new Label();
            label.text = "";
            label.AddToClassList("h2");
            root.Add(label);

            ListView listView = new ListView();
            listView.BindProperty(serializedObject.FindProperty("layers"));
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            listView.showAddRemoveFooter = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showBorder = true;
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.headerTitle = "Layers";
            listView.showBoundCollectionSize = true;
            listView.showFoldoutHeader = true;

            root.Add(listView);

            foreach (PropertyField field in root.Query<PropertyField>().ToList())
                field.RegisterValueChangeCallback(x =>
                {
                    if (linkedModule)
                        linkedModule.UpdateSkyLayers();
                });

            return root;
        }

    }
}