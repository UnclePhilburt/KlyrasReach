using DistantLands.Cozy.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DistantLands.Cozy.EditorScripts
{

    [CustomPropertyDrawer(typeof(CozyHorizonProfile.HorizonLayerReference))]
    public class CozyHorizonLayerDrawer : PropertyDrawer
    {
        CozyHorizonModule linkedModule;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!linkedModule)
                if (CozyWeather.instance.GetModule(out CozyHorizonModule module))
                    linkedModule = module;

            // Root container
            var container = new VisualElement();
            var dynamicContainer = new VisualElement();


            // Create property fields
            var layerTypeField = new PropertyField(property.FindPropertyRelative("layerType"));
            var placementLocationField = new PropertyField(property.FindPropertyRelative("placementLocation"));
            var textureField = new PropertyField(property.FindPropertyRelative("texture"));
            var colorField = new PropertyField(property.FindPropertyRelative("color"));
            var fogAmountField = new PropertyField(property.FindPropertyRelative("fogAmount"), "Fog Amount");
            var fogLightAmountField = new PropertyField(property.FindPropertyRelative("fogLightAmount"), "Fog Light Amount");
            var renderPriorityOffsetField = new PropertyField(property.FindPropertyRelative("renderPriorityOffset"), "Render Priority Offset");

            // Add fields to container
            container.Add(layerTypeField);
            container.Add(placementLocationField);
            container.Add(textureField);
            container.Add(colorField);
            container.Add(fogAmountField);
            container.Add(fogLightAmountField);
            container.Add(renderPriorityOffsetField);
            container.Add(dynamicContainer);

            // Add conditional fields based on layerType
            layerTypeField.RegisterValueChangeCallback(evt =>
            {
                var layerType = property.FindPropertyRelative("layerType").enumValueIndex;

                // Remove all dynamic fields
                dynamicContainer.Clear();

                // Conditional fields
                if (layerType == 1)
                {
                    var placementHeight = new PropertyField();
                    placementHeight.BindProperty(property.FindPropertyRelative("placementHeight"));
                    var verticalScale = new PropertyField();
                    verticalScale.BindProperty(property.FindPropertyRelative("verticalScale"));
                    var tiling = new PropertyField();
                    tiling.BindProperty(property.FindPropertyRelative("tiling"));
                    var angle = new PropertyField();
                    angle.BindProperty(property.FindPropertyRelative("angle"));

                    dynamicContainer.Add(placementHeight);
                    dynamicContainer.Add(verticalScale);
                    dynamicContainer.Add(tiling);
                    dynamicContainer.Add(angle);

                }

                if (layerType == 2 || layerType == 3)
                {
                    
                    var pitch = new PropertyField();
                    pitch.BindProperty(property.FindPropertyRelative("pitch"));
                    var yaw = new PropertyField();
                    yaw.BindProperty(property.FindPropertyRelative("yaw"));
                    var roll = new PropertyField();
                    roll.BindProperty(property.FindPropertyRelative("roll"));
                    var size = new PropertyField();
                    size.BindProperty(property.FindPropertyRelative("size"));

                    dynamicContainer.Add(pitch);
                    dynamicContainer.Add(yaw);
                    dynamicContainer.Add(roll);
                    dynamicContainer.Add(size);
                }

                if (layerType == 3)
                {
                    var rows = new PropertyField();
                    rows.BindProperty(property.FindPropertyRelative("rows"));
                    var columns = new PropertyField();
                    columns.BindProperty(property.FindPropertyRelative("columns"));
                    var framerate = new PropertyField();
                    framerate.BindProperty(property.FindPropertyRelative("framerate"));

                    dynamicContainer.Add(rows);
                    dynamicContainer.Add(columns);
                    dynamicContainer.Add(framerate);
                }

                foreach (PropertyField field in container.Query<PropertyField>().ToList())
                    field.RegisterValueChangeCallback(x =>
                    {
                        if (linkedModule)
                            linkedModule.UpdateSkyLayers();
                    });
            });
            var layerType = property.FindPropertyRelative("layerType").enumValueIndex;

            // Remove all dynamic fields
            dynamicContainer.Clear();

            // Conditional fields
            if (layerType == 1)
            {
                var placementHeight = new PropertyField(property.FindPropertyRelative("placementHeight"), "Placement Height");
                var verticalScale = new PropertyField(property.FindPropertyRelative("verticalScale"), "Vertical Scale");
                var tiling = new PropertyField(property.FindPropertyRelative("tiling"), "Tiling");
                var angle = new PropertyField(property.FindPropertyRelative("angle"), "Angle");

                dynamicContainer.Add(placementHeight);
                dynamicContainer.Add(verticalScale);
                dynamicContainer.Add(tiling);
                dynamicContainer.Add(angle);

            }

            if (layerType == 2 || layerType == 3)
            {
                var pitch = new PropertyField(property.FindPropertyRelative("pitch"), "Pitch");
                var yaw = new PropertyField(property.FindPropertyRelative("yaw"), "Yaw");
                var roll = new PropertyField(property.FindPropertyRelative("roll"), "Roll");
                var size = new PropertyField(property.FindPropertyRelative("size"), "Size");

                dynamicContainer.Add(pitch);
                dynamicContainer.Add(yaw);
                dynamicContainer.Add(roll);
                dynamicContainer.Add(size);
            }

            if (layerType == 3)
            {
                var rows = new PropertyField(property.FindPropertyRelative("rows"), "Rows");
                var columns = new PropertyField(property.FindPropertyRelative("columns"), "Columns");
                var framerate = new PropertyField(property.FindPropertyRelative("framerate"), "Framerate");

                dynamicContainer.Add(rows);
                dynamicContainer.Add(columns);
                dynamicContainer.Add(framerate);
            }

            foreach (PropertyField field in container.Query<PropertyField>().ToList())
                field.RegisterValueChangeCallback(x =>
                {
                    if (linkedModule)
                        linkedModule.UpdateSkyLayers();
                });

            return container;
        }

        private void UpdateVisibility(SerializedProperty property, VisualElement container)
        {
            var layerType = property.FindPropertyRelative("layerType").enumValueIndex;

            // Remove all dynamic fields
            container.Clear();

            VisualElement dynamicContainer = new VisualElement();

            // Conditional fields
            if (layerType == 1)
            {
                var placementHeight = new PropertyField(property.FindPropertyRelative("placementHeight"), "Placement Height");
                var verticalScale = new PropertyField(property.FindPropertyRelative("verticalScale"), "Vertical Scale");
                var tiling = new PropertyField(property.FindPropertyRelative("tiling"), "Tiling");
                var angle = new PropertyField(property.FindPropertyRelative("angle"), "Angle");

                dynamicContainer.Add(placementHeight);
                dynamicContainer.Add(verticalScale);
                dynamicContainer.Add(tiling);
                dynamicContainer.Add(angle);

            }

            if (layerType == 2 || layerType == 3)
            {
                var pitch = new PropertyField(property.FindPropertyRelative("pitch"), "Pitch");
                var yaw = new PropertyField(property.FindPropertyRelative("yaw"), "Yaw");
                var roll = new PropertyField(property.FindPropertyRelative("roll"), "Roll");
                var size = new PropertyField(property.FindPropertyRelative("size"), "Size");

                dynamicContainer.Add(pitch);
                dynamicContainer.Add(yaw);
                dynamicContainer.Add(roll);
                dynamicContainer.Add(size);
            }

            if (layerType == 3)
            {
                var rows = new PropertyField(property.FindPropertyRelative("rows"), "Rows");
                var columns = new PropertyField(property.FindPropertyRelative("columns"), "Columns");
                var framerate = new PropertyField(property.FindPropertyRelative("framerate"), "Framerate");

                dynamicContainer.Add(rows);
                dynamicContainer.Add(columns);
                dynamicContainer.Add(framerate);
            }

            foreach (PropertyField field in container.Query<PropertyField>().ToList())
                field.RegisterValueChangeCallback(x =>
                {
                    if (linkedModule)
                        linkedModule.UpdateSkyLayers();
                });
        }
    }


}