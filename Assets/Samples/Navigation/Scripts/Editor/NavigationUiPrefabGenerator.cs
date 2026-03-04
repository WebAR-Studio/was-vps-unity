#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NavigationUiPrefabGenerator
{
    private const string UiRootFolder = "Assets/Samples/Navigation/Prefabs/UI";
    private const string UiComponentsFolder = UiRootFolder + "/Components";
    private const string UiScreensFolder = UiRootFolder + "/Screens";
    private const string UiExportFolder = UiRootFolder + "/FigmaExport";

    private const string ActionButtonPrefabPath = UiComponentsFolder + "/NavActionButton.prefab";
    private const string SearchFieldPrefabPath = UiComponentsFolder + "/NavSearchField.prefab";
    private const string ListItemPrefabPath = UiComponentsFolder + "/NavListItemButton.prefab";
    private const string ScrollViewPrefabPath = UiComponentsFolder + "/NavScrollView.prefab";

    private const string MenuViewPrefabPath = UiScreensFolder + "/NavMenuView.prefab";
    private const string LocalizingViewPrefabPath = UiScreensFolder + "/NavLocalizingView.prefab";
    private const string DirectionHintViewPrefabPath = UiScreensFolder + "/NavDirectionHintView.prefab";
    private const string TripSummaryViewPrefabPath = UiScreensFolder + "/NavTripSummaryView.prefab";
    private const string ArrivalViewPrefabPath = UiScreensFolder + "/NavArrivalView.prefab";
    private const string AboutViewPrefabPath = UiScreensFolder + "/NavAboutView.prefab";

    private const string RootCanvasPrefabPath = UiRootFolder + "/NavigationUiCanvas.prefab";
    private const string BackgroundImagePath = UiExportFolder + "/bg_main.jpg";
    private const string MenuPanelBackgroundPath = UiExportFolder + "/menu_panel_bg.png";
    private const string SearchIconPath = UiExportFolder + "/icon_search_raster.png";
    private const string DefaultCategoryIconPath = UiExportFolder + "/icon_shops_raster.png";
    private const string FoodCategoryIconPath = UiExportFolder + "/icon_food_raster.png";
    private const string EntertainmentCategoryIconPath = UiExportFolder + "/icon_entertainment_raster.png";
    private const string KidsCategoryIconPath = UiExportFolder + "/icon_kids_raster.png";
    private const string ServicesCategoryIconPath = UiExportFolder + "/icon_services_raster.png";
    private const string ChevronIconPath = UiExportFolder + "/icon_chevron_raster.png";
    private const string SeparatorPath = UiExportFolder + "/separator_raster.png";
    private const string RoundedPanelPath = UiExportFolder + "/rounded_panel_16.png";
    private const string RoundedPanelSmallPath = UiExportFolder + "/rounded_panel_12.png";
    private const string RoundedPanelLargePath = UiExportFolder + "/rounded_panel_40.png";
    private const string TurnRightIconPath = UiExportFolder + "/icon_turn_right_raster.png";
    private const string LocalizingHintText = "Rotate your phone and pop all the balloons around you to start!";

    [MenuItem("Samples/Navigation/Generate Navigation UI Prefabs")]
    public static void Generate()
    {
        EnsureFolder(UiRootFolder);
        EnsureFolder(UiComponentsFolder);
        EnsureFolder(UiScreensFolder);

        ConfigureSpriteImport(BackgroundImagePath);
        ConfigureSpriteImport(MenuPanelBackgroundPath);
        ConfigureSpriteImport(SearchIconPath);
        ConfigureSpriteImport(DefaultCategoryIconPath);
        ConfigureSpriteImport(FoodCategoryIconPath);
        ConfigureSpriteImport(EntertainmentCategoryIconPath);
        ConfigureSpriteImport(KidsCategoryIconPath);
        ConfigureSpriteImport(ServicesCategoryIconPath);
        ConfigureSpriteImport(ChevronIconPath);
        ConfigureSpriteImport(SeparatorPath);
        ConfigureSpriteImport(RoundedPanelPath);
        ConfigureSpriteImport(RoundedPanelSmallPath);
        ConfigureSpriteImport(RoundedPanelLargePath);
        ConfigureSpriteImport(TurnRightIconPath);

        var font = LoadBuiltinFont();
        if (font == null)
        {
            Debug.LogError("NavigationUiPrefabGenerator: built-in UI font was not found.");
            return;
        }

        var assets = new GeneratedAssets();
        assets.BackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundImagePath);
        assets.MenuPanelBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MenuPanelBackgroundPath);
        assets.SearchIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SearchIconPath);
        assets.DefaultCategoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultCategoryIconPath);
        assets.FoodCategoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FoodCategoryIconPath);
        assets.EntertainmentCategoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EntertainmentCategoryIconPath);
        assets.KidsCategoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KidsCategoryIconPath);
        assets.ServicesCategoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ServicesCategoryIconPath);
        assets.ChevronIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChevronIconPath);
        assets.SeparatorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SeparatorPath);
        assets.RoundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelPath);
        assets.RoundedPanelSmallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSmallPath);
        assets.RoundedPanelLargeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelLargePath);
        assets.TurnRightIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TurnRightIconPath);

        assets.ActionButtonPrefab = GenerateActionButton(font);
        assets.SearchFieldPrefab = GenerateSearchField(font, assets.SearchIconSprite);
        assets.ListItemPrefab = GenerateListItem(font, assets.DefaultCategoryIconSprite, assets.ChevronIconSprite, assets.SeparatorSprite);
        assets.ScrollViewPrefab = GenerateScrollView(assets.RoundedPanelSprite);

        assets.MenuViewPrefab = GenerateMenuView(font, assets);
        assets.LocalizingViewPrefab = GenerateLocalizingView(font, assets.RoundedPanelSprite);
        assets.DirectionHintViewPrefab = GenerateDirectionHintView(font, assets.RoundedPanelSmallSprite, assets.TurnRightIconSprite);
        assets.TripSummaryViewPrefab = GenerateTripSummaryView(font, assets.RoundedPanelLargeSprite, assets.RoundedPanelSmallSprite);
        assets.ArrivalViewPrefab = GenerateArrivalView(font, assets.RoundedPanelLargeSprite);
        assets.AboutViewPrefab = GenerateAboutView(font, assets.MenuPanelBackgroundSprite, assets.RoundedPanelLargeSprite, assets.RoundedPanelSmallSprite);

        GenerateRootCanvas(font, assets);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("NavigationUiPrefabGenerator: UI prefabs generated in Assets/Samples/Navigation/Prefabs/UI.");
    }

    public static void GenerateFromCommandLine()
    {
        Generate();
    }

    private static Font LoadBuiltinFont()
    {
        // Unity 2022+ uses LegacyRuntime.ttf, while older versions still expose Arial.ttf.
        var legacyRuntime = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (legacyRuntime != null)
        {
            return legacyRuntime;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static GameObject GenerateActionButton(Font font)
    {
        var root = CreateUiNode("NavActionButton", new Vector2(329f, 52f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.22f);

        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var label = CreateText(root.transform, "Label", font, "Button", 18, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.raycastTarget = false;

        return SavePrefab(root, ActionButtonPrefabPath);
    }

    private static GameObject GenerateSearchField(Font font, Sprite searchIcon)
    {
        var root = CreateUiNode("NavSearchField", new Vector2(328f, 52f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.22f);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (searchIcon != null)
        {
            var iconNode = CreateUiNode("Icon", new Vector2(20f, 20f), root.transform);
            var iconImage = iconNode.AddComponent<Image>();
            iconImage.sprite = searchIcon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            var iconLayout = iconNode.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.preferredHeight = 20f;
            iconLayout.minWidth = 20f;
            iconLayout.minHeight = 20f;
        }
        else
        {
            var icon = CreateText(root.transform, "Icon", font, "\u2315", 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.95f));
            icon.raycastTarget = false;
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.minWidth = 20f;
        }

        var placeholder = CreateText(root.transform, "Placeholder", font, "Поиск", 14, TextAnchor.MiddleLeft, Color.white);
        placeholder.raycastTarget = false;
        var placeholderLayout = placeholder.gameObject.AddComponent<LayoutElement>();
        placeholderLayout.flexibleWidth = 1f;

        return SavePrefab(root, SearchFieldPrefabPath);
    }

    private static GameObject GenerateListItem(Font font, Sprite categoryIcon, Sprite chevronIcon, Sprite separatorSprite)
    {
        var root = CreateUiNode("NavListItemButton", new Vector2(329f, 52f));

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 52f;

        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (categoryIcon != null)
        {
            var iconHolder = CreateUiNode("Icon", new Vector2(32f, 32f), root.transform);
            var iconLayout = iconHolder.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 32f;
            iconLayout.preferredHeight = 32f;
            iconLayout.minWidth = 32f;
            iconLayout.minHeight = 32f;

            var iconSpriteNode = CreateUiNode("Sprite", new Vector2(20f, 20f), iconHolder.transform);
            var iconSpriteRt = iconSpriteNode.GetComponent<RectTransform>();
            Stretch(iconSpriteRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            var iconImage = iconSpriteNode.AddComponent<Image>();
            iconImage.sprite = categoryIcon;
            iconImage.preserveAspect = true;
            iconImage.color = new Color(0.65f, 0.88f, 1f, 1f);
            iconImage.raycastTarget = false;
        }
        else
        {
            var iconHolder = CreateUiNode("Icon", new Vector2(32f, 32f), root.transform);
            var iconLayout = iconHolder.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 32f;
            iconLayout.preferredHeight = 32f;
            iconLayout.minWidth = 32f;
            iconLayout.minHeight = 32f;

            var icon = CreateText(iconHolder.transform, "Sprite", font, "\u25CE", 16, TextAnchor.MiddleCenter, new Color(0.65f, 0.88f, 1f, 1f));
            Stretch(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            icon.raycastTarget = false;
        }

        var label = CreateText(root.transform, "Label", font, "Point", 14, TextAnchor.MiddleLeft, Color.white);
        label.raycastTarget = false;
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        if (chevronIcon != null)
        {
            var chevronHolder = CreateUiNode("Chevron", new Vector2(32f, 32f), root.transform);
            var chevronLayout = chevronHolder.AddComponent<LayoutElement>();
            chevronLayout.preferredWidth = 32f;
            chevronLayout.preferredHeight = 32f;
            chevronLayout.minWidth = 32f;
            chevronLayout.minHeight = 32f;

            var chevronNode = CreateUiNode("Sprite", new Vector2(20f, 20f), chevronHolder.transform);
            var chevronRt = chevronNode.GetComponent<RectTransform>();
            Stretch(chevronRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            var chevronImage = chevronNode.AddComponent<Image>();
            chevronImage.sprite = chevronIcon;
            chevronImage.preserveAspect = true;
            chevronImage.color = Color.white;
            chevronImage.raycastTarget = false;
        }
        else
        {
            var chevronHolder = CreateUiNode("Chevron", new Vector2(32f, 32f), root.transform);
            var chevronLayout = chevronHolder.AddComponent<LayoutElement>();
            chevronLayout.preferredWidth = 32f;
            chevronLayout.preferredHeight = 32f;
            chevronLayout.minWidth = 32f;
            chevronLayout.minHeight = 32f;

            var chevron = CreateText(chevronHolder.transform, "Sprite", font, "\u203A", 24, TextAnchor.MiddleCenter, Color.white);
            Stretch(chevron.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            chevron.raycastTarget = false;
        }

        var separator = CreateUiNode("Separator", Vector2.zero, root.transform);
        var separatorRt = separator.GetComponent<RectTransform>();
        var separatorImage = separator.AddComponent<Image>();
        separatorImage.color = new Color(1f, 1f, 1f, 0.2f);
        if (separatorSprite != null)
        {
            separatorImage.sprite = separatorSprite;
            separatorImage.type = Image.Type.Simple;
            separatorImage.color = Color.white;
        }
        separatorImage.raycastTarget = false;

        var separatorLayout = separator.AddComponent<LayoutElement>();
        separatorLayout.ignoreLayout = true;

        // Keep separator pinned to the bottom edge and excluded from horizontal row layout.
        Stretch(separatorRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));

        return SavePrefab(root, ListItemPrefabPath);
    }

    private static GameObject GenerateScrollView(Sprite roundedPanelSprite)
    {
        var root = CreateUiNode("NavScrollView", new Vector2(329f, 300f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.2f);
        if (roundedPanelSprite != null)
        {
            bg.sprite = roundedPanelSprite;
            bg.type = Image.Type.Sliced;
        }

        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 20f;

        var viewport = CreateUiNode("Viewport", Vector2.zero, root.transform);
        var viewportRt = viewport.GetComponent<RectTransform>();
        Stretch(viewportRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        if (roundedPanelSprite != null)
        {
            viewportImage.sprite = roundedPanelSprite;
            viewportImage.type = Image.Type.Sliced;
        }
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = CreateUiNode("Content", Vector2.zero, viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        Stretch(contentRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        var verticalLayout = content.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
        verticalLayout.spacing = 0f;
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        return SavePrefab(root, ScrollViewPrefabPath);
    }

    private static GameObject GenerateMenuView(Font font, GeneratedAssets assets)
    {
        var root = CreateUiNode("MenuView", new Vector2(393f, 482f));
        var bg = root.AddComponent<Image>();
        if (assets.MenuPanelBackgroundSprite != null)
        {
            bg.sprite = assets.MenuPanelBackgroundSprite;
            bg.color = Color.white;
            bg.preserveAspect = false;
        }
        else
        {
            bg.color = new Color(0f, 0f, 0f, 0.32f);
        }

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText(root.transform, "Title", font, "Куда пойдём?", 32, TextAnchor.UpperLeft, Color.white);
        title.raycastTarget = false;
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40f;

        var categories = CreateUiNode("CategoriesPanel", new Vector2(329f, 316f), root.transform);
        var categoriesImage = categories.AddComponent<Image>();
        categoriesImage.color = Color.clear;
        categoriesImage.raycastTarget = false;

        var categoriesLayout = categories.AddComponent<LayoutElement>();
        categoriesLayout.minHeight = 240f;
        categoriesLayout.preferredHeight = 362f;
        categoriesLayout.flexibleHeight = 1f;

        var scrollView = InstantiatePrefab(assets.ScrollViewPrefab, categories.transform, "PointsScrollView");
        var scrollRt = scrollView.GetComponent<RectTransform>();
        Stretch(scrollRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var menuView = root.AddComponent<NavMenuView>();
        SetMenuViewReferences(menuView, assets.ListItemPrefab);
        return SavePrefab(root, MenuViewPrefabPath);
    }

    private static GameObject GenerateLocalizingView(Font font, Sprite roundedPanelSprite)
    {
        var root = CreateUiNode("NavLocalizingView", new Vector2(322f, 58f));

        var modalBg = root.AddComponent<Image>();
        modalBg.color = new Color(0f, 0f, 0f, 0.32f);
        modalBg.raycastTarget = false;
        if (roundedPanelSprite != null)
        {
            modalBg.sprite = roundedPanelSprite;
            modalBg.type = Image.Type.Sliced;
        }

        var label = CreateText(root.transform, "LocalizingText", font, LocalizingHintText, 16, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32f, -16f));
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        root.AddComponent<NavLocalizingView>();
        return SavePrefab(root, LocalizingViewPrefabPath);
    }

    private static GameObject GenerateDirectionHintView(Font font, Sprite roundedPanelSmallSprite, Sprite turnRightIcon)
    {
        var root = CreateUiNode("NavDirectionHintView", new Vector2(305f, 78f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.2f);
        bg.raycastTarget = false;
        if (roundedPanelSmallSprite != null)
        {
            bg.sprite = roundedPanelSmallSprite;
            bg.type = Image.Type.Sliced;
        }

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconRoot = CreateUiNode("DirectionIcon", new Vector2(48f, 48f), root.transform);
        var iconLayout = iconRoot.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 48f;
        iconLayout.preferredHeight = 48f;
        iconLayout.minWidth = 48f;
        iconLayout.minHeight = 48f;

        if (turnRightIcon != null)
        {
            var icon = CreateUiNode("Sprite", new Vector2(48f, 48f), iconRoot.transform);
            var iconRt = icon.GetComponent<RectTransform>();
            Stretch(iconRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = turnRightIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        else
        {
            var fallback = CreateText(iconRoot.transform, "Sprite", font, "\u21B1", 32, TextAnchor.MiddleCenter, new Color(0.3f, 0.45f, 0.96f, 1f));
            Stretch(fallback.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fallback.raycastTarget = false;
        }

        var textRoot = CreateUiNode("TextGroup", Vector2.zero, root.transform);
        var textLayout = textRoot.AddComponent<VerticalLayoutGroup>();
        textLayout.padding = new RectOffset(0, 0, 0, 0);
        textLayout.spacing = -2f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = false;
        textLayout.childForceExpandHeight = false;

        var textLayoutElement = textRoot.AddComponent<LayoutElement>();
        textLayoutElement.flexibleWidth = 1f;

        var title = CreateText(textRoot.transform, "TitleText", font, "Поверните направо", 20, TextAnchor.MiddleLeft, Color.white);
        title.raycastTarget = false;
        var titleElement = title.gameObject.AddComponent<LayoutElement>();
        titleElement.preferredHeight = 32f;

        var distance = CreateText(textRoot.transform, "DistanceText", font, "5м", 14, TextAnchor.MiddleLeft, Color.white);
        distance.raycastTarget = false;
        var distanceElement = distance.gameObject.AddComponent<LayoutElement>();
        distanceElement.preferredHeight = 24f;

        return SavePrefab(root, DirectionHintViewPrefabPath);
    }

    private static GameObject GenerateTripSummaryView(Font font, Sprite roundedPanelLargeSprite, Sprite roundedPanelSmallSprite)
    {
        var root = CreateUiNode("NavTripSummaryView", new Vector2(393f, 110f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.32f);
        if (roundedPanelLargeSprite != null)
        {
            bg.sprite = roundedPanelLargeSprite;
            bg.type = Image.Type.Sliced;
        }

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 26, 26);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var closeButtonNode = CreateUiNode("CloseButton", new Vector2(48f, 48f), root.transform);
        var closeButtonLayout = closeButtonNode.AddComponent<LayoutElement>();
        closeButtonLayout.preferredWidth = 48f;
        closeButtonLayout.preferredHeight = 48f;
        closeButtonLayout.minWidth = 48f;
        closeButtonLayout.minHeight = 48f;

        var closeBg = closeButtonNode.AddComponent<Image>();
        closeBg.color = new Color(0f, 0f, 0f, 0.2f);
        if (roundedPanelSmallSprite != null)
        {
            closeBg.sprite = roundedPanelSmallSprite;
            closeBg.type = Image.Type.Sliced;
        }

        var closeButton = closeButtonNode.AddComponent<Button>();
        closeButton.targetGraphic = closeBg;

        var closeLabel = CreateText(closeButtonNode.transform, "Icon", font, "\u00D7", 26, TextAnchor.MiddleCenter, new Color(0.9647059f, 0.5686275f, 0.5686275f, 1f));
        Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        closeLabel.raycastTarget = false;

        var textGroup = CreateUiNode("TextGroup", Vector2.zero, root.transform);
        var textGroupLayout = textGroup.AddComponent<VerticalLayoutGroup>();
        textGroupLayout.padding = new RectOffset(0, 0, 0, 0);
        textGroupLayout.spacing = 4f;
        textGroupLayout.childAlignment = TextAnchor.UpperCenter;
        textGroupLayout.childControlWidth = true;
        textGroupLayout.childControlHeight = true;
        textGroupLayout.childForceExpandWidth = false;
        textGroupLayout.childForceExpandHeight = false;

        var textGroupElement = textGroup.AddComponent<LayoutElement>();
        textGroupElement.preferredWidth = 225f;
        textGroupElement.preferredHeight = 58f;

        var metricsRow = CreateUiNode("MetricsRow", Vector2.zero, textGroup.transform);
        var metricsLayout = metricsRow.AddComponent<HorizontalLayoutGroup>();
        metricsLayout.padding = new RectOffset(0, 0, 0, 0);
        metricsLayout.spacing = 16f;
        metricsLayout.childAlignment = TextAnchor.MiddleCenter;
        metricsLayout.childControlWidth = true;
        metricsLayout.childControlHeight = true;
        metricsLayout.childForceExpandWidth = false;
        metricsLayout.childForceExpandHeight = false;

        var metricsElement = metricsRow.AddComponent<LayoutElement>();
        metricsElement.preferredWidth = 142f;
        metricsElement.preferredHeight = 30f;

        var duration = CreateText(metricsRow.transform, "DurationText", font, "5 min", 24, TextAnchor.MiddleCenter, Color.white);
        duration.fontStyle = FontStyle.Bold;
        duration.raycastTarget = false;
        var durationElement = duration.gameObject.AddComponent<LayoutElement>();
        durationElement.preferredWidth = 72f;
        durationElement.preferredHeight = 30f;

        var distance = CreateText(metricsRow.transform, "DistanceText", font, "10 m", 24, TextAnchor.MiddleCenter, Color.white);
        distance.fontStyle = FontStyle.Bold;
        distance.raycastTarget = false;
        var distanceElement = distance.gameObject.AddComponent<LayoutElement>();
        distanceElement.preferredWidth = 54f;
        distanceElement.preferredHeight = 30f;

        var place = CreateText(textGroup.transform, "PlaceText", font, "12 STOREEZ", 14, TextAnchor.MiddleCenter, new Color(0.64705884f, 0.88235295f, 1f, 1f));
        place.fontStyle = FontStyle.Bold;
        place.raycastTarget = false;
        var placeElement = place.gameObject.AddComponent<LayoutElement>();
        placeElement.preferredWidth = 71f;
        placeElement.preferredHeight = 24f;

        root.AddComponent<NavTripSummaryView>();
        return SavePrefab(root, TripSummaryViewPrefabPath);
    }

    private static GameObject GenerateArrivalView(Font font, Sprite roundedPanelLargeSprite)
    {
        var root = CreateUiNode("NavArrivalView", new Vector2(393f, 180f));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.32f);
        if (roundedPanelLargeSprite != null)
        {
            bg.sprite = roundedPanelLargeSprite;
            bg.type = Image.Type.Sliced;
        }

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 25, 25);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var textGroup = CreateUiNode("TextGroup", Vector2.zero, root.transform);
        var textGroupLayout = textGroup.AddComponent<VerticalLayoutGroup>();
        textGroupLayout.padding = new RectOffset(0, 0, 0, 0);
        textGroupLayout.spacing = 4f;
        textGroupLayout.childAlignment = TextAnchor.UpperCenter;
        textGroupLayout.childControlWidth = true;
        textGroupLayout.childControlHeight = true;
        textGroupLayout.childForceExpandWidth = false;
        textGroupLayout.childForceExpandHeight = false;

        var textGroupElement = textGroup.AddComponent<LayoutElement>();
        textGroupElement.preferredWidth = 353f;
        textGroupElement.preferredHeight = 58f;

        var title = CreateText(textGroup.transform, "ArrivalTitleText", font, "You have arrived", 24, TextAnchor.MiddleCenter, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 150f;
        titleLayout.preferredHeight = 30f;

        var subtitle = CreateText(textGroup.transform, "ArrivalLocationText", font, "12 STOREEZ", 14, TextAnchor.MiddleCenter, new Color(0.64705884f, 0.88235295f, 1f, 1f));
        subtitle.fontStyle = FontStyle.Bold;
        subtitle.raycastTarget = false;
        var subtitleLayout = subtitle.gameObject.AddComponent<LayoutElement>();
        subtitleLayout.preferredWidth = 71f;
        subtitleLayout.preferredHeight = 24f;

        var buttonsRow = CreateUiNode("ButtonsRow", Vector2.zero, root.transform);
        var buttonsLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.padding = new RectOffset(0, 0, 0, 0);
        buttonsLayout.spacing = 16f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;

        var buttonsRowElement = buttonsRow.AddComponent<LayoutElement>();
        buttonsRowElement.preferredWidth = 353f;
        buttonsRowElement.preferredHeight = 56f;

        var aboutButtonNode = CreateUiNode("AboutLocationButton", new Vector2(168.5f, 56f), buttonsRow.transform);
        var aboutButtonLayout = aboutButtonNode.AddComponent<LayoutElement>();
        aboutButtonLayout.preferredWidth = 168.5f;
        aboutButtonLayout.preferredHeight = 56f;
        aboutButtonLayout.minWidth = 168.5f;
        aboutButtonLayout.minHeight = 56f;

        var aboutButtonImage = aboutButtonNode.AddComponent<Image>();
        aboutButtonImage.color = new Color(0f, 0f, 0f, 0.2f);
        if (roundedPanelLargeSprite != null)
        {
            aboutButtonImage.sprite = roundedPanelLargeSprite;
            aboutButtonImage.type = Image.Type.Sliced;
        }

        var aboutButton = aboutButtonNode.AddComponent<Button>();
        aboutButton.targetGraphic = aboutButtonImage;

        var aboutLabel = CreateText(aboutButtonNode.transform, "Label", font, "About the location", 14, TextAnchor.MiddleCenter, Color.white);
        aboutLabel.fontStyle = FontStyle.Bold;
        aboutLabel.raycastTarget = false;
        Stretch(aboutLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var finishButtonNode = CreateUiNode("FinishButton", new Vector2(168.5f, 56f), buttonsRow.transform);
        var finishButtonLayout = finishButtonNode.AddComponent<LayoutElement>();
        finishButtonLayout.preferredWidth = 168.5f;
        finishButtonLayout.preferredHeight = 56f;
        finishButtonLayout.minWidth = 168.5f;
        finishButtonLayout.minHeight = 56f;

        var finishButtonImage = finishButtonNode.AddComponent<Image>();
        finishButtonImage.color = new Color(0.07450981f, 0.83137256f, 0.69411767f, 0.5f);
        if (roundedPanelLargeSprite != null)
        {
            finishButtonImage.sprite = roundedPanelLargeSprite;
            finishButtonImage.type = Image.Type.Sliced;
        }

        var finishButton = finishButtonNode.AddComponent<Button>();
        finishButton.targetGraphic = finishButtonImage;

        var finishLabel = CreateText(finishButtonNode.transform, "Label", font, "Finish", 14, TextAnchor.MiddleCenter, Color.white);
        finishLabel.fontStyle = FontStyle.Bold;
        finishLabel.raycastTarget = false;
        Stretch(finishLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        root.AddComponent<NavArrivalView>();
        return SavePrefab(root, ArrivalViewPrefabPath);
    }

    private static GameObject GenerateAboutView(Font font, Sprite menuPanelBackgroundSprite, Sprite roundedPanelLargeSprite, Sprite roundedPanelSmallSprite)
    {
        var root = CreateUiNode("NavAboutView", new Vector2(393f, 634f));
        var bg = root.AddComponent<Image>();
        bg.color = Color.white;
        if (menuPanelBackgroundSprite != null)
        {
            bg.sprite = menuPanelBackgroundSprite;
            bg.type = Image.Type.Simple;
        }

        var overlay = CreateUiNode("Overlay", Vector2.zero, root.transform);
        var overlayRt = overlay.GetComponent<RectTransform>();
        Stretch(overlayRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.32f);
        overlayImage.raycastTarget = false;
        if (roundedPanelLargeSprite != null)
        {
            overlayImage.sprite = roundedPanelLargeSprite;
            overlayImage.type = Image.Type.Sliced;
        }

        var content = CreateUiNode("Content", new Vector2(328f, 568f), root.transform);
        var contentRt = content.GetComponent<RectTransform>();
        Stretch(contentRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -40f), new Vector2(328f, 568f));

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 16f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;

        var info = CreateUiNode("Info", new Vector2(328f, 118f), content.transform);
        var infoLayout = info.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(0, 0, 0, 0);
        infoLayout.spacing = 8f;
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = false;
        infoLayout.childForceExpandHeight = false;
        var infoLayoutElement = info.AddComponent<LayoutElement>();
        infoLayoutElement.preferredWidth = 328f;
        infoLayoutElement.preferredHeight = 118f;

        var title = CreateText(info.transform, "AboutTitleText", font, "12 STOREEZ", 24, TextAnchor.UpperLeft, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;
        var titleElement = title.gameObject.AddComponent<LayoutElement>();
        titleElement.preferredWidth = 328f;
        titleElement.preferredHeight = 30f;

        var location = CreateText(info.transform, "AboutLocationText", font, "Floor 3, sector 23", 14, TextAnchor.UpperLeft, new Color(0.64705884f, 0.88235295f, 1f, 1f));
        location.raycastTarget = false;
        var locationElement = location.gameObject.AddComponent<LayoutElement>();
        locationElement.preferredWidth = 328f;
        locationElement.preferredHeight = 24f;

        var categories = CreateText(info.transform, "AboutCategoriesText", font, "Aviapark Mall, Khodynsky Blvd 4, Moscow", 12, TextAnchor.UpperLeft, new Color(0.7058824f, 0.7176471f, 0.73333335f, 1f));
        categories.raycastTarget = false;
        categories.horizontalOverflow = HorizontalWrapMode.Wrap;
        categories.verticalOverflow = VerticalWrapMode.Truncate;
        var categoriesElement = categories.gameObject.AddComponent<LayoutElement>();
        categoriesElement.preferredWidth = 328f;
        categoriesElement.preferredHeight = 48f;

        var scheduleRow = CreateUiNode("ScheduleRow", new Vector2(328f, 32f), content.transform);
        var scheduleRowLayout = scheduleRow.AddComponent<HorizontalLayoutGroup>();
        scheduleRowLayout.padding = new RectOffset(0, 0, 0, 0);
        scheduleRowLayout.spacing = 0f;
        scheduleRowLayout.childAlignment = TextAnchor.MiddleRight;
        scheduleRowLayout.childControlWidth = true;
        scheduleRowLayout.childControlHeight = true;
        scheduleRowLayout.childForceExpandWidth = false;
        scheduleRowLayout.childForceExpandHeight = false;
        var scheduleRowElement = scheduleRow.AddComponent<LayoutElement>();
        scheduleRowElement.preferredWidth = 328f;
        scheduleRowElement.preferredHeight = 32f;

        var scheduleBadge = CreateUiNode("ScheduleBadge", new Vector2(129f, 32f), scheduleRow.transform);
        var scheduleBadgeElement = scheduleBadge.AddComponent<LayoutElement>();
        scheduleBadgeElement.preferredWidth = 129f;
        scheduleBadgeElement.preferredHeight = 32f;
        scheduleBadgeElement.minWidth = 129f;
        scheduleBadgeElement.minHeight = 32f;

        var scheduleBadgeImage = scheduleBadge.AddComponent<Image>();
        scheduleBadgeImage.color = new Color(0f, 0f, 0f, 0.2f);
        if (roundedPanelSmallSprite != null)
        {
            scheduleBadgeImage.sprite = roundedPanelSmallSprite;
            scheduleBadgeImage.type = Image.Type.Sliced;
        }
        scheduleBadgeImage.raycastTarget = false;

        var scheduleBadgeLayout = scheduleBadge.AddComponent<HorizontalLayoutGroup>();
        scheduleBadgeLayout.padding = new RectOffset(12, 12, 4, 4);
        scheduleBadgeLayout.spacing = 8f;
        scheduleBadgeLayout.childAlignment = TextAnchor.MiddleCenter;
        scheduleBadgeLayout.childControlWidth = true;
        scheduleBadgeLayout.childControlHeight = true;
        scheduleBadgeLayout.childForceExpandWidth = false;
        scheduleBadgeLayout.childForceExpandHeight = false;

        var clockIcon = CreateText(scheduleBadge.transform, "Icon", font, "◷", 14, TextAnchor.MiddleCenter, Color.white);
        clockIcon.raycastTarget = false;
        var clockElement = clockIcon.gameObject.AddComponent<LayoutElement>();
        clockElement.preferredWidth = 20f;
        clockElement.preferredHeight = 20f;

        var scheduleText = CreateText(scheduleBadge.transform, "TimeText", font, "10:00-22:00", 14, TextAnchor.MiddleCenter, Color.white);
        scheduleText.raycastTarget = false;
        var scheduleTextElement = scheduleText.gameObject.AddComponent<LayoutElement>();
        scheduleTextElement.preferredHeight = 24f;

        var descriptionSection = CreateUiNode("DescriptionSection", new Vector2(328f, 386f), content.transform);
        var descriptionSectionElement = descriptionSection.AddComponent<LayoutElement>();
        descriptionSectionElement.preferredWidth = 328f;
        descriptionSectionElement.preferredHeight = 386f;
        descriptionSectionElement.flexibleHeight = 1f;

        var description = CreateText(
            descriptionSection.transform,
            "AboutDescriptionText",
            font,
            "Floor 3, sector 23.\n\nMinimal wardrobe for men and women.\n\n12 STOREEZ is a Russian clothing brand founded by twin sisters Irina and Marina Golomazdina and Ivan Khokhlov. The brand releases 12 stories each year - mini collections where all items are easy to combine.\n\nThe idea is to build a thoughtful long-lasting wardrobe and refresh it 1-2 times per season with relevant pieces.",
            14,
            TextAnchor.UpperLeft,
            Color.white);
        description.raycastTarget = false;
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        description.verticalOverflow = VerticalWrapMode.Overflow;
        var descriptionRt = description.rectTransform;
        Stretch(descriptionRt, Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);

        var closeButton = CreateUiNode("AboutCloseButton", new Vector2(24f, 24f), root.transform);
        var closeButtonRt = closeButton.GetComponent<RectTransform>();
        Stretch(closeButtonRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-32f, -32f), new Vector2(24f, 24f));
        var closeButtonImage = closeButton.AddComponent<Image>();
        closeButtonImage.color = new Color(1f, 1f, 1f, 0f);
        var closeButtonComponent = closeButton.AddComponent<Button>();
        closeButtonComponent.targetGraphic = closeButtonImage;

        var closeIcon = CreateText(closeButton.transform, "Icon", font, "\u00D7", 24, TextAnchor.MiddleCenter, Color.white);
        closeIcon.raycastTarget = false;
        Stretch(closeIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        root.AddComponent<NavAboutView>();
        return SavePrefab(root, AboutViewPrefabPath);
    }

    private static void GenerateRootCanvas(Font font, GeneratedAssets assets)
    {
        var root = CreateUiNode("NavigationUiCanvas", new Vector2(393f, 852f));

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<GraphicRaycaster>();

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(393f, 852f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var controller = root.AddComponent<NavigationRouteUiController>();

        var background = CreateUiNode("Background", Vector2.zero, root.transform);
        var backgroundRt = background.GetComponent<RectTransform>();
        Stretch(backgroundRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.white;
        backgroundImage.raycastTarget = false;
        backgroundImage.sprite = assets.BackgroundSprite;
        backgroundImage.preserveAspect = true;

        var menuView = InstantiatePrefab(assets.MenuViewPrefab, root.transform, "MenuView");
        var menuRt = menuView.GetComponent<RectTransform>();
        Stretch(menuRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 482f));

        var localizingView = InstantiatePrefab(assets.LocalizingViewPrefab, root.transform, "LocalizingView");
        var localizingRt = localizingView.GetComponent<RectTransform>();
        Stretch(localizingRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(322f, 58f));
        localizingView.SetActive(false);

        var directionHintView = InstantiatePrefab(assets.DirectionHintViewPrefab, root.transform, "DirectionHintView");
        var directionHintRt = directionHintView.GetComponent<RectTransform>();
        Stretch(directionHintRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(305f, 78f));
        directionHintView.SetActive(false);

        var tripSummaryView = InstantiatePrefab(assets.TripSummaryViewPrefab, root.transform, "TripSummaryView");
        var tripSummaryRt = tripSummaryView.GetComponent<RectTransform>();
        Stretch(tripSummaryRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 110f));
        tripSummaryView.SetActive(false);

        var arrivalView = InstantiatePrefab(assets.ArrivalViewPrefab, root.transform, "ArrivalActionsView");
        var arrivalRt = arrivalView.GetComponent<RectTransform>();
        Stretch(arrivalRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 180f));
        arrivalView.SetActive(false);

        var aboutView = InstantiatePrefab(assets.AboutViewPrefab, root.transform, "AboutView");
        var aboutRt = aboutView.GetComponent<RectTransform>();
        Stretch(aboutRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 634f));
        aboutView.SetActive(false);

        SetControllerReferences(
            controller,
            menuView,
            localizingView,
            tripSummaryView,
            arrivalView,
            aboutView);

        SavePrefab(root, RootCanvasPrefabPath);
    }

    private static void SetControllerReferences(
        NavigationRouteUiController controller,
        GameObject menuView,
        GameObject localizingView,
        GameObject tripSummaryView,
        GameObject arrivalView,
        GameObject aboutView)
    {
        var so = new SerializedObject(controller);

        so.FindProperty("_menuView").objectReferenceValue = menuView;
        so.FindProperty("_localizingView").objectReferenceValue = localizingView;
        so.FindProperty("_tripSummaryView").objectReferenceValue = tripSummaryView;
        so.FindProperty("_arrivalActionsView").objectReferenceValue = arrivalView;
        so.FindProperty("_aboutView").objectReferenceValue = aboutView;

        so.FindProperty("_forceMenuOnEnable").boolValue = true;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMenuViewReferences(NavMenuView menuView, GameObject pointButtonPrefab)
    {
        if (menuView == null)
        {
            return;
        }

        var so = new SerializedObject(menuView);
        var pointButtonComponent = pointButtonPrefab != null ? pointButtonPrefab.GetComponent<Button>() : null;
        so.FindProperty("_pointButtonPrefab").objectReferenceValue = pointButtonComponent;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject InstantiatePrefab(GameObject prefabAsset, Transform parent, string newName)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        instance.name = newName;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void ConfigureSpriteImport(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning($"NavigationUiPrefabGenerator: missing background asset at '{assetPath}'.");
            return;
        }

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        var changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var segments = path.Split('/');
        if (segments.Length < 2)
        {
            return;
        }

        var current = segments[0];
        for (var i = 1; i < segments.Length; i++)
        {
            var next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }

    private static GameObject CreateUiNode(string name, Vector2 sizeDelta, Transform parent = null)
    {
        var node = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            node.transform.SetParent(parent, false);
        }

        var rt = node.GetComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
        return node;
    }

    private static Text CreateText(Transform parent, string name, Font font, string value, int fontSize, TextAnchor anchor, Color color)
    {
        var go = CreateUiNode(name, Vector2.zero, parent);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static GameObject SavePrefab(GameObject root, string assetPath)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private sealed class GeneratedAssets
    {
        public Sprite BackgroundSprite;
        public Sprite MenuPanelBackgroundSprite;
        public Sprite SearchIconSprite;
        public Sprite DefaultCategoryIconSprite;
        public Sprite FoodCategoryIconSprite;
        public Sprite EntertainmentCategoryIconSprite;
        public Sprite KidsCategoryIconSprite;
        public Sprite ServicesCategoryIconSprite;
        public Sprite ChevronIconSprite;
        public Sprite SeparatorSprite;
        public Sprite RoundedPanelSprite;
        public Sprite RoundedPanelSmallSprite;
        public Sprite RoundedPanelLargeSprite;
        public Sprite TurnRightIconSprite;

        public GameObject ActionButtonPrefab;
        public GameObject SearchFieldPrefab;
        public GameObject ListItemPrefab;
        public GameObject ScrollViewPrefab;

        public GameObject MenuViewPrefab;
        public GameObject LocalizingViewPrefab;
        public GameObject DirectionHintViewPrefab;
        public GameObject TripSummaryViewPrefab;
        public GameObject ArrivalViewPrefab;
        public GameObject AboutViewPrefab;
    }
}
#endif
