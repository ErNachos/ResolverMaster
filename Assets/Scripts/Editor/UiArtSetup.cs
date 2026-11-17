using System;
using System.Collections.Generic;
using System.IO;
using ResolverMaster.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-005 interface assets:
    /// - TextMesh Pro: imports the TMP Essential Resources once (shaders, settings) and creates the Fredoka SDF font assets (static atlas
    ///   with the Latin characters the texts use) plus the bold font material with the dark SDF outline;
    /// - the import settings of the Gemini UI sprites cut by Tools/Art/make_ui_art.py (9-slice borders from Assets/Art/UI/ui_art.json);
    /// - the material of the disabled buttons (shader ResolverMaster/UI Desaturate);
    /// - fills the <see cref="UiKitStyle"/> with all of them.
    /// The TMP resources are imported once, before the rest (in batchmode AssetDatabase.ImportPackage would finish after the editor quits):
    /// Unity -batchmode -quit -projectPath P -importPackage "P/Library/PackageCache/com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage"
    /// (or the menu Resolver Master > Setup > Import TMP Essentials in the open Editor).
    /// </summary>
    public static class UiArtSetup
    {
        public const string UiArtDir = "Assets/Art/UI";
        public const string ManifestPath = UiArtDir + "/ui_art.json";
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string FontRegularAsset = "Assets/Art/Fonts/Fredoka-Regular SDF.asset";
        public const string FontBoldAsset = "Assets/Art/Fonts/Fredoka-SemiBold SDF.asset";
        public const string FontBoldOutlinedMaterial = "Assets/Art/Fonts/Fredoka-SemiBold SDF Outline.mat";
        public const string DisabledMaterialPath = "Assets/Art/Shaders/UiDesaturate.mat";
        public const string DisabledShader = "ResolverMaster/UI Desaturate";

        /// <summary>Every Gemini element the game needs (the validator checks they exist).</summary>
        public static readonly string[] Elements =
        {
            "btn_green", "btn_cream", "btn_round_cream", "btn_round_yellow", "panel", "ribbon", "banner_chapter",
            "tile_level", "tile_done", "tile_locked",
            "icon_back", "icon_next", "icon_home", "icon_close", "icon_undo", "icon_restart", "icon_help", "icon_check",
            "star_full", "star_empty", "icon_lock", "icon_settings",
            "icon_sound_on", "icon_sound_off", "icon_vibration", "icon_language",
            "icon_faucet", "icon_trophy", "icon_bolt", "icon_calendar",
        };

        /// <summary>Characters baked in the font atlases: ASCII, Italian and a few common European letters and signs.</summary>
        public const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "àèéìíîòóùúÀÈÉÌÍÒÓÙÚçÇñÑäöüÄÖÜßâêôûëïÂÊÔ·×–—‘’“”…«»°€¡¿";

        [Serializable]
        sealed class Manifest
        {
            public List<Entry> elements = new List<Entry>();
        }

        [Serializable]
        sealed class Entry
        {
            public string name;
            public int w, h;
            public string slice;
            public int[] border;
        }

        // ------------------------------------------------------------------ TextMesh Pro

        [MenuItem("Resolver Master/Setup/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (File.Exists(TmpSettingsPath))
            {
                Debug.Log("UiArtSetup: TMP Essential Resources already imported.");
                return;
            }
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (info == null) throw new InvalidOperationException("package com.unity.ugui not found");
            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package)) throw new FileNotFoundException("TMP Essential Resources not found", package);
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
            Debug.Log("UiArtSetup: TMP Essential Resources imported into Assets/TextMesh Pro.");
        }

        /// <summary>Creates the Fredoka SDF font assets and the outlined material when missing (<paramref name="rebuild"/>: always).</summary>
        public static void EnsureFonts(bool rebuild = false)
        {
            if (!File.Exists(TmpSettingsPath))
                throw new InvalidOperationException("TMP Essential Resources missing: import them first (-importPackage, see UiArtSetup)");
            var regular = CreateFontAsset(UiKitAssets.FontRegularPath, FontRegularAsset, rebuild);
            var bold = CreateFontAsset(UiKitAssets.FontBoldPath, FontBoldAsset, rebuild);
            if (regular == null || bold == null) throw new InvalidOperationException("font assets not created");
            if (rebuild || !File.Exists(FontBoldOutlinedMaterial))
            {
                if (File.Exists(FontBoldOutlinedMaterial)) AssetDatabase.DeleteAsset(FontBoldOutlinedMaterial);
                var mat = new Material(bold.material) { name = Path.GetFileNameWithoutExtension(FontBoldOutlinedMaterial) };
                AssetDatabase.CreateAsset(mat, FontBoldOutlinedMaterial);
            }
        }

        /// <summary>Outline values of the outlined material come from the style (called on every setup run, so a style change applies).</summary>
        public static void ApplyOutline(UiKitStyle style)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FontBoldOutlinedMaterial);
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", style.textOutline);
            mat.SetColor("_OutlineColor", style.outline);
            mat.SetFloat("_FaceDilate", style.textOutlineDilate);
            ShaderUtilities.GetShaderPropertyIDs();
            ShaderUtilities.UpdateShaderRatios(mat);
            EditorUtility.SetDirty(mat);
        }

        static TMP_FontAsset CreateFontAsset(string fontPath, string outPath, bool rebuild)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null && !rebuild) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(outPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) throw new FileNotFoundException("font missing", fontPath);
            // 90 pt sampling, padding 12: room for a thick outline; static atlas = no runtime changes, nothing rewritten while playing
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 12, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            if (fa == null) throw new InvalidOperationException("TMP could not read " + fontPath);
            fa.TryAddCharacters(Charset, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"UiArtSetup: {Path.GetFileName(fontPath)} has no glyph for: {missing}");
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            fa.name = Path.GetFileNameWithoutExtension(outPath);
            AssetDatabase.CreateAsset(fa, outPath);
            var tex = fa.atlasTexture;
            tex.name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(tex, fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiArtSetup: {outPath} created ({fa.characterTable.Count} characters, atlas {tex.width}x{tex.height}).");
            return fa;
        }

        // ------------------------------------------------------------------ Gemini sprites

        /// <summary>Sprite import settings of every element listed in ui_art.json: mipmaps (they are drawn smaller), 9-slice borders.</summary>
        public static int ConfigureUiArt()
        {
            if (!File.Exists(ManifestPath)) throw new FileNotFoundException("run python Tools/Art/make_ui_art.py", ManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            int changed = 0;
            foreach (var e in manifest.elements)
            {
                string path = $"{UiArtDir}/{e.name}.png";
                AssetDatabase.ImportAsset(path);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null)
                {
                    Debug.LogError("UiArtSetup: not a texture: " + path);
                    continue;
                }
                ArtImportRules.ApplyUiArtDefaults(imp);
                var b = e.border != null && e.border.Length == 4 ? new Vector4(e.border[0], e.border[1], e.border[2], e.border[3]) : Vector4.zero;
                imp.spriteBorder = b;
                imp.SaveAndReimport();
                changed++;
            }
            return changed;
        }

        static Sprite Ui(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiArtDir}/{name}.png");

        /// <summary>The material of disabled buttons (created when missing).</summary>
        public static Material EnsureDisabledMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DisabledMaterialPath);
            if (mat != null) return mat;
            var shader = Shader.Find(DisabledShader);
            if (shader == null) throw new InvalidOperationException("shader '" + DisabledShader + "' not found (Assets/Art/Shaders/UiDesaturate.shader)");
            mat = new Material(shader) { name = "UiDesaturate" };
            AssetDatabase.CreateAsset(mat, DisabledMaterialPath);
            return mat;
        }

        /// <summary>Puts fonts, materials and the Gemini sprites into the style.</summary>
        public static void FillStyle(UiKitStyle s)
        {
            s.fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRegularAsset);
            s.fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldAsset);
            s.fontBoldOutlined = AssetDatabase.LoadAssetAtPath<Material>(FontBoldOutlinedMaterial);
            s.disabledMaterial = EnsureDisabledMaterial();
            s.buttonGreen = Ui("btn_green");
            s.buttonCream = Ui("btn_cream");
            s.roundCream = Ui("btn_round_cream");
            s.roundYellow = Ui("btn_round_yellow");
            s.panel = Ui("panel");
            s.ribbon = Ui("ribbon");
            s.chapterBanner = Ui("banner_chapter");
            s.tileLevel = Ui("tile_level");
            s.tileDone = Ui("tile_done");
            s.tileLocked = Ui("tile_locked");
            s.starFull = Ui("star_full");
            s.starEmpty = Ui("star_empty");
            ApplyOutline(s);
            EditorUtility.SetDirty(s);
        }
    }
}
