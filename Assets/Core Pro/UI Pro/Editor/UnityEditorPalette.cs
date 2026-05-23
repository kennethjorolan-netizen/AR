// Colors taken from: https://www.foundations.unity.com/fundamentals/color-palette
namespace CorePro.Editor
{
    using UnityEngine;
    using UnityEditor;

    public static class UnityEditorPalette
    {
        private static readonly Color windowBackground_Dark= new Color(0.220f, 0.220f, 0.220f, 1f);
        private static readonly Color windowBackground_Light = new Color(0.784f, 0.784f, 0.784f, 1f);
   
        private static readonly Color helpBoxText_Dark = new Color(0.741f, 0.741f, 0.741f, 1f);
        private static readonly Color helpBoxText_Light = new Color(0.11f, 0.11f, 0.11f, 1f);
        // Inspector Titlebar Border Accent
        private static readonly Color inspectorTitlebarBorderAccent_Dark = new Color(0.188f, 0.188f, 0.188f, 1f); //#303030
        private static readonly Color inspectorTitlebarBorderAccent_Light = new Color(0.729f, 0.729f, 0.729f, 1f); // #BABABA
        // Inspector Titlebar Border
        private static readonly Color inspectorTitlebarBorder_Dark = new Color(0.102f, 0.102f, 0.102f, 1f); // #1A1A1A
        private static readonly Color inspectorTitlebarBorder_Light = new Color(0.102f, 0.102f, 0.102f, 1f); // #1A1A1A
        // Default border
        private static readonly Color default_border_Dark = new Color(0.137f, 0.137f, 0.137f, 1f); // #232323
        private static readonly Color default_border_Light = new Color(0.6f, 0.6f, 0.6f, 1f); // ##232323
        // Tab Background
        private static readonly Color tabBackground_Dark = new Color(0.208f, 0.208f, 0.208f, 1f); // #383838

        public static Color WindowBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return windowBackground_Dark;
                else
                    return windowBackground_Light;
            }
        }
        
        public static Color TabBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return tabBackground_Dark;
                else
                    return windowBackground_Light;
            }
        }

        public static Color HelpBoxText
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return helpBoxText_Dark;
                else
                    return helpBoxText_Light;
            }
        }
        
        public static Color DefaultBorder
        {   
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return default_border_Dark;
                else
                    return default_border_Light;
            }
        } 
        
        public static Color InspectorTitlebarBorderAccent
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return inspectorTitlebarBorderAccent_Dark;
                else
                    return inspectorTitlebarBorderAccent_Light;
            }
        }
        
        public static Color InspectorTitlebarBorder
        {   
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return inspectorTitlebarBorder_Dark;
                else
                    return inspectorTitlebarBorder_Light;
            }
        } 
    }
}
