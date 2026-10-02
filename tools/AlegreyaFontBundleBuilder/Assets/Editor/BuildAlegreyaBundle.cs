using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

internal static class BuildAlegreyaBundle
{
    private const string SourceFontPath = "Assets/Fonts/AlegreyaSans-Regular.ttf";
    private const string FontAssetPath = "Assets/Fonts/AlegreyaSans-Regular.asset";
    private const string TmpSettingsPath = "Assets/Resources/TMP Settings.asset";
    private const string BundleName = "knownformulahelper_fonts";
    private const string RequiredCharacters =
        "0123456789<>+-/.,:;!?%()[]{}&'\""
        + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
        + "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF"
        + "\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF"
        + "\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF"
        + "\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF"
        + "\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF"
        + "\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF"
        + "\u0100\u0101\u0102\u0103\u0104\u0105\u0106\u0107\u0108\u0109\u010A\u010B\u010C\u010D\u010E\u010F"
        + "\u0110\u0111\u0112\u0113\u0114\u0115\u0116\u0117\u0118\u0119\u011A\u011B\u011C\u011D\u011E\u011F"
        + "\u0120\u0121\u0122\u0123\u0124\u0125\u0126\u0127\u0128\u0129\u012A\u012B\u012C\u012D\u012E\u012F"
        + "\u0130\u0131\u0132\u0133\u0134\u0135\u0136\u0137\u0138\u0139\u013A\u013B\u013C\u013D\u013E\u013F"
        + "\u0140\u0141\u0142\u0143\u0144\u0145\u0146\u0147\u0148\u014A\u014B\u014C\u014D\u014E\u014F"
        + "\u0150\u0151\u0152\u0153\u0154\u0155\u0156\u0157\u0158\u0159\u015A\u015B\u015C\u015D\u015E\u015F"
        + "\u0160\u0161\u0162\u0163\u0164\u0165\u0166\u0167\u0168\u0169\u016A\u016B\u016C\u016D\u016E\u016F"
        + "\u0170\u0171\u0172\u0173\u0174\u0175\u0176\u0177\u0178\u0179\u017A\u017B\u017C\u017D\u017E\u017F"
        + "\u1E9E";

    public static void Build()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
                throw new InvalidOperationException("Could not import " + SourceFontPath + ".");

            EnsureTmpSettings();
            AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true
            );

            if (fontAsset == null)
                throw new InvalidOperationException(
                    "TMP could not create the Alegreya Sans font asset."
                );

            fontAsset.name = "AlegreyaSans-Regular";

            if (!fontAsset.TryAddCharacters(RequiredCharacters, out string missingCharacters))
            {
                string missing = string.IsNullOrEmpty(missingCharacters)
                    ? "unknown"
                    : missingCharacters;
                throw new InvalidOperationException(
                    "The font asset could not add required characters: " + missing
                );
            }

            if (!string.IsNullOrEmpty(missingCharacters))
                throw new InvalidOperationException(
                    "Alegreya Sans is missing required characters: " + missingCharacters
                );

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.ReadFontAssetDefinition();

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
            {
                if (atlasTexture != null)
                    AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            AssetImporter importer = AssetImporter.GetAtPath(FontAssetPath);
            if (importer == null)
                throw new InvalidOperationException(
                    "Could not obtain the TMP font asset importer."
                );

            importer.assetBundleName = BundleName;
            importer.SaveAndReimport();

            string outputDirectory = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "BuildOutput"
            );
            Directory.CreateDirectory(outputDirectory);

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                outputDirectory,
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64
            );

            if (manifest == null)
                throw new InvalidOperationException(
                    "Unity did not produce an AssetBundle manifest."
                );

            string bundlePath = Path.Combine(outputDirectory, BundleName);
            if (!File.Exists(bundlePath))
                throw new InvalidOperationException("Unity did not produce " + bundlePath + ".");

            Debug.Log(
                "Built "
                    + bundlePath
                    + " with "
                    + fontAsset.characterLookupTable.Count
                    + " glyph entries."
            );
            AssetDatabase.DeleteAsset(TmpSettingsPath);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Validate()
    {
        try
        {
            string bundlePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "BuildOutput",
                BundleName
            );
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                throw new InvalidOperationException("Could not load " + bundlePath + ".");

            TMP_FontAsset[] fontAssets = bundle.LoadAllAssets<TMP_FontAsset>();
            if (fontAssets.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one TMP font asset, found " + fontAssets.Length + "."
                );

            TMP_FontAsset fontAsset = fontAssets[0];
            foreach (char character in RequiredCharacters)
            {
                if (!fontAsset.characterLookupTable.ContainsKey(character))
                    throw new InvalidOperationException(
                        "Bundle is missing U+" + ((int)character).ToString("X4") + "."
                    );
            }

            if (fontAsset.material == null || fontAsset.material.shader == null)
                throw new InvalidOperationException(
                    "Bundle font asset has no usable TMP material shader."
                );

            Debug.Log(
                "Validated "
                    + fontAsset.name
                    + ": "
                    + fontAsset.characterLookupTable.Count
                    + " glyph entries, material shader '"
                    + fontAsset.material.shader.name
                    + "'."
            );
            bundle.Unload(false);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureTmpSettings()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));

        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<TMP_Settings>();
            AssetDatabase.CreateAsset(settings, TmpSettingsPath);
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("assetVersion").stringValue = "2";
        SerializedProperty activeFontFeatures = serializedSettings.FindProperty(
            "m_ActiveFontFeatures"
        );
        if (activeFontFeatures != null)
            activeFontFeatures.arraySize = 0;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        TMP_Settings.LoadDefaultSettings();
    }
}
