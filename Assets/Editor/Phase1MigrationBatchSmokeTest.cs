using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YGOSharp.OCGWrapper.Enums;

public static class Phase1MigrationBatchSmokeTest
{
    private sealed class AliasRestrictionCase
    {
        public int AliasCode;
        public int CardId;
        public int MaxCopies;
        public YGOSharp.Banlist Banlist;
    }

    private sealed class DeckParsingCase
    {
        public int MainSectionExtraCardId;
        public int ExtraSectionMainCardId;
        public int SideSectionCardId;
    }

    public static void Run()
    {
        string deckPath = RuntimePaths.GetFilePath(RuntimeDirectory.Deck, ".phase1-smoke.ydk");
        string banlistDeckPath = RuntimePaths.GetFilePath(RuntimeDirectory.Deck, ".phase1-banlist-smoke.ydk");
        string runtimeTextDirectoryPath = RuntimePaths.GetFilePath(RuntimeDirectory.Deck, ".phase2-runtime-text");
        string runtimeTextPath = Path.Combine(runtimeTextDirectoryPath, "sample.txt");
        string replayRecordName = ".phase1-smoke";
        string replayPath = RuntimeReplayFile.GetReplayRecordPath(replayRecordName);
        int exitCode = 0;

        try
        {
            RuntimeArchiveBootstrap.EnsureRequiredDirectories(Debug.Log);

            if (!File.Exists(RuntimePaths.GetFilePath(RuntimeDirectory.Config, "strings.conf")))
            {
                throw new Exception("config/strings.conf was not found through RuntimePaths");
            }

            if (!File.Exists(RuntimePaths.GetFilePath(RuntimeDirectory.Config, "ver.txt")))
            {
                throw new Exception("config/ver.txt was not found through RuntimePaths");
            }

            if (!File.Exists(RuntimePaths.GetFilePath(RuntimeDirectory.Config, "bot.conf")))
            {
                throw new Exception("config/bot.conf was not found through RuntimePaths");
            }

            if (!File.Exists(RuntimePaths.GetFilePath(RuntimeDirectory.Config, "hosts.conf")))
            {
                throw new Exception("config/hosts.conf was not found through RuntimePaths");
            }

            string puzzleDirectoryPath = RuntimePaths.GetDirectoryPath(RuntimeDirectory.Puzzle);
            if (!Directory.Exists(puzzleDirectoryPath))
            {
                throw new Exception("puzzle directory was not found through RuntimePaths");
            }

            FileInfo[] puzzleFiles = new DirectoryInfo(puzzleDirectoryPath).GetFiles("*.lua");
            if (puzzleFiles.Length == 0)
            {
                throw new Exception("puzzle directory did not contain any .lua files through RuntimePaths");
            }

            Texture2D runtimeTexture = RuntimeTextureLoader.Load(RuntimeDirectory.Texture, "duel", "phase", "phase.png");
            if (runtimeTexture == null)
            {
                throw new Exception("phase texture could not be loaded through RuntimeTextureLoader");
            }

            Texture2D texture = UIHelper.getTexture2D(RuntimeDirectory.Texture, "duel", "phase", "phase.png");
            if (texture == null)
            {
                throw new Exception("phase texture could not be loaded through RuntimePaths");
            }

            Texture2D deckTableTexture = RuntimeTextureLoader.Load(RuntimeDirectory.Texture, "duel", "deckTable.png");
            if (deckTableTexture == null)
            {
                throw new Exception("deckTable texture could not be loaded through RuntimeTextureLoader");
            }

            Texture2D backgroundDeskTexture = RuntimeTextureLoader.Load(RuntimeDirectory.Texture, "common", "desk.jpg");
            if (backgroundDeskTexture == null)
            {
                throw new Exception("common desk texture could not be loaded through RuntimeTextureLoader");
            }

            Texture2D fieldTexture = UIHelper.getTexture2D(RuntimeDirectory.Texture, "duel", "field.png");
            if (fieldTexture == null)
            {
                throw new Exception("field texture could not be loaded through RuntimePaths");
            }

            VerifyRuntimeTextureTools(fieldTexture);

            Texture2D newFieldTexture = UIHelper.getTexture2D(RuntimeDirectory.Texture, "duel", "newfield.png");
            if (newFieldTexture == null)
            {
                throw new Exception("newfield texture could not be loaded through RuntimePaths");
            }

            FileInfo[] fieldPictureFiles = new DirectoryInfo(RuntimePaths.GetFilePath(RuntimeDirectory.Picture, "field"))
                .GetFiles();
            FileInfo sampleFieldPicture = null;
            for (int i = 0; i < fieldPictureFiles.Length; i++)
            {
                string extension = fieldPictureFiles[i].Extension.ToLowerInvariant();
                if (extension == ".png" || extension == ".jpg")
                {
                    sampleFieldPicture = fieldPictureFiles[i];
                    break;
                }
            }

            if (sampleFieldPicture == null)
            {
                throw new Exception("picture/field did not contain a sample field image");
            }

            Texture2D fieldPictureTexture =
                UIHelper.getTexture2D(RuntimeDirectory.Picture, "field", sampleFieldPicture.Name);
            if (fieldPictureTexture == null)
            {
                throw new Exception("sample field picture could not be loaded through RuntimePaths");
            }

            FileInfo sampleUiTexture = FindSampleFile(
                RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "ui"),
                ".png");
            if (sampleUiTexture == null)
            {
                throw new Exception("texture/ui did not contain a sample .png file");
            }

            Texture2D uiTexture = GameTextureManager.get(Path.GetFileNameWithoutExtension(sampleUiTexture.Name));
            if (uiTexture == null)
            {
                throw new Exception("GameTextureManager.get could not load a sample texture/ui asset");
            }

            FileInfo sampleFaceTexture = FindSampleFile(
                RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "face"),
                ".png");
            if (sampleFaceTexture == null)
            {
                throw new Exception("texture/face did not contain a sample .png file");
            }

