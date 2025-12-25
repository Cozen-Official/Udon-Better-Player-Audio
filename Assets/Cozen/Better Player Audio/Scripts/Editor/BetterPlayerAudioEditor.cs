#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cozen
{
    [CustomEditor(typeof(BetterPlayerAudio))]
    public class BetterPlayerAudioEditor : Editor
    {
        private SerializedProperty minPlayersProperty;
        private SerializedProperty maxPlayersProperty;
        private SerializedProperty extendedPlayerRangeProperty;
        private SerializedProperty curveTypeProperty;
        private SerializedProperty startDistanceProperty;
        private SerializedProperty endDistanceProperty;
        private SerializedProperty startGainProperty;
        private SerializedProperty endGainProperty;
        private SerializedProperty voiceLowPassProperty;
        private SerializedProperty checkIntervalProperty;
        
        private const int NormalMaxPlayers = 80;
        private const int ExtendedMaxPlayers = 100;
        private const int CurveGraphHeight = 150;
        private const int CurveGraphPadding = 40;
        
        private void OnEnable()
        {
            minPlayersProperty = serializedObject.FindProperty("minPlayers");
            maxPlayersProperty = serializedObject.FindProperty("maxPlayers");
            extendedPlayerRangeProperty = serializedObject.FindProperty("extendedPlayerRange");
            curveTypeProperty = serializedObject.FindProperty("curveType");
            startDistanceProperty = serializedObject.FindProperty("startDistance");
            endDistanceProperty = serializedObject.FindProperty("endDistance");
            startGainProperty = serializedObject.FindProperty("startGain");
            endGainProperty = serializedObject.FindProperty("endGain");
            voiceLowPassProperty = serializedObject.FindProperty("voiceLowPass");
            checkIntervalProperty = serializedObject.FindProperty("checkInterval");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            BetterPlayerAudio targetScript = (BetterPlayerAudio)target;
            
            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Better Player Audio", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Developed by Cozen");
            EditorGUILayout.HelpBox(
                "This script runs locally on every player's client. Each player's instance adjusts how they hear other players' voices based on the current player count.",
                MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Player Count Thresholds Section
            EditorGUILayout.LabelField("Player Count Thresholds", EditorStyles.boldLabel);
            
            bool extendedRange = extendedPlayerRangeProperty.boolValue;
            int maxRange = extendedRange ? ExtendedMaxPlayers : NormalMaxPlayers;
            
            // Extended range checkbox
            EditorGUILayout.PropertyField(extendedPlayerRangeProperty, 
                new GUIContent("I get special treatment (unusual)", 
                    "Enable extended player range (1-100) for special VRChat events. Normal instances cap at 80 players."));
            
            // Min Players slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Min Players", 
                "The minimum number of players before interpolation kicks in. Below this count, audio settings remain at their start values."), 
                GUILayout.Width(EditorGUIUtility.labelWidth));
            minPlayersProperty.intValue = EditorGUILayout.IntSlider(minPlayersProperty.intValue, 1, maxRange);
            EditorGUILayout.EndHorizontal();
            
            // Max Players slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Max Players", 
                "The maximum number of players at which interpolation reaches its end values. Above this count, audio settings remain at their end values."), 
                GUILayout.Width(EditorGUIUtility.labelWidth));
            maxPlayersProperty.intValue = EditorGUILayout.IntSlider(maxPlayersProperty.intValue, 1, maxRange);
            EditorGUILayout.EndHorizontal();
            
            // Validation: ensure min < max
            if (minPlayersProperty.intValue >= maxPlayersProperty.intValue)
            {
                EditorGUILayout.HelpBox("Min Players must be less than Max Players!", MessageType.Error);
                // Auto-correct
                if (maxPlayersProperty.intValue <= 1)
                {
                    maxPlayersProperty.intValue = 2;
                }
                if (minPlayersProperty.intValue >= maxPlayersProperty.intValue)
                {
                    minPlayersProperty.intValue = maxPlayersProperty.intValue - 1;
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Interpolation Curve Section
            EditorGUILayout.LabelField("Interpolation Curve", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(curveTypeProperty, new GUIContent("Curve Type", 
                "The curve type used to interpolate between start and end values as player count changes."));
            
            // Draw the curve graph
            EditorGUILayout.Space(5);
            DrawCurveGraph(targetScript, maxRange);
            
            EditorGUILayout.Space(10);
            
            // Voice Far Distance Section
            EditorGUILayout.LabelField("Voice Far Distance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startDistanceProperty, new GUIContent("Start Distance (m)", 
                "The voice far distance (in meters) when player count is at or below Min Players."));
            EditorGUILayout.PropertyField(endDistanceProperty, new GUIContent("End Distance (m)", 
                "The voice far distance (in meters) when player count reaches or exceeds Max Players."));
            
            EditorGUILayout.Space(10);
            
            // Voice Gain Section
            EditorGUILayout.LabelField("Voice Gain", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startGainProperty, new GUIContent("Start Gain (dB)", 
                "The voice gain (in decibels, 0-24) when player count is at or below Min Players."));
            EditorGUILayout.PropertyField(endGainProperty, new GUIContent("End Gain (dB)", 
                "The voice gain (in decibels, 0-24) when player count reaches or exceeds Max Players."));
            
            EditorGUILayout.Space(10);
            
            // Voice Options Section
            EditorGUILayout.LabelField("Voice Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(voiceLowPassProperty, new GUIContent("Voice Low Pass", 
                "Whether to enable the low-pass filter on distant voices."));
            
            EditorGUILayout.Space(10);
            
            // Update Settings Section
            EditorGUILayout.LabelField("Update Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(checkIntervalProperty, new GUIContent("Check Interval (s)", 
                "Interval in seconds between periodic player count checks. This serves as a fallback in case join/leave events are missed. Set to 0 to disable periodic checks."));
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawCurveGraph(BetterPlayerAudio targetScript, int maxRange)
        {
            // Reserve space for the graph
            Rect graphRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, 
                GUILayout.Height(CurveGraphHeight + CurveGraphPadding * 2));
            
            // Draw background
            EditorGUI.DrawRect(graphRect, new Color(0.15f, 0.15f, 0.15f));
            
            // Calculate inner graph area (with padding for labels)
            Rect innerRect = new Rect(
                graphRect.x + CurveGraphPadding + 10,
                graphRect.y + 20,
                graphRect.width - CurveGraphPadding - 30,
                graphRect.height - CurveGraphPadding - 30
            );
            
            // Draw grid
            DrawGrid(innerRect, maxRange);
            
            // Draw axis labels
            DrawAxisLabels(graphRect, innerRect, maxRange, targetScript);
            
            // Draw the curve
            DrawCurve(innerRect, targetScript, maxRange);
            
            // Draw legend
            DrawLegend(graphRect, targetScript);
        }
        
        private void DrawGrid(Rect rect, int maxRange)
        {
            // Draw grid lines
            Handles.color = new Color(0.3f, 0.3f, 0.3f);
            
            // Vertical lines (every 10 players)
            int step = maxRange <= 80 ? 10 : 20;
            for (int i = 0; i <= maxRange; i += step)
            {
                float x = rect.x + (i / (float)maxRange) * rect.width;
                Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.y + rect.height));
            }
            
            // Horizontal lines (0%, 25%, 50%, 75%, 100%)
            for (int i = 0; i <= 4; i++)
            {
                float y = rect.y + rect.height - (i / 4f) * rect.height;
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.x + rect.width, y));
            }
            
            // Draw border
            Handles.color = new Color(0.5f, 0.5f, 0.5f);
            Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y));
            Handles.DrawLine(new Vector3(rect.x, rect.y + rect.height), new Vector3(rect.x + rect.width, rect.y + rect.height));
            Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.y + rect.height));
            Handles.DrawLine(new Vector3(rect.x + rect.width, rect.y), new Vector3(rect.x + rect.width, rect.y + rect.height));
        }
        
        private void DrawAxisLabels(Rect graphRect, Rect innerRect, int maxRange, BetterPlayerAudio targetScript)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            
            GUIStyle axisLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            // X-axis label
            GUI.Label(new Rect(graphRect.x, graphRect.y + graphRect.height - 18, graphRect.width, 16), 
                "Player Count", axisLabelStyle);
            
            // Y-axis label (rotated would be ideal but Unity makes that hard, so we use abbreviation)
            GUI.Label(new Rect(graphRect.x + 2, graphRect.y + graphRect.height / 2 - 8, 35, 16), 
                "t", axisLabelStyle);
            
            // X-axis tick labels
            int step = maxRange <= 80 ? 20 : 25;
            for (int i = 0; i <= maxRange; i += step)
            {
                float x = innerRect.x + (i / (float)maxRange) * innerRect.width;
                GUI.Label(new Rect(x - 15, innerRect.y + innerRect.height + 2, 30, 16), i.ToString(), labelStyle);
            }
            
            // Y-axis tick labels (0, 0.5, 1)
            GUI.Label(new Rect(innerRect.x - 25, innerRect.y + innerRect.height - 8, 20, 16), "0", labelStyle);
            GUI.Label(new Rect(innerRect.x - 30, innerRect.y + innerRect.height / 2 - 8, 25, 16), "0.5", labelStyle);
            GUI.Label(new Rect(innerRect.x - 25, innerRect.y - 8, 20, 16), "1", labelStyle);
            
            // Min/Max markers
            int minPlayers = targetScript.minPlayers;
            int maxPlayers = targetScript.maxPlayers;
            
            GUIStyle markerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.4f, 0.8f, 0.4f) }
            };
            
            float minX = innerRect.x + (minPlayers / (float)maxRange) * innerRect.width;
            float maxX = innerRect.x + (maxPlayers / (float)maxRange) * innerRect.width;
            
            GUI.Label(new Rect(minX - 20, innerRect.y - 18, 40, 16), $"Min:{minPlayers}", markerStyle);
            
            markerStyle.normal.textColor = new Color(0.8f, 0.4f, 0.4f);
            GUI.Label(new Rect(maxX - 20, innerRect.y - 18, 40, 16), $"Max:{maxPlayers}", markerStyle);
        }
        
        private void DrawCurve(Rect rect, BetterPlayerAudio targetScript, int maxRange)
        {
            int minPlayers = targetScript.minPlayers;
            int maxPlayers = targetScript.maxPlayers;
            AudioInterpolationCurve curveType = targetScript.curveType;
            
            // Draw the curve with multiple segments for smoothness
            int segments = 100;
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                int playerCount = Mathf.RoundToInt((i / (float)segments) * maxRange);
                float t = CalculateInterpolationFactorForEditor(playerCount, minPlayers, maxPlayers, curveType);
                
                float x = rect.x + (i / (float)segments) * rect.width;
                float y = rect.y + rect.height - t * rect.height;
                
                points[i] = new Vector3(x, y, 0);
            }
            
            // Draw the curve line
            Handles.color = new Color(0.2f, 0.6f, 1f);
            Handles.DrawAAPolyLine(3f, points);
            
            // Draw min threshold line (vertical, green)
            float minX = rect.x + (minPlayers / (float)maxRange) * rect.width;
            Handles.color = new Color(0.4f, 0.8f, 0.4f, 0.5f);
            Handles.DrawDottedLine(new Vector3(minX, rect.y), new Vector3(minX, rect.y + rect.height), 4f);
            
            // Draw max threshold line (vertical, red)
            float maxX = rect.x + (maxPlayers / (float)maxRange) * rect.width;
            Handles.color = new Color(0.8f, 0.4f, 0.4f, 0.5f);
            Handles.DrawDottedLine(new Vector3(maxX, rect.y), new Vector3(maxX, rect.y + rect.height), 4f);
        }
        
        private void DrawLegend(Rect graphRect, BetterPlayerAudio targetScript)
        {
            GUIStyle legendStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white }
            };
            
            float legendY = graphRect.y + 5;
            float legendX = graphRect.x + graphRect.width - 120;
            
            // Curve name
            GUI.Label(new Rect(legendX, legendY, 115, 16), $"Curve: {targetScript.curveType}", legendStyle);
        }
        
        /// <summary>
        /// Calculates the interpolation factor for the editor preview.
        /// This mirrors the runtime calculation but is available at edit time.
        /// </summary>
        private float CalculateInterpolationFactorForEditor(int playerCount, int minPlayers, int maxPlayers, AudioInterpolationCurve curveType)
        {
            if (playerCount <= minPlayers)
            {
                return 0f;
            }
            
            if (playerCount >= maxPlayers)
            {
                return 1f;
            }
            
            float range = maxPlayers - minPlayers;
            float position = playerCount - minPlayers;
            float t = position / range;
            
            return ApplyCurveForEditor(t, curveType);
        }
        
        /// <summary>
        /// Applies the curve transformation for the editor preview.
        /// </summary>
        private float ApplyCurveForEditor(float t, AudioInterpolationCurve curveType)
        {
            switch (curveType)
            {
                case AudioInterpolationCurve.EaseIn:
                    return t * t;
                    
                case AudioInterpolationCurve.EaseOut:
                    return 1f - (1f - t) * (1f - t);
                    
                case AudioInterpolationCurve.EaseInOut:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                    
                case AudioInterpolationCurve.Smoothstep:
                    return t * t * (3f - 2f * t);
                    
                case AudioInterpolationCurve.Smootherstep:
                    return t * t * t * (t * (6f * t - 15f) + 10f);
                    
                case AudioInterpolationCurve.Exponential:
                    return Mathf.Pow(2f, t) - 1f;
                    
                case AudioInterpolationCurve.Diminishing:
                    return Mathf.Sqrt(t);
                    
                case AudioInterpolationCurve.LogLike:
                    return Mathf.Log(Mathf.Lerp(1f, Mathf.Exp(1f), t));
                    
                case AudioInterpolationCurve.Linear:
                default:
                    return t;
            }
        }
    }
}
#endif
