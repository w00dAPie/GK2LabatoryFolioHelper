using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using GK2LaboratoryFolioHelper.Configuration;
using GK2LaboratoryFolioHelper.Infrastructure;
using TMPro;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class TypographyFontResolver
{
    private const string BundleName = "knownformulahelper_fonts.bundle";
    private const string RequiredCharacters =
        "0123456789<>+-/.,:;!?%()[]{}&'\""
        + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
        + "ÄÖÜäöüẞßÀÁÂÃÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕØŒÙÚÛÝŸ"
        + "àáâãåæçèéêëìíîïðñòóôõøœùúûýÿ";

    private static TMP_FontAsset referenceFont;
    private static Material referenceMaterial;
    private static TMP_SpriteAsset referenceSpriteAsset;
    private static TMP_FontAsset bundledFont;
    private static AssetBundle fontBundle;
    private static bool inventoryLogged;
    private static bool bundleAttempted;
    private static bool fallbackLogged;
    private static bool missingReferenceLogged;

    internal static bool IsSharp => UiConfig.FontStyle?.Value == UiFontStyle.Sharp;
    internal static Material ReferenceMaterial => referenceMaterial;
    internal static TMP_SpriteAsset ReferenceSpriteAsset => referenceSpriteAsset;
    internal static TMP_FontAsset BundledFont => bundledFont;

    internal static void Initialize(TextMeshProUGUI source)
    {
        if (source != null)
        {
            referenceFont ??= source.font;
            referenceMaterial ??= source.fontSharedMaterial;
            referenceSpriteAsset ??= source.spriteAsset;
        }

        ResolveReferenceFromLoadedUi();
        LogLoadedFontsOnce();
        TryLoadBundledFont();
    }

    internal static TMP_FontAsset ResolveFont()
    {
        ResolveReferenceFromLoadedUi();
        TryLoadBundledFont();
        return IsSharp ? bundledFont ?? referenceFont : referenceFont;
    }

    internal static void WarnMissingReferenceOnce()
    {
        if (missingReferenceLogged)
        {
            return;
        }

        missingReferenceLogged = true;
        ModLog.Warning(
            "Could not resolve a loaded TMP font for helper UI; retaining TMP defaults."
        );
    }

    internal static void Shutdown()
    {
        if (fontBundle != null)
        {
            fontBundle.Unload(false);
        }

        fontBundle = null;
        bundledFont = null;
        referenceFont = null;
        referenceMaterial = null;
        referenceSpriteAsset = null;
        inventoryLogged = false;
        bundleAttempted = false;
        fallbackLogged = false;
        missingReferenceLogged = false;
    }

    private static void ResolveReferenceFromLoadedUi()
    {
        if (referenceFont != null)
        {
            return;
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || text.font == null)
            {
                continue;
            }

            referenceFont = text.font;
            referenceMaterial = text.fontSharedMaterial;
            referenceSpriteAsset = text.spriteAsset;
            return;
        }
    }

    private static void LogLoadedFontsOnce()
    {
        if (inventoryLogged)
        {
            return;
        }

        inventoryLogged = true;
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        if (fonts.Length == 0)
        {
            ModLog.Debug("No TMP_FontAsset resources are loaded yet.");
            return;
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (TMP_FontAsset font in fonts)
        {
            if (font != null && !string.IsNullOrEmpty(font.name))
            {
                names.Add(font.name);
            }
        }

        ModLog.Debug($"Loaded game TMP fonts ({names.Count}): {string.Join(", ", names)}");
    }

    private static void TryLoadBundledFont()
    {
        if (bundleAttempted || !IsSharp)
        {
            return;
        }

        bundleAttempted = true;
        string bundlePath = GetBundlePath();
        if (!File.Exists(bundlePath))
        {
            LogFallbackOnce($"Bundle file was not found: '{bundlePath}'.");
            return;
        }

        try
        {
            fontBundle = AssetBundle.LoadFromFile(bundlePath);
            if (fontBundle == null)
            {
                LogFallbackOnce($"AssetBundle.LoadFromFile returned null: '{bundlePath}'.");
                return;
            }

            foreach (TMP_FontAsset candidate in fontBundle.LoadAllAssets<TMP_FontAsset>())
            {
                if (candidate == null || !ContainsRequiredCharacters(candidate))
                {
                    continue;
                }

                bundledFont = candidate;
                ModLog.Info(
                    $"Loaded validated helper TMP font '{candidate.name}' from '{bundlePath}'."
                );
                return;
            }

            LogFallbackOnce(
                $"The helper font bundle contains no TMP font with the required Latin, German, digit and punctuation glyphs: '{bundlePath}'."
            );
        }
        catch (Exception exception)
        {
            LogFallbackOnce(
                $"Could not load the optional helper font bundle '{bundlePath}': {exception.Message}"
            );
        }
    }

    private static bool ContainsRequiredCharacters(TMP_FontAsset font)
    {
        foreach (char character in RequiredCharacters)
        {
            if (!ContainsCharacter(font, character, new HashSet<TMP_FontAsset>()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsCharacter(
        TMP_FontAsset font,
        char character,
        HashSet<TMP_FontAsset> visited
    )
    {
        if (font == null || !visited.Add(font))
        {
            return false;
        }

        if (font.characterLookupTable.ContainsKey(character))
        {
            return true;
        }

        if (font.fallbackFontAssetTable == null)
        {
            return false;
        }

        foreach (TMP_FontAsset fallback in font.fallbackFontAssetTable)
        {
            if (ContainsCharacter(fallback, character, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetBundlePath()
    {
        string pluginDirectory = Path.GetDirectoryName(typeof(ModTypography).Assembly.Location);
        if (string.IsNullOrEmpty(pluginDirectory))
        {
            pluginDirectory = Path.Combine(Paths.PluginPath, "GK2KnownFormulaHelper");
        }

        return Path.Combine(pluginDirectory, "assets", BundleName);
    }

    private static void LogFallbackOnce(string reason)
    {
        if (fallbackLogged)
        {
            return;
        }

        fallbackLogged = true;
        ModLog.Warning($"Optional helper font unavailable; using the loaded game font. {reason}");
    }
}