            string sampleFaceName = Path.GetFileNameWithoutExtension(sampleFaceTexture.Name);
            UIHelper.faces.Clear();
            InvokeUiHelperIniFaces();
            if (!UIHelper.faces.ContainsKey(sampleFaceName) || UIHelper.faces[sampleFaceName] == null)
            {
                throw new Exception("UIHelper.iniFaces did not load a sample texture/face asset");
            }

            Type cardsManagerType = InitializeCardData();
            IDictionary loadedCards = GetLoadedCards(cardsManagerType);
            VerifyYgoSharpHookBehavior(loadedCards, banlistDeckPath);
            VerifyDeckParsingBehavior(loadedCards, deckPath);

            RuntimePaths.EnsureDirectory(RuntimeDirectory.Deck);
            RuntimeTextFile.EnsureFileExists(runtimeTextPath);
            if (!File.Exists(runtimeTextPath))
            {
                throw new Exception("RuntimeTextFile.EnsureFileExists did not create the target file");
            }

            File.WriteAllText(runtimeTextPath, "a -> b\r\nline2\r\n");

            string[] runtimeTextLines = RuntimeTextFile.ReadNormalizedLines(runtimeTextPath);
            if (runtimeTextLines.Length < 3 || runtimeTextLines[0] != "a -> b" || runtimeTextLines[1] != "line2")
            {
                throw new Exception("RuntimeTextFile.ReadNormalizedLines did not preserve normalized line content");
            }

            string[] strippedRuntimeTextLines = RuntimeTextFile.ReadNormalizedLines(runtimeTextPath, true);
            if (strippedRuntimeTextLines.Length < 3 || strippedRuntimeTextLines[0] != "a->b" || strippedRuntimeTextLines[1] != "line2")
            {
                throw new Exception("RuntimeTextFile.ReadNormalizedLines(stripSpaces: true) did not strip spaces");
            }

            VerifyConfigAndInterStringPersistence(runtimeTextDirectoryPath);

            VerifyReplaySeamBehavior(replayRecordName, replayPath);

