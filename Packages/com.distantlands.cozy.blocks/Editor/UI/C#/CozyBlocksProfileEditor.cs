

using DistantLands.Cozy.Data;
using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.EditorScripts
{
    [CustomEditor(typeof(BlockProfile))]
    [CanEditMultipleObjects]
    public class E_BlockProfile : Editor
    {

        BlockProfile t;
        public static BlockProfile.TimeBlocks selectedTime = 0;
        public CozyTransitModule transit;
        public CozyTransitModuleEditor transitEditor;

        void OnEnable()
        {

            t = (BlockProfile)target;
            if (selectedTime == 0)
                selectedTime = GetFirstActiveBlock();
            CozyWeather.instance.GetModule(out transit);
            transitEditor = CreateEditor(transit) as CozyTransitModuleEditor;

        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBlocksBar();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBlocks"), true);
            EditorGUILayout.Space(20);
            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("BoldLabel"))
            {
                fontStyle = FontStyle.Bold
            };


            switch (selectedTime)
            {
                case BlockProfile.TimeBlocks.dawn:
                    EditorGUILayout.LabelField("Dawn", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("dawnBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("dawn"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.morning:
                    EditorGUILayout.LabelField("Morning", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("morningBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("morning"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.day:
                    EditorGUILayout.LabelField("Day", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("dayBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("day"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.afternoon:
                    EditorGUILayout.LabelField("Afternoon", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("afternoonBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("afternoon"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.evening:
                    EditorGUILayout.LabelField("Evening", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("eveningBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("evening"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.twilight:
                    EditorGUILayout.LabelField("Twilight", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("twilightBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("twilight"));
                    EditorGUI.indentLevel--;
                    break;
                case BlockProfile.TimeBlocks.night:
                    EditorGUILayout.LabelField("Night", labelStyle);
                    EditorGUI.indentLevel++;
                    transitEditor.DisplayBlockEditor("nightBlock");
                    RenderListOfBlocks(serializedObject.FindProperty("night"));
                    EditorGUI.indentLevel--;
                    break;

            }

            serializedObject.ApplyModifiedProperties();

        }

        public void EditInline(BlocksModule mod)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {

                mod.GetBlocks();
                serializedObject.ApplyModifiedProperties();

            }
        }

        public void DrawBlocksBar()
        {

            Rect box = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(box, new Color(0.1f, 0.1f, 0.1f));
            CozyTransitModule.TimeBlockName currentBlock = transit.GetTimeBlock();

            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.night))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.night);
                Rect fill2 = new Rect(box.x, box.y, box.width * ConvertTimeBlockToTransitBlock(GetFirstActiveBlock()).start - 15, box.height);

                if (selectedTime == BlockProfile.TimeBlocks.night)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y - 1, fill.width, fill.height + 2), new Color(0f, 1f, 1f));
                    EditorGUI.DrawRect(new Rect(fill2.x, fill2.y - 1, fill2.width, fill2.height + 2), new Color(0f, 1f, 1f));
                }

                if (currentBlock == CozyTransitModule.TimeBlockName.night)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                    EditorGUI.DrawRect(new Rect(fill2.x, fill2.y + 1, fill2.width, fill2.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0, 0, 0.3f));
                EditorGUI.DrawRect(fill2, new Color(0, 0, 0.3f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.nightBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && (fill.Contains(Event.current.mousePosition) || fill2.Contains(Event.current.mousePosition)))
                {
                    selectedTime = BlockProfile.TimeBlocks.night;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.dawn))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.dawn);

                if (selectedTime == BlockProfile.TimeBlocks.dawn)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.dawn)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.5f, 0.3f, 0.6f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.dawnBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.dawn;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.morning))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.morning);

                if (selectedTime == BlockProfile.TimeBlocks.morning)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.morning)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.7f, 0.3f, 0.3f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.morningBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.morning;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.day))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.day);

                if (selectedTime == BlockProfile.TimeBlocks.day)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.day)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.3f, 0.6f, 0.9f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.dayBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.day;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.afternoon))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.afternoon);

                if (selectedTime == BlockProfile.TimeBlocks.afternoon)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.afternoon)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.5f, 0.75f, 0.9f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.afternoonBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.afternoon;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.evening))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.evening);

                if (selectedTime == BlockProfile.TimeBlocks.evening)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.evening)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.8f, 0.5f, 0.3f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.eveningBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.evening;
                    Repaint();
                }
            }
            if (t.timeBlocks.HasFlag(BlockProfile.TimeBlocks.twilight))
            {
                Rect fill = GetRectForBlock(box, BlockProfile.TimeBlocks.twilight);

                if (selectedTime == BlockProfile.TimeBlocks.twilight)
                {
                    EditorGUI.DrawRect(new Rect(fill.x - 1, fill.y - 1, fill.width + 2, fill.height + 2), new Color(0f, 1f, 1f));
                }
                if (currentBlock == CozyTransitModule.TimeBlockName.twilight)
                {
                    EditorGUI.DrawRect(new Rect(fill.x, fill.y + 2, fill.width, fill.height), new Color(0f, 0.7f, 1f));
                }

                EditorGUI.DrawRect(fill, new Color(0.8f, 0.3f, 0.6f));
                EditorGUI.DrawRect(new Rect(fill.x, box.y, 1, box.height), Color.white);
                EditorGUI.DrawRect(new Rect(box.width * transit.twilightBlock.end, box.y, 2, box.height), new Color(1, 1, 1, 0.1f));
                if (Event.current.type == EventType.MouseDown && fill.Contains(Event.current.mousePosition))
                {
                    selectedTime = BlockProfile.TimeBlocks.twilight;
                    Repaint();
                }
            }


            Rect lowerBar = EditorGUILayout.GetControlRect(false, 10);
            // EditorGUI.DrawRect(new Rect((lowerBar.width * transit.weatherSphere.modifiedDayPercentage) - 1, lowerBar.y + 5, 2, 10), Color.white);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            transit.weatherSphere.timeModule.currentTime = GUI.HorizontalSlider(lowerBar, transit.weatherSphere.timeModule.currentTime, 0, 1);
            EditorGUI.indentLevel = indent;
            EditorGUILayout.Space();
        }

        public BlockProfile.TimeBlocks NextBlock(BlockProfile.TimeBlocks currentBlock)
        {
            for (int i = (int)currentBlock * 2; i < (int)BlockProfile.TimeBlocks.night; i += i)
            {
                if (t.timeBlocks.HasFlag((BlockProfile.TimeBlocks)i))
                    return (BlockProfile.TimeBlocks)i;
            }

            return BlockProfile.TimeBlocks.night;
        }

        public BlockProfile.TimeBlocks GetFirstActiveBlock()
        {
            for (int i = 1; i < (int)BlockProfile.TimeBlocks.night; i += i)
            {
                if (t.timeBlocks.HasFlag((BlockProfile.TimeBlocks)i))
                    return (BlockProfile.TimeBlocks)i;
            }

            return BlockProfile.TimeBlocks.night;
        }

        public Rect GetRectForBlock(Rect source, BlockProfile.TimeBlocks currentBlock)
        {
            float startPos = (source.width + 20) * ConvertTimeBlockToTransitBlock(currentBlock).start;
            CozyTransitModule.TimeBlock nextBlock = ConvertTimeBlockToTransitBlock(NextBlock(currentBlock));
            Rect rect;
            if (currentBlock != BlockProfile.TimeBlocks.night)
                rect = new Rect(startPos, source.y, (source.width + 20) * (nextBlock.start - ConvertTimeBlockToTransitBlock(currentBlock).start), source.height);
            else
                rect = new Rect(startPos, source.y, (source.width + 20) - startPos, source.height);

            return rect;
        }

        public CozyTransitModule.TimeBlock ConvertTimeBlockToTransitBlock(BlockProfile.TimeBlocks block)
        {
            if (block == BlockProfile.TimeBlocks.dawn)
                return transit.dawnBlock;
            if (block == BlockProfile.TimeBlocks.morning)
                return transit.morningBlock;
            if (block == BlockProfile.TimeBlocks.day)
                return transit.dayBlock;
            if (block == BlockProfile.TimeBlocks.afternoon)
                return transit.afternoonBlock;
            if (block == BlockProfile.TimeBlocks.evening)
                return transit.eveningBlock;
            if (block == BlockProfile.TimeBlocks.twilight)
                return transit.twilightBlock;

            return transit.nightBlock;
        }

        public void RenderListOfBlocks(SerializedProperty list)
        {

            EditorGUILayout.Space();

            if (list.arraySize == 0)
            {
                EditorGUILayout.HelpBox($"This time block contains no color blocks! Please add one for Blocks to function properly.", MessageType.Warning);
                EditorGUILayout.Space();
            }


            for (int i = 0; i < list.arraySize; i++)
            {
                Color bgColor = i % 2 == 1 ? Color.clear : new Color(0, 0, 0, 0.1f);
                Rect ctrl = EditorGUILayout.GetControlRect(false, 0);
                EditorGUI.DrawRect(new Rect(ctrl.x, ctrl.y - 5, ctrl.width, EditorGUIUtility.singleLineHeight * 4), bgColor);
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));

                EditorGUILayout.BeginHorizontal();

                if (list.GetArrayElementAtIndex(i).objectReferenceValue)
                    EditorGUILayout.LabelField((list.GetArrayElementAtIndex(i).objectReferenceValue as BlocksBlendable).chance.GetChance().ToString("Current chance after effectors is 0%"));

                if (GUILayout.Button("Remove"))
                    list.DeleteArrayElementAtIndex(i);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(25);
            }
            if (GUILayout.Button("Add Block"))
                list.InsertArrayElementAtIndex(list.arraySize);

        }

    }
}