            Debug.Log("Phase1MigrationBatchSmokeTest OK");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            exitCode = 1;
        }
        finally
        {
            if (File.Exists(deckPath))
            {
                File.Delete(deckPath);
            }

            if (File.Exists(banlistDeckPath))
            {
                File.Delete(banlistDeckPath);
            }

            if (File.Exists(runtimeTextPath))
            {
                File.Delete(runtimeTextPath);
            }

            if (Directory.Exists(runtimeTextDirectoryPath))
            {
                Directory.Delete(runtimeTextDirectoryPath, true);
            }

            if (File.Exists(replayPath))
            {
                File.Delete(replayPath);
            }
        }

        EditorApplication.Exit(exitCode);
    }

    private static void VerifyReplaySeamBehavior(string replayRecordName, string replayPath)
    {
        Package replayPackage = new Package();
        replayPackage.Fuction = (int)GameMessage.sibyl_replay;
        replayPackage.Data = new BinaryMaster(new byte[] { 1, 2, 3, 4 });

        Package startPackage = new Package();
        startPackage.Fuction = (int)GameMessage.Start;
        startPackage.Data = new BinaryMaster(new byte[] { 9, 8, 7 });

        byte[] encodedReplayRecord = ReplayPackageCodec.Encode(new List<Package> { replayPackage, startPackage });
        RuntimeReplayFile.WriteReplayRecord(replayRecordName, encodedReplayRecord);
        if (!File.Exists(replayPath))
        {
            throw new Exception("RuntimeReplayFile.WriteReplayRecord did not create the target replay file");
        }

        byte[] persistedReplayRecord = RuntimeReplayFile.ReadReplayRecord(replayRecordName);
        if (!BuffersEqual(encodedReplayRecord, persistedReplayRecord))
        {
            throw new Exception("RuntimeReplayFile.ReadReplayRecord did not preserve the encoded replay bytes");
        }

        List<Package> decodedReplayPackages = ReplayPackageCodec.Decode(persistedReplayRecord);
        if (decodedReplayPackages.Count != 2)
        {
            throw new Exception("ReplayPackageCodec.Decode did not preserve replay package count");
        }

        if (decodedReplayPackages[0].Fuction != replayPackage.Fuction ||
            !BuffersEqual(decodedReplayPackages[0].Data.get(), replayPackage.Data.get()))
        {
            throw new Exception("ReplayPackageCodec failed to preserve the sibyl replay payload");
        }

        if (decodedReplayPackages[1].Fuction != startPackage.Fuction ||
            !BuffersEqual(decodedReplayPackages[1].Data.get(), startPackage.Data.get()))
        {
            throw new Exception("ReplayPackageCodec failed to preserve the non-replay payload");
        }
    }

    private static bool BuffersEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private static FileInfo FindSampleFile(string directoryPath, params string[] allowedExtensions)
    {
        if (!Directory.Exists(directoryPath))
        {
            return null;
        }

        FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            string extension = files[i].Extension.ToLowerInvariant();
            for (int extensionIndex = 0; extensionIndex < allowedExtensions.Length; extensionIndex++)
            {
                if (extension == allowedExtensions[extensionIndex])
                {
                    return files[i];
                }
            }
        }

        return null;
    }

    private static void InvokeUiHelperIniFaces()
    {
        MethodInfo initializeFacesMethod = typeof(UIHelper).GetMethod(
            "iniFaces",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (initializeFacesMethod == null)
        {
            throw new Exception("UIHelper.iniFaces was not found");
        }

        initializeFacesMethod.Invoke(null, null);
    }

    private static void VerifyConfigAndInterStringPersistence(string directoryPath)
    {
        string configPath = Path.Combine(directoryPath, "config-smoke.conf");
        string translationPath = Path.Combine(directoryPath, "translation-smoke.conf");
        string missingConfigDirectoryPath = Path.Combine(directoryPath, "missing-config");
        string missingTranslationDirectoryPath = Path.Combine(directoryPath, "missing-translation");
        string invalidConfigInitializePath = Path.Combine(configPath, "bootstrap.conf");
        string invalidTranslationInitializePath = Path.Combine(translationPath, "bootstrap.conf");
        string configKey = "__phase2_config_missing_key__";
        string configDefault = "default-value";
        string updatedConfigValue = "updated-value";
        string translationKey = "__phase2_translation@n@ui__";

        RuntimeTextFile.EnsureFileExists(configPath);
        RuntimeTextFile.EnsureFileExists(translationPath);

        object originalConfigTranslations = GetStaticField(typeof(Config), "translations");
        object originalConfigUis = GetStaticField(typeof(Config), "uits");
        object originalConfigPath = GetStaticField(typeof(Config), "path");
        object originalConfigLoaded = GetStaticField(typeof(Config), "loaded");
        object originalInterStringTranslations = GetStaticField(typeof(InterString), "translations");
        object originalInterStringPath = GetStaticField(typeof(InterString), "path");
        object originalInterStringLoaded = GetStaticField(typeof(InterString), "loaded");
        bool originalProgramNoAccess = Program.noAccess;
        bool originalRuntimeNoAccess = RuntimeStatus.NoAccess;

        try
        {
            SetStaticField(typeof(Config), "translations", Activator.CreateInstance(originalConfigTranslations.GetType()));
            SetStaticField(typeof(Config), "uits", Activator.CreateInstance(originalConfigUis.GetType()));
            SetStaticField(typeof(Config), "path", configPath);
            SetStaticField(typeof(Config), "loaded", false);

            Config.initialize(configPath);
            string configValue = Config.Get(configKey, configDefault);
            if (configValue != configDefault)
            {
                throw new Exception("Config.Get did not return the default value for a missing key");
            }

            string configText = File.ReadAllText(configPath);
            if (!configText.Contains(configKey + "->" + configDefault + "\r\n"))
            {
                throw new Exception("Config.Get did not append the missing key to disk");
            }

            Config.Set(configKey, updatedConfigValue);
            string updatedConfigText = File.ReadAllText(configPath);
            if (!updatedConfigText.Contains(configKey + "->" + updatedConfigValue + "\r\n"))
            {
                throw new Exception("Config.Set did not rewrite the updated key/value to disk");
            }

            SetStaticField(typeof(InterString), "translations", Activator.CreateInstance(originalInterStringTranslations.GetType()));
            SetStaticField(typeof(InterString), "path", translationPath);
            SetStaticField(typeof(InterString), "loaded", false);

            string translationValue = InterString.Get(translationKey);
            if (translationValue != "__phase2_translation\r\n__")
            {
                throw new Exception("InterString.Get did not format fallback translation text");
            }

            string translationText = File.ReadAllText(translationPath);
            if (!translationText.Contains(translationKey + "->" + translationKey + "\r\n"))
            {
                throw new Exception("InterString.Get did not append the missing translation to disk");
            }

            Program.noAccess = false;
            RuntimeStatus.NoAccess = false;
            SetStaticField(typeof(Config), "translations", Activator.CreateInstance(originalConfigTranslations.GetType()));
            SetStaticField(typeof(Config), "path", Path.Combine(missingConfigDirectoryPath, "config-smoke.conf"));
            string missingConfigValue = Config.Get("__phase2_config_get_write_failure__", "1");
            if (missingConfigValue != "1")
            {
                throw new Exception("Config.Get did not return the default value during a write failure");
            }
            if (!RuntimeStatus.NoAccess || !Program.noAccess)
            {
                throw new Exception("Config.Get write failure no longer toggles the shared no-access status");
            }

            Program.noAccess = false;
            RuntimeStatus.NoAccess = false;
            SetStaticField(typeof(Config), "translations", Activator.CreateInstance(originalConfigTranslations.GetType()));
            Config.initialize(invalidConfigInitializePath);
            if (!RuntimeStatus.NoAccess || !Program.noAccess)
            {
                throw new Exception("Config.initialize failure no longer toggles the shared no-access status");
            }

            Program.noAccess = false;
            RuntimeStatus.NoAccess = false;
            SetStaticField(typeof(Config), "translations", Activator.CreateInstance(originalConfigTranslations.GetType()));
            SetStaticField(typeof(Config), "path", Path.Combine(missingConfigDirectoryPath, "config-smoke.conf"));
            Config.Set("__phase2_config_write_failure__", "1");
            if (!RuntimeStatus.NoAccess || !Program.noAccess)
            {
                throw new Exception("Config.Set write failure no longer toggles the shared no-access status");
            }

            Program.noAccess = false;
            RuntimeStatus.NoAccess = false;
            SetStaticField(typeof(InterString), "translations", Activator.CreateInstance(originalInterStringTranslations.GetType()));
            InterString.initialize(invalidTranslationInitializePath);
            if (!RuntimeStatus.NoAccess || !Program.noAccess)
            {
                throw new Exception("InterString.initialize failure no longer toggles the shared no-access status");
            }

            Program.noAccess = false;
            RuntimeStatus.NoAccess = false;
            SetStaticField(typeof(InterString), "translations", Activator.CreateInstance(originalInterStringTranslations.GetType()));
            SetStaticField(typeof(InterString), "path", Path.Combine(missingTranslationDirectoryPath, "translation-smoke.conf"));
            InterString.Get("__phase2_interstring_write_failure__");
            if (!RuntimeStatus.NoAccess || !Program.noAccess)
            {
                throw new Exception("InterString.Get write failure no longer toggles the shared no-access status");
            }
        }
        finally
        {
            SetStaticField(typeof(Config), "translations", originalConfigTranslations);
            SetStaticField(typeof(Config), "uits", originalConfigUis);
            SetStaticField(typeof(Config), "path", originalConfigPath);
            SetStaticField(typeof(Config), "loaded", originalConfigLoaded);
            SetStaticField(typeof(InterString), "translations", originalInterStringTranslations);
            SetStaticField(typeof(InterString), "path", originalInterStringPath);
            SetStaticField(typeof(InterString), "loaded", originalInterStringLoaded);
            Program.noAccess = originalProgramNoAccess;
            RuntimeStatus.NoAccess = originalRuntimeNoAccess;
        }
    }

    private static void VerifyRuntimeTextureTools(Texture2D fieldTexture)
    {
        Texture2D[] sliced = RuntimeTextureTools.SliceField(fieldTexture);
        if (sliced == null || sliced.Length != 3)
        {
            throw new Exception("RuntimeTextureTools.SliceField did not return three slices");
        }

        for (int index = 0; index < sliced.Length; index++)
        {
            if (sliced[index] == null)
            {
                throw new Exception("RuntimeTextureTools.SliceField returned a null texture slice");
            }

            if (sliced[index].width <= 0 || sliced[index].height <= 0)
            {
                throw new Exception("RuntimeTextureTools.SliceField returned an empty texture slice");
            }
        }
    }

    private static Type InitializeCardData()
    {
        string translationPath = RuntimePaths.GetFilePath(RuntimeDirectory.Config, "translation.conf");
        string cardsPath = RuntimePaths.GetFilePath(RuntimeDirectory.Cdb, "cards.cdb");
        string banlistPath = RuntimePaths.GetFilePath(RuntimeDirectory.Config, "lflist.conf");

        if (!File.Exists(translationPath))
        {
            throw new Exception("config/translation.conf was not found through RuntimePaths");
        }

        if (!File.Exists(cardsPath))
        {
            throw new Exception("cdb/cards.cdb was not found through RuntimePaths");
        }

        if (!File.Exists(banlistPath))
        {
            throw new Exception("config/lflist.conf was not found through RuntimePaths");
        }

        InterString.initialize(translationPath);

        Type cardsManagerType = typeof(YGOSharp.BanlistManager).Assembly.GetType("YGOSharp.CardsManager");
        if (cardsManagerType == null)
        {
            throw new Exception("YGOSharp.CardsManager type was not found");
        }

        MethodInfo initializeMethod = cardsManagerType.GetMethod(
            "initialize",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(bool) },
            null);
        if (initializeMethod == null)
        {
            throw new Exception("YGOSharp.CardsManager.initialize(string, bool) was not found");
        }

        initializeMethod.Invoke(null, new object[] { cardsPath, false });
        YGOSharp.BanlistManager.initialize(banlistPath);

        return cardsManagerType;
    }

    private static IDictionary GetLoadedCards(Type cardsManagerType)
    {
        FieldInfo cardsField = cardsManagerType.GetField("_cards", BindingFlags.Static | BindingFlags.NonPublic);
        if (cardsField == null)
        {
            throw new Exception("YGOSharp.CardsManager._cards field was not found");
        }

        IDictionary loadedCards = cardsField.GetValue(null) as IDictionary;
        if (loadedCards == null || loadedCards.Count == 0)
        {
            throw new Exception("YGOSharp.CardsManager did not load any cards");
        }

        return loadedCards;
    }

    private static object GetStaticField(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new Exception(type.FullName + "." + fieldName + " field was not found");
        }

        return field.GetValue(null);
    }

    private static void SetStaticField(Type type, string fieldName, object value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new Exception(type.FullName + "." + fieldName + " field was not found");
        }

        field.SetValue(null, value);
    }

    private static void VerifyYgoSharpHookBehavior(IDictionary loadedCards, string deckPath)
    {
        AliasRestrictionCase restrictionCase = FindAliasRestrictionCase(loadedCards);
        int restrictedCopies = restrictionCase.MaxCopies + 1;
        List<int> fillerCardIds = FindUnrestrictedMainDeckCards(
            loadedCards,
            restrictionCase.Banlist,
            restrictionCase.AliasCode,
            restrictionCase.CardId,
            40 - restrictedCopies);

        YGOSharp.Card restrictedCard = YGOSharp.Card.Get(restrictionCase.CardId);
        if (restrictedCard == null)
        {
            throw new Exception("Card.Get returned null after CardsManager initialization");
        }

        if (restrictedCard.Alias != restrictionCase.AliasCode)
        {
            throw new Exception("Card.Get returned a card with an unexpected alias value");
        }

        WriteDeckFile(deckPath, restrictionCase.CardId, restrictedCopies, fillerCardIds);
        YGOSharp.Deck aliasDeck = new YGOSharp.Deck(deckPath);
        int invalidCode = aliasDeck.Check(restrictionCase.Banlist, true, true);
        if (invalidCode != restrictionCase.AliasCode)
        {
            throw new Exception("Alias-sensitive banlist validation did not return the aliased card code");
        }
    }

    private static void VerifyDeckParsingBehavior(IDictionary loadedCards, string deckPath)
    {
        DeckParsingCase deckParsingCase = FindDeckParsingCase(loadedCards);
        WriteDeckParsingFile(
            deckPath,
            deckParsingCase.MainSectionExtraCardId,
            deckParsingCase.ExtraSectionMainCardId,
            deckParsingCase.SideSectionCardId);

        YGOSharp.Deck declaredDeck = new YGOSharp.Deck(deckPath);
        if (declaredDeck.Main.Count != 1 || declaredDeck.Main[0] != deckParsingCase.MainSectionExtraCardId)
        {
            throw new Exception("YGOSharp.Deck no longer honors the declared #main section");
        }

        if (declaredDeck.Extra.Count != 1 || declaredDeck.Extra[0] != deckParsingCase.ExtraSectionMainCardId)
        {
            throw new Exception("YGOSharp.Deck no longer honors the declared #extra section");
        }

        if (declaredDeck.Side.Count != 1 || declaredDeck.Side[0] != deckParsingCase.SideSectionCardId)
        {
            throw new Exception("YGOSharp.Deck no longer honors the declared !side section");
        }

        YGOSharp.Deck correctedDeck;
        DeckManager.FromYDKtoCodedDeck(deckPath, out correctedDeck);
        if (correctedDeck.Main.Count != 1 || correctedDeck.Main[0] != deckParsingCase.ExtraSectionMainCardId)
        {
            throw new Exception("DeckManager.FromYDKtoCodedDeck no longer auto-corrects non-extra cards into Main");
        }

        if (correctedDeck.Extra.Count != 1 || correctedDeck.Extra[0] != deckParsingCase.MainSectionExtraCardId)
        {
            throw new Exception("DeckManager.FromYDKtoCodedDeck no longer auto-corrects extra cards into Extra");
        }

        if (correctedDeck.Side.Count != 1 || correctedDeck.Side[0] != deckParsingCase.SideSectionCardId)
        {
            throw new Exception("DeckManager.FromYDKtoCodedDeck no longer preserves !side declarations");
        }
    }

    private static AliasRestrictionCase FindAliasRestrictionCase(IDictionary loadedCards)
    {
        foreach (YGOSharp.Banlist banlist in YGOSharp.BanlistManager.Banlists)
        {
            foreach (DictionaryEntry entry in loadedCards)
            {
                YGOSharp.Card card = entry.Value as YGOSharp.Card;
                if (card == null || card.Alias == 0 || card.Alias == card.Id)
                {
                    continue;
                }

                int maxCopies = banlist.GetQuantity(card.Id);
                if (maxCopies >= 3)
                {
                    continue;
                }

                if (banlist.GetQuantity(card.Alias) != maxCopies)
                {
                    continue;
                }

                return new AliasRestrictionCase
                {
                    AliasCode = card.Alias,
                    CardId = card.Id,
                    MaxCopies = maxCopies,
                    Banlist = banlist,
                };
            }
        }

        throw new Exception("Could not find an alias-sensitive restricted card for smoke validation");
    }

    private static DeckParsingCase FindDeckParsingCase(IDictionary loadedCards)
    {
        YGOSharp.Card extraCard = null;
        YGOSharp.Card mainCard = null;

        foreach (DictionaryEntry entry in loadedCards)
        {
            YGOSharp.Card card = entry.Value as YGOSharp.Card;
            if (card == null || card.HasType(CardType.Token))
            {
                continue;
            }

            if (extraCard == null && card.IsExtraCard())
            {
                extraCard = card;
            }
            else if (mainCard == null && !card.IsExtraCard())
            {
                mainCard = card;
            }

            if (extraCard != null && mainCard != null)
            {
                return new DeckParsingCase
                {
                    MainSectionExtraCardId = extraCard.Id,
                    ExtraSectionMainCardId = mainCard.Id,
                    SideSectionCardId = extraCard.Id,
                };
            }
        }

        throw new Exception("Could not find representative main/extra cards for deck parsing smoke validation");
    }

    private static List<int> FindUnrestrictedMainDeckCards(
        IDictionary loadedCards,
        YGOSharp.Banlist banlist,
        int excludedAliasCode,
        int excludedCardId,
        int requiredCount)
    {
        List<int> fillerCardIds = new List<int>();

        foreach (DictionaryEntry entry in loadedCards)
        {
            YGOSharp.Card card = entry.Value as YGOSharp.Card;
            if (card == null || card.Id == excludedCardId || card.Id == excludedAliasCode)
            {
                continue;
            }

            if (card.Alias != 0)
            {
                continue;
            }

            if (card.HasType(CardType.Token) || card.IsExtraCard())
            {
                continue;
            }

            if (banlist.GetQuantity(card.Id) != 3)
            {
                continue;
            }

            fillerCardIds.Add(card.Id);
            if (fillerCardIds.Count == requiredCount)
            {
                return fillerCardIds;
            }
        }

        throw new Exception("Could not find enough unrestricted main-deck filler cards for smoke validation");
    }

    private static void WriteDeckFile(string path, int restrictedCardId, int restrictedCopies, IList<int> fillerCardIds)
    {
        if (fillerCardIds.Count != 40 - restrictedCopies)
        {
            throw new Exception("Smoke deck filler cards did not add up to 40 main-deck cards");
        }

        List<int> main = new List<int>();
        for (int i = 0; i < restrictedCopies; i++)
        {
            main.Add(restrictedCardId);
        }

        for (int i = 0; i < fillerCardIds.Count; i++)
        {
            main.Add(fillerCardIds[i]);
        }

        File.WriteAllText(path, SerializeDeckToYdk(main, new List<int>(), new List<int>()));
    }

    private static void WriteDeckParsingFile(
        string path,
        int mainSectionExtraCardId,
        int extraSectionMainCardId,
        int sideSectionCardId)
    {
        File.WriteAllText(
            path,
            SerializeDeckToYdk(
                new List<int> { mainSectionExtraCardId },
                new List<int> { extraSectionMainCardId },
                new List<int> { sideSectionCardId }));
    }

    private static string SerializeDeckToYdk(IList<int> main, IList<int> extra, IList<int> side)
    {
        MethodInfo serializerMethod = FindYdkDeckSerializerMethod();
        if (serializerMethod != null)
        {
            return (string)serializerMethod.Invoke(null, new object[] { main, extra, side });
        }

        return SerializeDeckToYdkLegacy(main, extra, side);
    }

    private static MethodInfo FindYdkDeckSerializerMethod()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Type serializerType = assemblies[assemblyIndex].GetType("YGOSharp.YdkDeckSerializer", false);
            if (serializerType == null)
            {
                continue;
            }

            MethodInfo method = serializerType.GetMethod(
                "Serialize",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(IList<int>), typeof(IList<int>), typeof(IList<int>) },
                null);
            if (method != null)
            {
                return method;
            }
        }

        return null;
    }

    private static string SerializeDeckToYdkLegacy(IList<int> main, IList<int> extra, IList<int> side)
    {
        string value = "#created by ygopro2\r\n#main\r\n";
        for (int i = 0; i < main.Count; i++)
        {
            value += main[i].ToString() + "\r\n";
        }

        value += "#extra\r\n";
        for (int i = 0; i < extra.Count; i++)
        {
            value += extra[i].ToString() + "\r\n";
        }

        value += "!side\r\n";
        for (int i = 0; i < side.Count; i++)
        {
            value += side[i].ToString() + "\r\n";
        }

        return value;
    }
}
