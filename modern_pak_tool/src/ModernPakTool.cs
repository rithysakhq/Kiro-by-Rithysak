using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

public sealed class AppEntry : Application
{
    [STAThread]
    public static int Main()
    {
        AppEntry app = new AppEntry();
        app.Startup += delegate { app.MainWindow = new MainWindow(); app.MainWindow.Show(); };
        return app.Run();
    }
}

public sealed class MainWindow : Window
{
    private const string AppTitle = "Kiro by Rithysak";
    private const string AppIconResourceName = "ModernPakTool.kiro_app_icon.png";
    private const string AppIconFileName = "Kiro by Rithysak - AppIcon.png";
    private const string FullLogoResourceName = "ModernPakTool.kiro_full_logo.png";
    private const string FullLogoFileName = "Kiro by Rithysak - full logo.png";
    private readonly TextBox _packFolder = new TextBox();
    private readonly TextBox _packOutput = new TextBox();
    private readonly TextBox _unpackFile = new TextBox();
    private readonly TextBox _unpackOutput = new TextBox();
    private readonly Button _packBrowseButton = new Button();
    private readonly Button _packOutputButton = new Button();
    private readonly Button _packDefaultButton = new Button();
    private readonly Button _unpackBrowseButton = new Button();
    private readonly Button _unpackOutputButton = new Button();
    private readonly Button _unpackDefaultButton = new Button();
    private readonly TextBlock _status = new TextBlock();
    private readonly TextBlock _phaseText = new TextBlock();
    private readonly Border _phaseBadge = new Border();
    private readonly ProgressBar _progress = new ProgressBar();
    private readonly Expander _detailsExpander = new Expander();
    private readonly TextBox _detailsBox = new TextBox();
    private readonly Button _copyDetailsButton = new Button();
    private readonly Button _packModeButton = new Button();
    private readonly Button _unpackModeButton = new Button();
    private readonly Button _inspectModeButton = new Button();
    private readonly Button _reportsModeButton = new Button();
    private readonly Button _settingsModeButton = new Button();
    private readonly ContentControl _modeContent = new ContentControl();
    private readonly Button _packButton = new Button();
    private readonly Button _unpackButton = new Button();
    private readonly Button _unpackInspectButton = new Button();
    private readonly Button _packClearButton = new Button();
    private readonly Button _packOpenButton = new Button();
    private readonly Button _unpackClearButton = new Button();
    private readonly Button _unpackOpenButton = new Button();
    private readonly TextBox _inspectFolder = new TextBox();
    private readonly TextBox _inspectSearch = new TextBox();
    private readonly ComboBox _inspectFilter = new ComboBox();
    private readonly Button _inspectBrowseButton = new Button();
    private readonly Button _inspectScanButton = new Button();
    private readonly Button _inspectSaveButton = new Button();
    private readonly Button _inspectOpenOutputButton = new Button();
    private readonly Button _inspectOpenFileButton = new Button();
    private readonly Button _inspectOpenFolderButton = new Button();
    private readonly Button _inspectCopyPathButton = new Button();
    private readonly Button _inspectCopyIdButton = new Button();
    private readonly Button _inspectShowDetailsButton = new Button();
    private readonly DataGrid _inventoryGrid = new DataGrid();
    private readonly TextBlock _inspectSummary = new TextBlock();
    private readonly TextBox _inventoryDetails = new TextBox();
    private readonly TextBox _reportsBox = new TextBox();
    private readonly Button _reportsCopyButton = new Button();
    private readonly Button _reportsOpenInventoryButton = new Button();
    private readonly Button _reportsOpenSummaryButton = new Button();
    private readonly TextBox _settingsReferenceBox = new TextBox();
    private readonly LinearGradientBrush _interactiveBackground = new LinearGradientBrush();
    private readonly Border _packInfoPanel = new Border();
    private readonly Border _unpackInfoPanel = new Border();
    private readonly TextBlock _packInfoTitle = new TextBlock();
    private readonly TextBlock _packInfoBody = new TextBlock();
    private readonly TextBlock _unpackInfoTitle = new TextBlock();
    private readonly TextBlock _unpackInfoBody = new TextBlock();
    private readonly Border _recentPanel = new Border();
    private readonly TextBlock _recentText = new TextBlock();
    private readonly List<UIElement> _inputControls = new List<UIElement>();
    private readonly List<string> _recentItems = new List<string>();

    private UIElement _packView;
    private UIElement _unpackView;
    private UIElement _inspectView;
    private UIElement _reportsView;
    private UIElement _settingsView;
    private WorkbenchMode _activeMode = WorkbenchMode.Pack;
    private bool _isPackMode = true;
    private bool _isBusy;
    private bool _engineReady = true;
    private string _lastDetails = "";
    private InventoryResult _lastInventory;
    private string _lastInventoryPath = "";
    private string _lastRecoverySummaryPath = "";
    private string _lastUnpackOutput = "";

    public MainWindow()
    {
        Title = AppTitle;
        Icon = LoadLogoImage();
        Width = 820;
        Height = 530;
        MinWidth = 820;
        MinHeight = 530;
        MaxWidth = 820;
        MaxHeight = 530;
        SizeToContent = SizeToContent.Manual;
        ResizeMode = ResizeMode.CanMinimize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ConfigureInteractiveBackground();
        Background = _interactiveBackground;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;
        KeyDown += OnWindowKeyDown;
        SourceInitialized += delegate { EnableDarkTitleBar(); };

        Content = BuildUi();
        WireEvents();
        SwitchMode(WorkbenchMode.Pack);
        SetDetails("");
        OperationResult runtime = PakOperations.CheckRuntime();
        _engineReady = runtime.Success;
        if (_engineReady)
        {
            SetStatus("Ready", "Choose a folder to pack or a PAK file to unpack.", UiTone.Info);
        }
        else
        {
            SetStatus("Engine Missing", runtime.Message, UiTone.Error);
            SetDetails("Startup preflight failed.\r\n" + runtime.Message);
            _detailsExpander.IsExpanded = true;
        }
        UpdatePackPreflight();
        UpdateUnpackPreflight();
        UpdateActionAvailability();
    }

    private UIElement BuildUi()
    {
        Grid root = new Grid();
        root.Margin = new Thickness(9);
        root.AllowDrop = true;
        root.PreviewDragOver += OnWindowDragOver;
        root.Drop += OnWindowDrop;
        root.MouseMove += OnGlassMouseMove;
        root.MouseLeave += delegate { UpdateGlassGradient(0.46, 0.28); };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        UIElement gridOverlay = BuildGridOverlay();
        Grid.SetRowSpan(gridOverlay, 3);
        Panel.SetZIndex(gridOverlay, 0);
        root.Children.Add(gridOverlay);

        UIElement brandShape = BuildAbstractBlueShape();
        Grid.SetRowSpan(brandShape, 3);
        Panel.SetZIndex(brandShape, 1);
        root.Children.Add(brandShape);

        UIElement header = BuildHeader();
        Grid.SetRow(header, 0);
        Panel.SetZIndex(header, 2);
        root.Children.Add(header);

        UIElement statusPanel = BuildStatusPanel();
        ((FrameworkElement)statusPanel).Margin = new Thickness(0, 7, 0, 0);
        Grid.SetRow(statusPanel, 1);
        Panel.SetZIndex(statusPanel, 2);
        root.Children.Add(statusPanel);

        Border workPanel = CreatePanel(8);
        workPanel.Margin = new Thickness(0, 7, 0, 0);
        workPanel.ClipToBounds = true;
        Grid.SetRow(workPanel, 2);
        Panel.SetZIndex(workPanel, 2);
        Grid workGrid = new Grid();
        workGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        workGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workPanel.Child = workGrid;

        workGrid.Children.Add(BuildModeSwitch());

        _packView = BuildPackView();
        _unpackView = BuildUnpackView();
        _inspectView = BuildInspectView();
        _reportsView = BuildReportsView();
        _settingsView = BuildSettingsView();
        _modeContent.Margin = new Thickness(0, 7, 0, 0);
        _modeContent.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _modeContent.VerticalContentAlignment = VerticalAlignment.Stretch;

        ScrollViewer modeScroller = new ScrollViewer();
        modeScroller.Content = _modeContent;
        modeScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        modeScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        modeScroller.CanContentScroll = false;
        Grid.SetRow(modeScroller, 1);
        workGrid.Children.Add(modeScroller);

        ConfigureRecentPanel();
        Grid.SetRow(_recentPanel, 2);
        workGrid.Children.Add(_recentPanel);

        root.Children.Add(workPanel);

        return root;
    }

    private static UIElement BuildGridOverlay()
    {
        Border overlay = new Border();
        overlay.Background = CreateGridLineBrush();
        overlay.Opacity = 0.46;
        overlay.IsHitTestVisible = false;
        return overlay;
    }

    private static UIElement BuildAbstractBlueShape()
    {
        Canvas canvas = new Canvas();
        canvas.Width = 430;
        canvas.Height = 360;
        canvas.HorizontalAlignment = HorizontalAlignment.Right;
        canvas.VerticalAlignment = VerticalAlignment.Top;
        canvas.Margin = new Thickness(0, -120, -165, 0);
        canvas.IsHitTestVisible = false;
        canvas.Opacity = 0.44;
        canvas.RenderTransform = new ScaleTransform(0.72, 0.72);
        canvas.RenderTransformOrigin = new Point(1, 0);

        System.Windows.Shapes.Ellipse glow = new System.Windows.Shapes.Ellipse();
        glow.Width = 420;
        glow.Height = 300;
        glow.Fill = CreateRadialGlowBrush();
        Canvas.SetLeft(glow, 110);
        Canvas.SetTop(glow, 72);
        canvas.Children.Add(glow);

        System.Windows.Shapes.Path ribbonBack = new System.Windows.Shapes.Path();
        ribbonBack.Data = Geometry.Parse("M98,240 C178,116 303,86 430,40 C394,118 391,187 452,252 C346,230 267,257 194,356 C178,298 146,263 98,240 Z");
        ribbonBack.Fill = CreateRibbonBrush(Color.FromArgb(178, 21, 75, 224), Color.FromArgb(34, 70, 180, 255));
        ribbonBack.Stretch = Stretch.Fill;
        ribbonBack.Width = 370;
        ribbonBack.Height = 330;
        Canvas.SetLeft(ribbonBack, 142);
        Canvas.SetTop(ribbonBack, 58);
        canvas.Children.Add(ribbonBack);

        System.Windows.Shapes.Path ribbonFront = new System.Windows.Shapes.Path();
        ribbonFront.Data = Geometry.Parse("M42,222 C120,160 197,146 304,66 C287,157 318,223 426,294 C305,301 218,327 120,410 C122,326 93,267 42,222 Z");
        ribbonFront.Fill = CreateRibbonBrush(Color.FromArgb(225, 0, 106, 255), Color.FromArgb(72, 135, 224, 255));
        ribbonFront.Stretch = Stretch.Fill;
        ribbonFront.Width = 390;
        ribbonFront.Height = 350;
        Canvas.SetLeft(ribbonFront, 90);
        Canvas.SetTop(ribbonFront, 74);
        canvas.Children.Add(ribbonFront);

        System.Windows.Shapes.Path highlight = new System.Windows.Shapes.Path();
        highlight.Data = Geometry.Parse("M92,195 C164,181 233,145 323,80 C301,133 311,183 360,232 C264,211 188,234 101,306 C111,258 108,224 92,195 Z");
        highlight.Fill = CreateRibbonBrush(Color.FromArgb(190, 107, 191, 255), Color.FromArgb(12, 255, 255, 255));
        highlight.Stretch = Stretch.Fill;
        highlight.Width = 260;
        highlight.Height = 220;
        Canvas.SetLeft(highlight, 158);
        Canvas.SetTop(highlight, 110);
        canvas.Children.Add(highlight);

        return canvas;
    }

    private UIElement BuildHeader()
    {
        Grid header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border iconShell = new Border();
        iconShell.Width = 40;
        iconShell.Height = 40;
        iconShell.CornerRadius = new CornerRadius(8);
        iconShell.Background = Argb(150, 3, 8, 14);
        iconShell.BorderBrush = Argb(132, 73, 222, 229);
        iconShell.BorderThickness = new Thickness(1);
        iconShell.Child = new Image
        {
            Source = LoadLogoImage(),
            Width = 30,
            Height = 30,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Children.Add(iconShell);

        StackPanel titleStack = new StackPanel();
        titleStack.Margin = new Thickness(10, 0, 0, 0);
        titleStack.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(titleStack, 1);

        StackPanel nameLine = new StackPanel();
        nameLine.Orientation = Orientation.Horizontal;
        nameLine.VerticalAlignment = VerticalAlignment.Center;

        TextBlock title = new TextBlock();
        title.Text = "Kiro";
        title.FontSize = 21;
        title.FontWeight = FontWeights.SemiBold;
        title.Foreground = Rgb(246, 250, 255);
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        nameLine.Children.Add(title);

        TextBlock byline = new TextBlock();
        byline.Text = "by Rithysak";
        byline.FontSize = 12;
        byline.FontWeight = FontWeights.SemiBold;
        byline.Foreground = Rgb(38, 226, 232);
        byline.Margin = new Thickness(9, 6, 0, 0);
        nameLine.Children.Add(byline);
        titleStack.Children.Add(nameLine);

        TextBlock subtitle = new TextBlock();
        subtitle.Text = "Pack, unpack, inspect, and report on JX PAK archives.";
        subtitle.FontSize = 12;
        subtitle.Foreground = Rgb(145, 158, 178);
        subtitle.Margin = new Thickness(0, 1, 0, 0);
        subtitle.TextWrapping = TextWrapping.Wrap;
        titleStack.Children.Add(subtitle);

        header.Children.Add(titleStack);
        return header;
    }

    private UIElement BuildModeSwitch()
    {
        Border shell = new Border();
        shell.HorizontalAlignment = HorizontalAlignment.Left;
        shell.Background = Argb(88, 9, 14, 24);
        shell.BorderBrush = Argb(96, 91, 111, 139);
        shell.BorderThickness = new Thickness(1);
        shell.CornerRadius = new CornerRadius(6);
        shell.Padding = new Thickness(3);

        StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
        AddModeButton(buttons, _packModeButton, "Pack", WorkbenchMode.Pack);
        AddModeButton(buttons, _unpackModeButton, "Unpack", WorkbenchMode.Unpack);
        AddModeButton(buttons, _inspectModeButton, "Inspect", WorkbenchMode.Inspect);
        AddModeButton(buttons, _reportsModeButton, "Reports", WorkbenchMode.Reports);
        AddModeButton(buttons, _settingsModeButton, "Settings", WorkbenchMode.Settings);
        shell.Child = buttons;
        return shell;
    }

    private void AddModeButton(StackPanel buttons, Button button, string text, WorkbenchMode mode)
    {
        ConfigureButton(button, text, false);
        button.MinWidth = 82;
        button.Padding = new Thickness(12, 6, 12, 7);
        button.BorderThickness = new Thickness(1);
        button.Margin = buttons.Children.Count == 0 ? new Thickness(0) : new Thickness(3, 0, 0, 0);
        button.Click += delegate { SwitchMode(mode); };
        Track(button);
        buttons.Children.Add(button);
    }

    private UIElement BuildPackView()
    {
        Grid grid = new Grid();
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement source = BuildPathRow(
            "folder",
            "Source folder",
            "Every file below this folder is handed to the legacy pack engine.",
            _packFolder,
            _packBrowseButton,
            "Browse...",
            ChoosePackFolder,
            null,
            null);
        Grid.SetRow(source, 0);
        grid.Children.Add(source);

        UIElement destination = BuildPathRow(
            "archive",
            "Destination PAK",
            "Defaults to a sibling .pak file; a matching .pak.txt sidecar is written too.",
            _packOutput,
            _packOutputButton,
            "Save as...",
            ChoosePackOutput,
            _packDefaultButton,
            ResetPackOutput);
        Grid.SetRow(destination, 1);
        grid.Children.Add(destination);

        ConfigureInfoPanel(_packInfoPanel, _packInfoTitle, _packInfoBody);
        Grid.SetRow(_packInfoPanel, 2);
        grid.Children.Add(_packInfoPanel);

        UIElement actions = BuildActionRow(_packButton, "Pack Folder", _packClearButton, _packOpenButton);
        _packButton.Click += async delegate { await PackAsync(); };
        _packClearButton.Click += delegate { ClearPack(); };
        _packOpenButton.Click += delegate { OpenPackOutput(); };
        Grid.SetRow(actions, 3);
        grid.Children.Add(actions);

        return grid;
    }

    private UIElement BuildUnpackView()
    {
        Grid grid = new Grid();
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement source = BuildPathRow(
            "archive",
            "Source PAK",
            "A matching TXT manifest preserves original filenames during extraction.",
            _unpackFile,
            _unpackBrowseButton,
            "Browse...",
            ChoosePakFile,
            null,
            null);
        Grid.SetRow(source, 0);
        grid.Children.Add(source);

        UIElement destination = BuildPathRow(
            "folder",
            "Output folder",
            "Defaults to a folder beside the archive.",
            _unpackOutput,
            _unpackOutputButton,
            "Choose...",
            ChooseUnpackOutput,
            _unpackDefaultButton,
            ResetUnpackOutput);
        Grid.SetRow(destination, 1);
        grid.Children.Add(destination);

        ConfigureInfoPanel(_unpackInfoPanel, _unpackInfoTitle, _unpackInfoBody);
        Grid.SetRow(_unpackInfoPanel, 2);
        grid.Children.Add(_unpackInfoPanel);

        UIElement actions = BuildUnpackActionRow();
        _unpackButton.Click += async delegate { await UnpackAsync(); };
        _unpackInspectButton.Click += async delegate { await InspectUnpackOutputAsync(); };
        _unpackClearButton.Click += delegate { ClearUnpack(); };
        _unpackOpenButton.Click += delegate { OpenUnpackOutput(); };
        Grid.SetRow(actions, 3);
        grid.Children.Add(actions);

        return grid;
    }

    private UIElement BuildUnpackActionRow()
    {
        StackPanel row = new StackPanel();
        row.Orientation = Orientation.Horizontal;

        ConfigureButton(_unpackButton, "Unpack PAK", true);
        _unpackButton.MinWidth = 132;
        row.Children.Add(_unpackButton);
        Track(_unpackButton);

        ConfigureButton(_unpackInspectButton, "Inspect Output", false);
        _unpackInspectButton.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(_unpackInspectButton);
        Track(_unpackInspectButton);

        ConfigureButton(_unpackOpenButton, "Open Output", false);
        _unpackOpenButton.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(_unpackOpenButton);
        Track(_unpackOpenButton);

        ConfigureButton(_unpackClearButton, "Clear", false);
        _unpackClearButton.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(_unpackClearButton);
        Track(_unpackClearButton);

        return row;
    }

    private UIElement BuildInspectView()
    {
        Grid grid = new Grid();
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Stretch;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement folder = BuildPathRow(
            "folder",
            "Output folder to inspect",
            "Scans extracted files without rewriting game payloads. The inventory report is written as _pak_tool_inventory.tsv.",
            _inspectFolder,
            _inspectBrowseButton,
            "Choose...",
            ChooseInspectFolder,
            null,
            null);
        Grid.SetRow(folder, 0);
        grid.Children.Add(folder);

        StackPanel filters = new StackPanel();
        filters.Orientation = Orientation.Horizontal;
        filters.Margin = new Thickness(0, 0, 0, 6);

        _inspectFilter.Items.Add("All");
        _inspectFilter.Items.Add("Recovered names");
        _inspectFilter.Items.Add("Typed unknowns");
        _inspectFilter.Items.Add("Unknown binary");
        _inspectFilter.Items.Add("Text/config/script");
        _inspectFilter.Items.Add("Sprites/resources");
        _inspectFilter.Items.Add("Risky/uncertain");
        _inspectFilter.SelectedIndex = 0;
        _inspectFilter.Width = 132;
        ConfigureComboBox(_inspectFilter);
        _inspectFilter.SelectionChanged += delegate { ApplyInventoryFilter(); };
        filters.Children.Add(_inspectFilter);
        Track(_inspectFilter);

        ConfigureTextBox(_inspectSearch);
        _inspectSearch.Width = 226;
        _inspectSearch.Margin = new Thickness(8, 0, 0, 0);
        _inspectSearch.ToolTip = "Search path, ID, extension, type, or status";
        _inspectSearch.TextChanged += delegate { ApplyInventoryFilter(); };
        filters.Children.Add(_inspectSearch);
        Track(_inspectSearch);

        ConfigureButton(_inspectScanButton, "Scan", true);
        _inspectScanButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectScanButton.Click += async delegate { await ScanInspectFolderAsync(); };
        filters.Children.Add(_inspectScanButton);
        Track(_inspectScanButton);

        ConfigureButton(_inspectSaveButton, "Save", false);
        _inspectSaveButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectSaveButton.Click += delegate { SaveCurrentInventory(); };
        filters.Children.Add(_inspectSaveButton);
        Track(_inspectSaveButton);

        ConfigureButton(_inspectOpenOutputButton, "Open", false);
        _inspectOpenOutputButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectOpenOutputButton.Click += delegate { OpenInspectFolder(); };
        filters.Children.Add(_inspectOpenOutputButton);
        Track(_inspectOpenOutputButton);

        Grid.SetRow(filters, 1);
        grid.Children.Add(filters);

        Border summaryPanel = CreatePlainPanel(7);
        summaryPanel.Margin = new Thickness(0, 0, 0, 6);
        _inspectSummary.Text = "No inventory loaded. Choose an unpacked output folder, then scan.";
        _inspectSummary.TextWrapping = TextWrapping.Wrap;
        _inspectSummary.Foreground = Rgb(183, 196, 213);
        summaryPanel.Child = _inspectSummary;
        Grid.SetRow(summaryPanel, 2);
        grid.Children.Add(summaryPanel);

        ConfigureInventoryGrid();
        Grid.SetRow(_inventoryGrid, 3);
        grid.Children.Add(_inventoryGrid);

        ConfigureReadOnlyBox(_inventoryDetails, 34);
        _inventoryDetails.Margin = new Thickness(0, 5, 0, 5);
        _inventoryDetails.Text = "Select a file to see what the app knows about it and what is safe to do next.";
        Grid.SetRow(_inventoryDetails, 4);
        grid.Children.Add(_inventoryDetails);

        StackPanel actions = new StackPanel();
        actions.Orientation = Orientation.Horizontal;
        ConfigureButton(_inspectOpenFileButton, "Open File", false);
        _inspectOpenFileButton.Click += delegate { OpenSelectedInventoryFile(); };
        actions.Children.Add(_inspectOpenFileButton);
        Track(_inspectOpenFileButton);

        ConfigureButton(_inspectOpenFolderButton, "Open Containing Folder", false);
        _inspectOpenFolderButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectOpenFolderButton.Click += delegate { OpenSelectedInventoryFolder(); };
        actions.Children.Add(_inspectOpenFolderButton);
        Track(_inspectOpenFolderButton);

        ConfigureButton(_inspectCopyPathButton, "Copy Path", false);
        _inspectCopyPathButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectCopyPathButton.Click += delegate { CopySelectedInventoryPath(); };
        actions.Children.Add(_inspectCopyPathButton);
        Track(_inspectCopyPathButton);

        ConfigureButton(_inspectCopyIdButton, "Copy ID", false);
        _inspectCopyIdButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectCopyIdButton.Click += delegate { CopySelectedInventoryId(); };
        actions.Children.Add(_inspectCopyIdButton);
        Track(_inspectCopyIdButton);

        ConfigureButton(_inspectShowDetailsButton, "Show Details", false);
        _inspectShowDetailsButton.Margin = new Thickness(8, 0, 0, 0);
        _inspectShowDetailsButton.Click += delegate { ShowSelectedInventoryDetails(); };
        actions.Children.Add(_inspectShowDetailsButton);
        Track(_inspectShowDetailsButton);

        Grid.SetRow(actions, 5);
        grid.Children.Add(actions);

        UpdateInventoryActionAvailability();
        return grid;
    }

    private void ConfigureInventoryGrid()
    {
        _inventoryGrid.AutoGenerateColumns = false;
        _inventoryGrid.IsReadOnly = true;
        _inventoryGrid.CanUserAddRows = false;
        _inventoryGrid.CanUserDeleteRows = false;
        _inventoryGrid.SelectionMode = DataGridSelectionMode.Single;
        _inventoryGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        _inventoryGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
        _inventoryGrid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        _inventoryGrid.Background = Argb(130, 6, 10, 18);
        _inventoryGrid.Foreground = Rgb(225, 233, 245);
        _inventoryGrid.BorderBrush = Argb(92, 87, 111, 143);
        _inventoryGrid.BorderThickness = new Thickness(1);
        _inventoryGrid.MinHeight = 72;
        _inventoryGrid.RowBackground = Argb(82, 12, 18, 30);
        _inventoryGrid.AlternatingRowBackground = Argb(112, 16, 24, 38);
        _inventoryGrid.HorizontalGridLinesBrush = Argb(80, 77, 100, 132);
        _inventoryGrid.VerticalGridLinesBrush = Argb(60, 77, 100, 132);
        _inventoryGrid.ColumnHeaderStyle = CreateDataGridHeaderStyle();
        _inventoryGrid.RowStyle = CreateDataGridRowStyle();
        _inventoryGrid.CellStyle = CreateDataGridCellStyle();
        _inventoryGrid.SelectionChanged += delegate { UpdateInventoryDetails(); };

        AddInventoryColumn("Status", "Status", 118);
        AddInventoryColumn("Type", "DetectedType", 80);
        AddInventoryColumn("Path", "RelativePath", 1);
        AddInventoryColumn("Confidence", "Confidence", 92);
        AddInventoryColumn("Editable", "EditableHint", 80);
        AddInventoryColumn("Repack", "RepackSafety", 82);
        AddInventoryColumn("Bytes", "SizeBytes", 82);
    }

    private static Style CreateDataGridHeaderStyle()
    {
        Style style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Argb(188, 10, 16, 27)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Rgb(158, 176, 200)));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Argb(92, 77, 100, 132)));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 6, 8, 6)));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        return style;
    }

    private static Style CreateDataGridRowStyle()
    {
        Style style = new Style(typeof(DataGridRow));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Rgb(225, 233, 245)));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Argb(54, 77, 100, 132)));
        return style;
    }

    private static Style CreateDataGridCellStyle()
    {
        Style style = new Style(typeof(DataGridCell));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Rgb(225, 233, 245)));
        return style;
    }

    private void AddInventoryColumn(string header, string binding, double width)
    {
        DataGridTextColumn column = new DataGridTextColumn();
        column.Header = header;
        column.Binding = new Binding(binding);
        if (width == 1)
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        else
            column.Width = new DataGridLength(width);
        _inventoryGrid.Columns.Add(column);
    }

    private UIElement BuildReportsView()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        ConfigureReadOnlyBox(_reportsBox, 250);
        _reportsBox.Text = "No report is loaded yet. Unpack an archive or scan an output folder to generate an inventory and recovery summary.";
        Grid.SetRow(_reportsBox, 0);
        grid.Children.Add(_reportsBox);

        StackPanel actions = new StackPanel();
        actions.Orientation = Orientation.Horizontal;
        actions.Margin = new Thickness(0, 8, 0, 0);

        ConfigureButton(_reportsCopyButton, "Copy Report", false);
        _reportsCopyButton.Click += delegate { CopyReportsText(); };
        actions.Children.Add(_reportsCopyButton);
        Track(_reportsCopyButton);

        ConfigureButton(_reportsOpenInventoryButton, "Open Inventory", false);
        _reportsOpenInventoryButton.Margin = new Thickness(8, 0, 0, 0);
        _reportsOpenInventoryButton.Click += delegate { OpenReportFile(_lastInventoryPath); };
        actions.Children.Add(_reportsOpenInventoryButton);
        Track(_reportsOpenInventoryButton);

        ConfigureButton(_reportsOpenSummaryButton, "Open Summary", false);
        _reportsOpenSummaryButton.Margin = new Thickness(8, 0, 0, 0);
        _reportsOpenSummaryButton.Click += delegate { OpenReportFile(_lastRecoverySummaryPath); };
        actions.Children.Add(_reportsOpenSummaryButton);
        Track(_reportsOpenSummaryButton);

        Grid.SetRow(actions, 1);
        grid.Children.Add(actions);

        UpdateReportsActionAvailability();
        return grid;
    }

    private UIElement BuildSettingsView()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border info = CreatePlainPanel(8);
        TextBlock copy = new TextBlock();
        copy.TextWrapping = TextWrapping.Wrap;
        copy.Foreground = Rgb(183, 196, 213);
        copy.Text = "Safe-first defaults are enabled. Inspection never rewrites extracted game files. Unknown-ID files are valid extracted entries whose original paths are missing. Experimental ID-aware patching is not enabled in this build.";
        info.Child = copy;
        Grid.SetRow(info, 0);
        grid.Children.Add(info);

        ConfigureReadOnlyBox(_settingsReferenceBox, 250);
        _settingsReferenceBox.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(_settingsReferenceBox, 1);
        grid.Children.Add(_settingsReferenceBox);
        UpdateReferenceSourcesPanel();
        return grid;
    }

    private UIElement BuildPathRow(
        string iconKind,
        string label,
        string hint,
        TextBox box,
        Button browseButton,
        string browseText,
        RoutedEventHandler browseHandler,
        Button resetButton,
        RoutedEventHandler resetHandler)
    {
        Grid row = new Grid();
        row.HorizontalAlignment = HorizontalAlignment.Stretch;
        row.Margin = new Thickness(0, 0, 0, 8);
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement icon = BuildMiniIcon(iconKind);
        Grid.SetRow(icon, 0);
        Grid.SetRowSpan(icon, 2);
        Grid.SetColumn(icon, 0);
        row.Children.Add(icon);

        StackPanel labels = new StackPanel();
        labels.Margin = new Thickness(11, 0, 10, 2);

        StackPanel headingLine = new StackPanel();
        headingLine.Orientation = Orientation.Horizontal;

        TextBlock step = new TextBlock();
        step.Text = GetWorkflowNumber(label);
        step.FontSize = 10;
        step.FontWeight = FontWeights.SemiBold;
        step.Foreground = Rgb(88, 166, 255);
        step.Margin = new Thickness(0, 2, 8, 0);
        headingLine.Children.Add(step);

        TextBlock heading = new TextBlock();
        heading.Text = label;
        heading.FontWeight = FontWeights.SemiBold;
        heading.Foreground = Rgb(239, 245, 255);
        headingLine.Children.Add(heading);
        labels.Children.Add(headingLine);

        TextBlock description = new TextBlock();
        description.Text = hint;
        description.Foreground = Rgb(145, 158, 178);
        description.FontSize = 11;
        description.TextWrapping = TextWrapping.Wrap;
        labels.Children.Add(description);

        Grid.SetRow(labels, 0);
        Grid.SetColumn(labels, 1);
        row.Children.Add(labels);

        Grid inputLine = new Grid();
        inputLine.Margin = new Thickness(18, 0, 0, 0);
        inputLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        StackPanel buttons = new StackPanel();
        buttons.Orientation = Orientation.Horizontal;
        buttons.HorizontalAlignment = HorizontalAlignment.Left;
        buttons.Margin = new Thickness(0, 0, 8, 0);
        ConfigureButton(browseButton, browseText, false);
        browseButton.MinWidth = Math.Max(browseButton.MinWidth, 96);
        browseButton.Click += browseHandler;
        buttons.Children.Add(browseButton);
        Track(browseButton);

        if (resetButton != null)
        {
            ConfigureButton(resetButton, "Default", false);
            resetButton.Margin = new Thickness(8, 0, 0, 0);
            resetButton.MinWidth = Math.Max(resetButton.MinWidth, 84);
            resetButton.Click += resetHandler;
            buttons.Children.Add(resetButton);
            Track(resetButton);
        }

        Grid.SetColumn(buttons, 0);
        inputLine.Children.Add(buttons);

        ConfigureTextBox(box);
        Grid.SetColumn(box, 1);
        inputLine.Children.Add(box);
        Track(box);

        Grid.SetRow(inputLine, 1);
        Grid.SetColumn(inputLine, 1);
        row.Children.Add(inputLine);
        return row;
    }

    private static string GetWorkflowNumber(string label)
    {
        if (label.IndexOf("Source", StringComparison.OrdinalIgnoreCase) >= 0 ||
            label.IndexOf("PAK file", StringComparison.OrdinalIgnoreCase) >= 0 ||
            label.IndexOf("folder to inspect", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "01";
        }

        if (label.IndexOf("Destination", StringComparison.OrdinalIgnoreCase) >= 0 ||
            label.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "02";
        }

        return "01";
    }

    private UIElement BuildActionRow(Button primary, string primaryText, Button clearButton, Button openButton)
    {
        StackPanel row = new StackPanel();
        row.Orientation = Orientation.Horizontal;
        row.Margin = new Thickness(0, 0, 0, 0);

        ConfigureButton(primary, primaryText, true);
        primary.MinWidth = 132;
        row.Children.Add(primary);
        Track(primary);

        ConfigureButton(clearButton, "Clear", false);
        clearButton.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(clearButton);
        Track(clearButton);

        ConfigureButton(openButton, "Open Output", false);
        openButton.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(openButton);
        Track(openButton);

        return row;
    }

    private UIElement BuildStatusPanel()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Border panel = CreatePanel(8);
        panel.MinHeight = 38;
        panel.Child = grid;

        DockPanel line = new DockPanel();
        _phaseBadge.CornerRadius = new CornerRadius(5);
        _phaseBadge.Padding = new Thickness(8, 1, 8, 2);
        _phaseBadge.Margin = new Thickness(0, 0, 10, 0);
        _phaseText.FontSize = 12;
        _phaseText.FontWeight = FontWeights.SemiBold;
        _phaseBadge.Child = _phaseText;
        DockPanel.SetDock(_phaseBadge, Dock.Left);
        line.Children.Add(_phaseBadge);

        _status.TextWrapping = TextWrapping.Wrap;
        _status.Foreground = Rgb(218, 227, 240);
        _status.VerticalAlignment = VerticalAlignment.Center;
        line.Children.Add(_status);
        Grid.SetRow(line, 0);
        grid.Children.Add(line);

        _progress.Height = 6;
        _progress.Margin = new Thickness(0, 7, 0, 0);
        _progress.Visibility = Visibility.Collapsed;
        _progress.Foreground = Rgb(88, 166, 255);
        _progress.Background = Argb(120, 4, 8, 16);
        Grid.SetRow(_progress, 1);
        grid.Children.Add(_progress);

        _detailsExpander.Header = "Details";
        _detailsExpander.Foreground = Rgb(169, 184, 206);
        _detailsExpander.Margin = new Thickness(0, 6, 0, 0);
        Grid detailsGrid = new Grid();
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _detailsBox.IsReadOnly = true;
        _detailsBox.AcceptsReturn = true;
        _detailsBox.TextWrapping = TextWrapping.Wrap;
        _detailsBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _detailsBox.Height = 60;
        _detailsBox.FontFamily = new FontFamily("Consolas");
        _detailsBox.FontSize = 12;
        _detailsBox.Background = Argb(156, 4, 8, 16);
        _detailsBox.Foreground = Rgb(214, 224, 238);
        _detailsBox.BorderBrush = Argb(120, 80, 104, 136);
        _detailsBox.BorderThickness = new Thickness(1);
        _detailsBox.Padding = new Thickness(6);
        Grid.SetRow(_detailsBox, 0);
        detailsGrid.Children.Add(_detailsBox);

        ConfigureButton(_copyDetailsButton, "Copy Details", false);
        _copyDetailsButton.HorizontalAlignment = HorizontalAlignment.Right;
        _copyDetailsButton.Margin = new Thickness(0, 8, 0, 0);
        _copyDetailsButton.Click += delegate { CopyDetails(); };
        Grid.SetRow(_copyDetailsButton, 1);
        detailsGrid.Children.Add(_copyDetailsButton);

        _detailsExpander.Content = detailsGrid;
        Grid.SetRow(_detailsExpander, 2);
        grid.Children.Add(_detailsExpander);

        return panel;
    }

    private static Border CreatePanel(double padding)
    {
        Border panel = new Border();
        panel.Background = CreateGlassSurfaceBrush();
        panel.BorderBrush = Argb(92, 96, 128, 168);
        panel.BorderThickness = new Thickness(1);
        panel.CornerRadius = new CornerRadius(7);
        panel.Padding = new Thickness(padding);
        panel.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 22,
            ShadowDepth = 2,
            Opacity = 0.35,
            Color = Color.FromRgb(0, 0, 0)
        };
        return panel;
    }

    private static Border CreatePlainPanel(double padding)
    {
        Border panel = new Border();
        panel.Background = Argb(106, 10, 16, 27);
        panel.BorderBrush = Argb(82, 87, 111, 143);
        panel.BorderThickness = new Thickness(1);
        panel.CornerRadius = new CornerRadius(7);
        panel.Padding = new Thickness(padding);
        return panel;
    }

    private void ConfigureInfoPanel(Border panel, TextBlock title, TextBlock body)
    {
        panel.CornerRadius = new CornerRadius(7);
        panel.BorderThickness = new Thickness(1);
        panel.Padding = new Thickness(8);
        panel.Margin = new Thickness(0, 0, 0, 8);

        StackPanel stack = new StackPanel();
        title.FontWeight = FontWeights.SemiBold;
        title.Margin = new Thickness(0, 0, 0, 3);
        body.TextWrapping = TextWrapping.Wrap;
        body.Foreground = Rgb(178, 191, 208);
        stack.Children.Add(title);
        stack.Children.Add(body);
        panel.Child = stack;
    }

    private void ConfigureRecentPanel()
    {
        _recentPanel.CornerRadius = new CornerRadius(7);
        _recentPanel.BorderThickness = new Thickness(1);
        _recentPanel.BorderBrush = Argb(82, 87, 111, 143);
        _recentPanel.Background = Argb(106, 10, 16, 27);
        _recentPanel.Padding = new Thickness(8);
        _recentPanel.Margin = new Thickness(0, 7, 0, 0);
        _recentPanel.Visibility = Visibility.Collapsed;

        StackPanel stack = new StackPanel();
        TextBlock label = new TextBlock();
        label.Text = "Recent outputs";
        label.FontWeight = FontWeights.SemiBold;
        label.Foreground = Rgb(235, 242, 255);
        label.Margin = new Thickness(0, 0, 0, 4);
        stack.Children.Add(label);
        _recentText.Foreground = Rgb(151, 166, 187);
        _recentText.FontSize = 12;
        _recentText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_recentText);
        _recentPanel.Child = stack;
    }

    private static UIElement BuildMiniIcon(string kind)
    {
        Grid icon = new Grid();
        icon.Width = 30;
        icon.Height = 30;
        icon.Margin = new Thickness(0, 12, 0, 0);

        Border shell = new Border();
        shell.CornerRadius = new CornerRadius(8);
        shell.Background = Argb(118, 13, 21, 35);
        shell.BorderBrush = Argb(118, 74, 119, 180);
        shell.BorderThickness = new Thickness(1);
        icon.Children.Add(shell);

        Canvas canvas = new Canvas();
        canvas.Width = 22;
        canvas.Height = 22;
        canvas.HorizontalAlignment = HorizontalAlignment.Center;
        canvas.VerticalAlignment = VerticalAlignment.Center;

        if (kind == "folder")
        {
            System.Windows.Shapes.Rectangle tab = new System.Windows.Shapes.Rectangle();
            tab.Width = 10;
            tab.Height = 5;
            tab.RadiusX = 2;
            tab.RadiusY = 2;
            tab.Fill = Rgb(215, 151, 51);
            Canvas.SetLeft(tab, 3);
            Canvas.SetTop(tab, 5);
            canvas.Children.Add(tab);

            System.Windows.Shapes.Rectangle body = new System.Windows.Shapes.Rectangle();
            body.Width = 19;
            body.Height = 13;
            body.RadiusX = 3;
            body.RadiusY = 3;
            body.Fill = Rgb(231, 171, 73);
            Canvas.SetLeft(body, 3);
            Canvas.SetTop(body, 8);
            canvas.Children.Add(body);
        }
        else
        {
            System.Windows.Shapes.Rectangle box = new System.Windows.Shapes.Rectangle();
            box.Width = 16;
            box.Height = 18;
            box.RadiusX = 3;
            box.RadiusY = 3;
            box.Fill = Rgb(47, 111, 174);
            Canvas.SetLeft(box, 4);
            Canvas.SetTop(box, 3);
            canvas.Children.Add(box);

            for (int i = 0; i < 3; i++)
            {
                System.Windows.Shapes.Rectangle line = new System.Windows.Shapes.Rectangle();
                line.Width = 8;
                line.Height = 1.5;
                line.RadiusX = 0.75;
                line.RadiusY = 0.75;
                line.Fill = Brushes.White;
                Canvas.SetLeft(line, 8);
                Canvas.SetTop(line, 8 + i * 4);
                canvas.Children.Add(line);
            }
        }

        icon.Children.Add(canvas);
        return icon;
    }

    private static ImageSource CreateArchiveIcon()
    {
        DrawingGroup group = new DrawingGroup();
        Brush blue = Rgb(47, 111, 174);
        Brush light = Rgb(227, 238, 250);
        Pen bluePen = new Pen(Rgb(33, 86, 142), 1.3);
        Pen lightPen = new Pen(light, 1.6);

        group.Children.Add(new GeometryDrawing(blue, bluePen, new RectangleGeometry(new Rect(4, 5, 24, 22), 4, 4)));
        group.Children.Add(new GeometryDrawing(Rgb(67, 132, 193), null, new RectangleGeometry(new Rect(7, 2, 18, 7), 3, 3)));
        group.Children.Add(new GeometryDrawing(null, lightPen, Geometry.Parse("M10,12 L22,12")));
        group.Children.Add(new GeometryDrawing(null, lightPen, Geometry.Parse("M10,17 L22,17")));
        group.Children.Add(new GeometryDrawing(null, lightPen, Geometry.Parse("M10,22 L18,22")));

        DrawingImage image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    private static ImageSource LoadLogoImage()
    {
        try
        {
            BitmapSource image = LoadBrandBitmapSource(AppIconResourceName, AppIconFileName);
            return image == null ? CreateArchiveIcon() : image;
        }
        catch
        {
            return CreateArchiveIcon();
        }
    }

    private static ImageSource LoadFullLogoImage()
    {
        try
        {
            BitmapSource image = LoadBrandBitmapSource(FullLogoResourceName, FullLogoFileName);
            if (image == null)
                return LoadLogoImage();

            Int32Rect crop = ClampCrop(image, new Int32Rect(360, 248, 1100, 400));
            CroppedBitmap cropped = new CroppedBitmap(image, crop);
            cropped.Freeze();
            return cropped;
        }
        catch
        {
            return LoadLogoImage();
        }
    }

    private static BitmapSource LoadBrandBitmapSource(string resourceName, string fileName)
    {
        Stream embedded = typeof(MainWindow).Assembly.GetManifestResourceStream(resourceName);
        if (embedded != null)
        {
            using (embedded)
                return CreateBitmapImage(embedded);
        }

        string path = FindBrandAssetPath(fileName);
        if (String.IsNullOrEmpty(path))
            return null;

        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage CreateBitmapImage(Stream stream)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string FindBrandAssetPath(string fileName)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDirectory, fileName),
            Path.Combine(baseDirectory, "assets", fileName),
            Path.Combine(baseDirectory, "..", "assets", fileName),
            Path.Combine(baseDirectory, "..", "..", "assets", fileName)
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string path = Path.GetFullPath(candidates[i]);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static Int32Rect ClampCrop(BitmapSource source, Int32Rect requested)
    {
        int x = Math.Max(0, Math.Min(requested.X, source.PixelWidth - 1));
        int y = Math.Max(0, Math.Min(requested.Y, source.PixelHeight - 1));
        int width = Math.Max(1, Math.Min(requested.Width, source.PixelWidth - x));
        int height = Math.Max(1, Math.Min(requested.Height, source.PixelHeight - y));
        return new Int32Rect(x, y, width, height);
    }

    private static void ConfigureTextBox(TextBox box)
    {
        box.Height = 28;
        box.VerticalContentAlignment = VerticalAlignment.Center;
        box.Padding = new Thickness(8, 0, 8, 0);
        box.Background = Argb(142, 5, 9, 17);
        box.Foreground = Rgb(230, 237, 248);
        box.CaretBrush = Rgb(88, 166, 255);
        box.BorderBrush = Argb(120, 84, 108, 140);
        box.BorderThickness = new Thickness(1);
        box.ToolTip = "Path";
    }

    private static void ConfigureComboBox(ComboBox box)
    {
        box.Height = 28;
        box.Padding = new Thickness(8, 0, 8, 0);
        box.Background = CreateButtonGlassBrush();
        box.Foreground = Rgb(220, 230, 244);
        box.BorderBrush = Argb(112, 96, 120, 154);
        box.BorderThickness = new Thickness(1);
        box.Style = CreateDarkComboBoxStyle();
        box.ItemContainerStyle = CreateDarkComboBoxItemStyle();
    }

    private static Style CreateDarkComboBoxStyle()
    {
        Style style = new Style(typeof(ComboBox));
        style.Setters.Add(new Setter(Control.TemplateProperty, CreateDarkComboBoxTemplate()));
        return style;
    }

    private static ControlTemplate CreateDarkComboBoxTemplate()
    {
        ControlTemplate template = new ControlTemplate(typeof(ComboBox));

        FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));
        grid.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        grid.AppendChild(border);

        FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(8, 0, 26, 0));
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        grid.AppendChild(presenter);

        FrameworkElementFactory arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "v");
        arrow.SetValue(TextBlock.FontSizeProperty, 11.0);
        arrow.SetValue(TextBlock.ForegroundProperty, Rgb(151, 166, 187));
        arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 9, 1));
        grid.AppendChild(arrow);

        FrameworkElementFactory popup = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.Popup));
        popup.Name = "PART_Popup";
        popup.SetValue(System.Windows.Controls.Primitives.Popup.PlacementProperty, System.Windows.Controls.Primitives.PlacementMode.Bottom);
        popup.SetValue(System.Windows.Controls.Primitives.Popup.AllowsTransparencyProperty, true);
        popup.SetValue(System.Windows.Controls.Primitives.Popup.FocusableProperty, false);
        popup.SetBinding(System.Windows.Controls.Primitives.Popup.IsOpenProperty, new Binding("IsDropDownOpen") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

        FrameworkElementFactory popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        popupBorder.SetValue(Border.BackgroundProperty, Argb(245, 8, 13, 24));
        popupBorder.SetValue(Border.BorderBrushProperty, Argb(132, 84, 108, 140));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetBinding(FrameworkElement.MinWidthProperty, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

        FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
        scrollViewer.SetValue(ScrollViewer.MaxHeightProperty, 220.0);
        scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);

        FrameworkElementFactory itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        scrollViewer.AppendChild(itemsPresenter);
        popupBorder.AppendChild(scrollViewer);
        popup.AppendChild(popupBorder);
        grid.AppendChild(popup);

        template.VisualTree = grid;
        return template;
    }

    private static Style CreateDarkComboBoxItemStyle()
    {
        Style style = new Style(typeof(ComboBoxItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Rgb(220, 230, 244)));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));

        Trigger highlight = new Trigger();
        highlight.Property = ComboBoxItem.IsHighlightedProperty;
        highlight.Value = true;
        highlight.Setters.Add(new Setter(Control.BackgroundProperty, Argb(110, 10, 82, 214)));
        highlight.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        style.Triggers.Add(highlight);

        Trigger selected = new Trigger();
        selected.Property = ComboBoxItem.IsSelectedProperty;
        selected.Value = true;
        selected.Setters.Add(new Setter(Control.BackgroundProperty, Argb(130, 10, 72, 164)));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        style.Triggers.Add(selected);

        return style;
    }

    private static void ConfigureReadOnlyBox(TextBox box, double height)
    {
        box.IsReadOnly = true;
        box.AcceptsReturn = true;
        box.TextWrapping = TextWrapping.Wrap;
        box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        box.Height = height;
        box.FontFamily = new FontFamily("Consolas");
        box.FontSize = 12;
        box.Background = Argb(142, 5, 9, 17);
        box.Foreground = Rgb(213, 224, 240);
        box.CaretBrush = Rgb(88, 166, 255);
        box.BorderBrush = Argb(120, 84, 108, 140);
        box.BorderThickness = new Thickness(1);
        box.Padding = new Thickness(6);
    }

    private static void ConfigureButton(Button button, string text, bool primary)
    {
        button.Content = text;
        button.Height = 28;
        button.MinWidth = primary ? 112 : 80;
        button.Padding = new Thickness(12, 0, 12, 0);
        button.FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal;
        button.Background = primary ? CreatePrimaryGlassBrush() : CreateButtonGlassBrush();
        button.Foreground = primary ? Brushes.White : Rgb(220, 230, 244);
        button.BorderBrush = primary ? Argb(232, 44, 141, 255) : Argb(112, 96, 120, 154);
        button.BorderThickness = new Thickness(1);
        button.Style = CreateRoundedButtonStyle();
        button.Cursor = Cursors.Hand;
    }

    private static Style CreateRoundedButtonStyle()
    {
        Style style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.TemplateProperty, CreateRoundedButtonTemplate()));
        return style;
    }

    private static ControlTemplate CreateRoundedButtonTemplate()
    {
        ControlTemplate template = new ControlTemplate(typeof(Button));

        FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
        border.Name = "border";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

        FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(presenter);

        template.VisualTree = border;

        Trigger disabled = new Trigger();
        disabled.Property = UIElement.IsEnabledProperty;
        disabled.Value = false;
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.58, "border"));
        template.Triggers.Add(disabled);

        return template;
    }

    private void WireEvents()
    {
        _packFolder.TextChanged += delegate { OnPackInputsChanged(); };
        _packOutput.TextChanged += delegate { OnPackInputsChanged(); };
        _unpackFile.TextChanged += delegate { OnUnpackInputsChanged(); };
        _unpackOutput.TextChanged += delegate { OnUnpackInputsChanged(); };
        _inspectFolder.TextChanged += delegate { OnInspectInputsChanged(); };
    }

    private void ChoosePackFolder(object sender, RoutedEventArgs e)
    {
        using (WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog())
        {
            dlg.Description = "Choose folder to pack";
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                _packFolder.Text = dlg.SelectedPath;
                _packOutput.Text = DefaultPackOutput(dlg.SelectedPath);
            }
        }
    }

    private void ChoosePackOutput(object sender, RoutedEventArgs e)
    {
        WinForms.SaveFileDialog dlg = new WinForms.SaveFileDialog();
        dlg.Filter = "PAK archive (*.pak)|*.pak|All files (*.*)|*.*";
        dlg.Title = "Choose output PAK";
        if (!String.IsNullOrWhiteSpace(_packOutput.Text))
            dlg.FileName = _packOutput.Text;
        if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            _packOutput.Text = dlg.FileName;
    }

    private void ChoosePakFile(object sender, RoutedEventArgs e)
    {
        WinForms.OpenFileDialog dlg = new WinForms.OpenFileDialog();
        dlg.Filter = "PAK archive (*.pak)|*.pak|All files (*.*)|*.*";
        dlg.Title = "Choose PAK file";
        if (dlg.ShowDialog() == WinForms.DialogResult.OK)
        {
            _unpackFile.Text = dlg.FileName;
            _unpackOutput.Text = DefaultUnpackOutput(dlg.FileName);
        }
    }

    private void ChooseUnpackOutput(object sender, RoutedEventArgs e)
    {
        using (WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog())
        {
            dlg.Description = "Choose output folder";
            if (!String.IsNullOrWhiteSpace(_unpackOutput.Text) && Directory.Exists(_unpackOutput.Text))
                dlg.SelectedPath = _unpackOutput.Text;
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                _unpackOutput.Text = dlg.SelectedPath;
        }
    }

    private void ChooseInspectFolder(object sender, RoutedEventArgs e)
    {
        using (WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog())
        {
            dlg.Description = "Choose unpacked output folder to inspect";
            if (!String.IsNullOrWhiteSpace(_inspectFolder.Text) && Directory.Exists(_inspectFolder.Text))
                dlg.SelectedPath = _inspectFolder.Text;
            else if (!String.IsNullOrWhiteSpace(_unpackOutput.Text) && Directory.Exists(_unpackOutput.Text))
                dlg.SelectedPath = _unpackOutput.Text;
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                _inspectFolder.Text = dlg.SelectedPath;
        }
    }

    private void ResetPackOutput(object sender, RoutedEventArgs e)
    {
        string value = DefaultPackOutput(_packFolder.Text.Trim());
        if (!String.IsNullOrWhiteSpace(value))
            _packOutput.Text = value;
    }

    private void ResetUnpackOutput(object sender, RoutedEventArgs e)
    {
        string value = DefaultUnpackOutput(_unpackFile.Text.Trim());
        if (!String.IsNullOrWhiteSpace(value))
            _unpackOutput.Text = value;
    }

    private async Task PackAsync()
    {
        string folder = _packFolder.Text.Trim();
        string output = _packOutput.Text.Trim();

        SetStatus("Validating", "Checking source folder and output paths.", UiTone.Busy);
        OperationResult runtime = PakOperations.CheckRuntime();
        _engineReady = runtime.Success;
        if (!_engineReady)
        {
            SetStatus("Engine Missing", runtime.Message, UiTone.Error);
            SetDetails("Pack stopped before invoking the legacy engine.\r\n" + runtime.Message);
            _detailsExpander.IsExpanded = true;
            UpdateActionAvailability();
            return;
        }

        if (!Directory.Exists(folder))
        {
            SetStatus("Needs Input", "Choose an existing folder to pack.", UiTone.Error);
            _detailsExpander.IsExpanded = false;
            return;
        }

        if (String.IsNullOrWhiteSpace(output))
        {
            SetStatus("Needs Input", "Choose where to save the PAK file.", UiTone.Error);
            _detailsExpander.IsExpanded = false;
            return;
        }

        if (!ConfirmPackOverwrite(output))
        {
            SetStatus("Ready", "Packing cancelled before any files were changed.", UiTone.Info);
            return;
        }

        SetBusy("Scanning", "Counting files before handing the folder to the legacy engine.");
        try
        {
            int fileCount;
            string countError;
            if (!TryCountFiles(folder, out fileCount, out countError))
            {
                SetStatus("Error", countError, UiTone.Error);
                SetDetails("Pack failed during preflight.\r\nSource: " + folder + "\r\nOutput: " + output + "\r\nReason: " + countError);
                _detailsExpander.IsExpanded = true;
                return;
            }

            if (fileCount == 0)
            {
                SetStatus("Empty Folder", "The selected folder has no files to pack.", UiTone.Warning);
                SetDetails("Pack stopped before invoking the legacy engine.\r\nSource: " + folder + "\r\nOutput: " + output);
                return;
            }

            SetStatus("Running", "The legacy engine is creating the PAK archive and TXT sidecar.", UiTone.Busy);
            SetDetails("Mode: Pack\r\nSource folder: " + folder + "\r\nOutput PAK: " + output + "\r\nOutput sidecar: " + output + ".txt\r\nFiles discovered: " + fileCount.ToString());
            OperationResult result = await Task.Run(delegate { return PakOperations.Pack(folder, output); });
            if (result.Success)
            {
                SetStatus("Complete", result.Message, UiTone.Success);
                SetDetails("Pack completed.\r\nSource folder: " + folder + "\r\nOutput PAK: " + output + "\r\nOutput sidecar: " + output + ".txt\r\nFiles packed: " + result.FileCount.ToString());
                AddRecent("Packed: " + output);
            }
            else
            {
                SetStatus("Error", result.Message, UiTone.Error);
                SetDetails("Pack failed.\r\nSource folder: " + folder + "\r\nOutput PAK: " + output + "\r\nLegacy engine message: " + result.Message);
                _detailsExpander.IsExpanded = true;
            }
        }
        catch (Exception ex)
        {
            string message = ex.GetType().Name + ": " + ex.Message;
            SetStatus("Error", message, UiTone.Error);
            SetDetails("Pack failed with an unexpected exception.\r\nSource folder: " + folder + "\r\nOutput PAK: " + output + "\r\nException: " + message);
            _detailsExpander.IsExpanded = true;
        }
        finally
        {
            FinishBusy();
            UpdatePackPreflight();
        }
    }

    private async Task UnpackAsync()
    {
        string pak = _unpackFile.Text.Trim();
        string output = _unpackOutput.Text.Trim();

        SetStatus("Validating", "Checking archive, manifest, and output folder.", UiTone.Busy);
        OperationResult runtime = PakOperations.CheckRuntime();
        _engineReady = runtime.Success;
        if (!_engineReady)
        {
            SetStatus("Engine Missing", runtime.Message, UiTone.Error);
            SetDetails("Unpack stopped before invoking the legacy engine.\r\n" + runtime.Message);
            _detailsExpander.IsExpanded = true;
            UpdateActionAvailability();
            return;
        }

        if (!File.Exists(pak))
        {
            SetStatus("Needs Input", "Choose an existing PAK file.", UiTone.Error);
            _detailsExpander.IsExpanded = false;
            return;
        }

        if (String.IsNullOrWhiteSpace(output))
        {
            SetStatus("Needs Input", "Choose an output folder.", UiTone.Error);
            _detailsExpander.IsExpanded = false;
            return;
        }

        if (!ConfirmUnpackOverwrite(output))
        {
            SetStatus("Ready", "Unpacking cancelled before any files were changed.", UiTone.Info);
            return;
        }

        SidecarInfo sidecar = GetSidecarInfo(pak);
        SetBusy("Running", "The legacy engine is extracting the archive.");
        try
        {
            SetDetails("Mode: Unpack\r\nSource PAK: " + pak + "\r\nOutput folder: " + output + "\r\nManifest status: " + sidecar.Title + "\r\nManifest path: " + sidecar.Path);
            OperationResult result = await Task.Run(delegate { return PakOperations.Unpack(pak, output); });
            if (result.Success)
            {
                SetStatus("Inspecting", "Building inventory and recovery summary without rewriting extracted game files.", UiTone.Busy);
                try
                {
                    InventoryResult inventory = await Task.Run(delegate { return InventoryScanner.Scan(output); });
                    string inventoryPath = await Task.Run(delegate { return ReportWriter.WriteInventory(output, inventory); });
                    result.Inventory = inventory;
                    result.InventoryReportPath = inventoryPath;
                    string recoverySummaryPath = await Task.Run(delegate { return ReportWriter.WriteRecoverySummary(output, pak, sidecar.Title, sidecar.Path, result, inventory); });
                    result.RecoverySummaryPath = recoverySummaryPath;
                    _lastInventory = inventory;
                    _lastInventoryPath = inventoryPath;
                    _lastRecoverySummaryPath = recoverySummaryPath;
                    _lastUnpackOutput = output;
                    _inspectFolder.Text = output;
                    DisplayInventory(inventory);
                }
                catch (Exception reportEx)
                {
                    result.RecoverySummary = (result.RecoverySummary == null ? "" : result.RecoverySummary + " ") +
                        "Inventory/report generation failed: " + reportEx.GetType().Name + ".";
                }

                UiTone finalTone = sidecar.Tone == UiTone.Warning || (result.Inventory != null && result.Inventory.UnknownBinaryCount > 0) ? UiTone.Warning : UiTone.Success;
                SetStatus("Complete", BuildCompletionMessage(result), finalTone);
                string details = BuildUnpackDetails(pak, output, sidecar, result);
                UpdateReportsPanel(details);
                SetDetails(details);
                AddRecent("Unpacked: " + output);
            }
            else
            {
                SetStatus("Error", result.Message, UiTone.Error);
                SetDetails("Unpack failed.\r\nSource PAK: " + pak + "\r\nOutput folder: " + output + "\r\nManifest status: " + sidecar.Title + "\r\nLegacy engine message: " + result.Message);
                _detailsExpander.IsExpanded = true;
            }
        }
        catch (Exception ex)
        {
            string message = ex.GetType().Name + ": " + ex.Message;
            SetStatus("Error", message, UiTone.Error);
            SetDetails("Unpack failed with an unexpected exception.\r\nSource PAK: " + pak + "\r\nOutput folder: " + output + "\r\nException: " + message);
            _detailsExpander.IsExpanded = true;
        }
        finally
        {
            FinishBusy();
            UpdateUnpackPreflight();
        }
    }

    private string BuildCompletionMessage(OperationResult result)
    {
        if (result.Inventory == null)
            return result.Message;

        return "Unpacked " + result.FileCount.ToString() + " files. " +
            result.Inventory.BuildShortSummary() +
            ". Unknown means original path missing, not corrupted.";
    }

    private string BuildUnpackDetails(string pak, string output, SidecarInfo sidecar, OperationResult result)
    {
        StringBuilder details = new StringBuilder();
        details.AppendLine("What happened");
        details.AppendLine("The archive was extracted and the output folder was inspected without rewriting extracted game files.");
        details.AppendLine();
        details.AppendLine("Source PAK: " + pak);
        details.AppendLine("Output folder: " + output);
        details.AppendLine("Manifest status: " + sidecar.Title);
        details.AppendLine("Manifest path: " + sidecar.Path);
        details.AppendLine("Total archive entries: " + result.FileCount.ToString());
        details.AppendLine();

        details.AppendLine("What was recovered");
        if (result.Inventory != null)
        {
            details.AppendLine("Recovered Names: " + result.Inventory.ExactNameCount.ToString());
            details.AppendLine("Typed Unknowns: " + result.Inventory.TypedUnknownCount.ToString());
            details.AppendLine("Unknown Binaries: " + result.Inventory.UnknownBinaryCount.ToString());
        }
        else
        {
            details.AppendLine("Inventory unavailable.");
        }

        details.AppendLine();
        details.AppendLine("What is still unknown");
        details.AppendLine("Files under _unknown_by_id are valid extracted entries whose original paths were not recovered. They may still be useful game resources.");
        details.AppendLine("Normal repacking is safest for recovered-name files. Unknown-ID files are uncertain normal repack candidates unless the original path is recovered.");
        details.AppendLine();

        details.AppendLine("Recommended next step");
        details.AppendLine("Use Inspect Output to filter typed unknowns, sprites/resources, and unknown binaries before deciding what to edit.");
        if (!String.IsNullOrWhiteSpace(result.RecoverySummary))
            details.AppendLine("Name recovery: " + result.RecoverySummary);
        if (!String.IsNullOrWhiteSpace(result.InventoryReportPath))
            details.AppendLine("Inventory: " + result.InventoryReportPath);
        if (!String.IsNullOrWhiteSpace(result.RecoverySummaryPath))
            details.AppendLine("Recovery summary: " + result.RecoverySummaryPath);
        if (!String.IsNullOrWhiteSpace(result.RecoveryReportPath))
            details.AppendLine("Recovery report: " + result.RecoveryReportPath);

        return details.ToString();
    }

    private void SetControls(bool enabled)
    {
        for (int i = 0; i < _inputControls.Count; i++)
            _inputControls[i].IsEnabled = enabled;
        UpdateActionAvailability();
    }

    private void SetBusy(string phase, string message)
    {
        _isBusy = true;
        SetControls(false);
        _progress.IsIndeterminate = true;
        _progress.Visibility = Visibility.Visible;
        SetStatus(phase, message, UiTone.Busy);
    }

    private void FinishBusy()
    {
        _isBusy = false;
        _progress.Visibility = Visibility.Collapsed;
        SetControls(true);
    }

    private void SetStatus(string phase, string message, UiTone tone)
    {
        _phaseText.Text = phase;
        _status.Text = message;

        Brush background;
        Brush border;
        Brush foreground;
        GetToneBrushes(tone, out background, out border, out foreground);
        _phaseBadge.Background = background;
        _phaseBadge.BorderBrush = border;
        _phaseBadge.BorderThickness = new Thickness(1);
        _phaseText.Foreground = foreground;
        _status.Foreground = foreground;
    }

    private void SetDetails(string text)
    {
        _lastDetails = text == null ? "" : text;
        _detailsBox.Text = _lastDetails;
        bool hasDetails = !String.IsNullOrWhiteSpace(_lastDetails);
        _copyDetailsButton.IsEnabled = hasDetails;
        _detailsExpander.Visibility = hasDetails ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetInfoPanel(Border panel, TextBlock title, TextBlock body, string titleText, string bodyText, UiTone tone)
    {
        Brush background;
        Brush border;
        Brush foreground;
        GetToneBrushes(tone, out background, out border, out foreground);
        panel.Background = background;
        panel.BorderBrush = border;
        title.Foreground = foreground;
        body.Foreground = Rgb(183, 196, 213);
        title.Text = "03 " + titleText;
        body.Text = bodyText;
    }

    private void SwitchMode(bool pack)
    {
        SwitchMode(pack ? WorkbenchMode.Pack : WorkbenchMode.Unpack);
    }

    private void SwitchMode(WorkbenchMode mode)
    {
        _activeMode = mode;
        _isPackMode = mode == WorkbenchMode.Pack;

        if (mode == WorkbenchMode.Pack)
            _modeContent.Content = _packView;
        else if (mode == WorkbenchMode.Unpack)
            _modeContent.Content = _unpackView;
        else if (mode == WorkbenchMode.Inspect)
            _modeContent.Content = _inspectView;
        else if (mode == WorkbenchMode.Reports)
            _modeContent.Content = _reportsView;
        else
        {
            UpdateReferenceSourcesPanel();
            _modeContent.Content = _settingsView;
        }

        SetSegmentState(_packModeButton, mode == WorkbenchMode.Pack);
        SetSegmentState(_unpackModeButton, mode == WorkbenchMode.Unpack);
        SetSegmentState(_inspectModeButton, mode == WorkbenchMode.Inspect);
        SetSegmentState(_reportsModeButton, mode == WorkbenchMode.Reports);
        SetSegmentState(_settingsModeButton, mode == WorkbenchMode.Settings);
        _packButton.IsDefault = mode == WorkbenchMode.Pack;
        _unpackButton.IsDefault = mode == WorkbenchMode.Unpack;
        _inspectScanButton.IsDefault = mode == WorkbenchMode.Inspect;
        UpdateActionAvailability();
    }

    private void SetSegmentState(Button button, bool selected)
    {
        button.Background = selected ? Argb(92, 9, 38, 82) : Argb(18, 7, 12, 22);
        button.Foreground = selected ? Rgb(246, 250, 255) : Rgb(151, 166, 187);
        button.BorderBrush = selected ? Argb(150, 42, 180, 235) : Argb(58, 86, 108, 140);
        button.BorderThickness = new Thickness(1);
        button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
    }

    private void OnPackInputsChanged()
    {
        UpdateToolTip(_packFolder);
        UpdateToolTip(_packOutput);
        UpdatePackPreflight();
        UpdateActionAvailability();
    }

    private void OnUnpackInputsChanged()
    {
        UpdateToolTip(_unpackFile);
        UpdateToolTip(_unpackOutput);
        UpdateUnpackPreflight();
        UpdateReferenceSourcesPanel();
        UpdateActionAvailability();
    }

    private void OnInspectInputsChanged()
    {
        UpdateToolTip(_inspectFolder);
        UpdateActionAvailability();
    }

    private void UpdatePackPreflight()
    {
        if (!_engineReady)
        {
            SetInfoPanel(_packInfoPanel, _packInfoTitle, _packInfoBody, "Engine unavailable", "The app can open, but pack and unpack actions need the legacy engine files to be installed correctly.", UiTone.Error);
            return;
        }

        string folder = _packFolder.Text.Trim();
        string output = _packOutput.Text.Trim();

        if (String.IsNullOrWhiteSpace(folder))
        {
            SetInfoPanel(_packInfoPanel, _packInfoTitle, _packInfoBody, "Source required", "Choose or drop a folder. The app will suggest a sibling .pak output path.", UiTone.Info);
            return;
        }

        if (!Directory.Exists(folder))
        {
            SetInfoPanel(_packInfoPanel, _packInfoTitle, _packInfoBody, "Folder not found", "The source folder does not exist. No archive can be created from this path.", UiTone.Error);
            return;
        }

        int fileCount;
        string countError;
        string countText = "File count unavailable";
        UiTone tone = UiTone.Success;
        if (TryCountFiles(folder, out fileCount, out countError))
        {
            countText = fileCount.ToString() + " files found";
            if (fileCount == 0)
                tone = UiTone.Warning;
        }
        else
        {
            countText = countError;
            tone = UiTone.Warning;
        }

        string sidecar = String.IsNullOrWhiteSpace(output) ? "Choose a destination to preview the TXT sidecar path." : output + ".txt";
        string overwrite = "";
        if (!String.IsNullOrWhiteSpace(output))
        {
            if (File.Exists(output))
                overwrite += " The PAK already exists.";
            if (File.Exists(output + ".txt"))
                overwrite += " The TXT sidecar already exists.";
            if (overwrite.Length > 0)
                tone = UiTone.Warning;
        }

        SetInfoPanel(_packInfoPanel, _packInfoTitle, _packInfoBody, "Pack preflight", countText + ". Sidecar target: " + sidecar + overwrite, tone);
    }

    private void UpdateUnpackPreflight()
    {
        if (!_engineReady)
        {
            SetInfoPanel(_unpackInfoPanel, _unpackInfoTitle, _unpackInfoBody, "Engine unavailable", "The app can open, but pack and unpack actions need the legacy engine files to be installed correctly.", UiTone.Error);
            return;
        }

        string pak = _unpackFile.Text.Trim();
        string output = _unpackOutput.Text.Trim();

        if (String.IsNullOrWhiteSpace(pak))
        {
            SetInfoPanel(_unpackInfoPanel, _unpackInfoTitle, _unpackInfoBody, "Archive required", "Choose or drop a .pak file. Manifest status will be checked before extraction.", UiTone.Info);
            return;
        }

        if (!File.Exists(pak))
        {
            SetInfoPanel(_unpackInfoPanel, _unpackInfoTitle, _unpackInfoBody, "PAK not found", "The selected PAK file does not exist.", UiTone.Error);
            return;
        }

        SidecarInfo sidecar = GetSidecarInfo(pak);
        string outputText = String.IsNullOrWhiteSpace(output) ? " Choose an output folder." : " Output: " + output;
        SetInfoPanel(_unpackInfoPanel, _unpackInfoTitle, _unpackInfoBody, sidecar.Title, sidecar.Message + outputText, sidecar.Tone);
    }

    private void UpdateActionAvailability()
    {
        bool enabled = !_isBusy && _engineReady;
        _packButton.IsEnabled = enabled && Directory.Exists(_packFolder.Text.Trim()) && !String.IsNullOrWhiteSpace(_packOutput.Text.Trim());
        _unpackButton.IsEnabled = enabled && File.Exists(_unpackFile.Text.Trim()) && !String.IsNullOrWhiteSpace(_unpackOutput.Text.Trim());
        _packOpenButton.IsEnabled = enabled && CanOpenPackOutput();
        _unpackOpenButton.IsEnabled = enabled && Directory.Exists(_unpackOutput.Text.Trim());
        _unpackInspectButton.IsEnabled = enabled && Directory.Exists(_unpackOutput.Text.Trim());
        _packClearButton.IsEnabled = enabled && (!String.IsNullOrWhiteSpace(_packFolder.Text) || !String.IsNullOrWhiteSpace(_packOutput.Text));
        _unpackClearButton.IsEnabled = enabled && (!String.IsNullOrWhiteSpace(_unpackFile.Text) || !String.IsNullOrWhiteSpace(_unpackOutput.Text));
        _packDefaultButton.IsEnabled = enabled && Directory.Exists(_packFolder.Text.Trim());
        _unpackDefaultButton.IsEnabled = enabled && File.Exists(_unpackFile.Text.Trim());
        _inspectScanButton.IsEnabled = enabled && Directory.Exists(_inspectFolder.Text.Trim());
        _inspectSaveButton.IsEnabled = enabled && _lastInventory != null;
        _inspectOpenOutputButton.IsEnabled = enabled && Directory.Exists(_inspectFolder.Text.Trim());
        UpdateInventoryActionAvailability();
        UpdateReportsActionAvailability();
    }

    private void ClearPack()
    {
        _packFolder.Clear();
        _packOutput.Clear();
        SetStatus("Ready", "Pack inputs cleared.", UiTone.Info);
    }

    private void ClearUnpack()
    {
        _unpackFile.Clear();
        _unpackOutput.Clear();
        SetStatus("Ready", "Unpack inputs cleared.", UiTone.Info);
    }

    private void OpenPackOutput()
    {
        string output = _packOutput.Text.Trim();
        if (File.Exists(output))
        {
            OpenExplorer("/select," + QuoteForExplorer(output));
            return;
        }

        string parent = Path.GetDirectoryName(output);
        if (!String.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
        {
            OpenExplorer(QuoteForExplorer(parent));
            return;
        }

        SetStatus("Unavailable", "The output location does not exist yet.", UiTone.Warning);
    }

    private void OpenUnpackOutput()
    {
        string output = _unpackOutput.Text.Trim();
        if (Directory.Exists(output))
            OpenExplorer(QuoteForExplorer(output));
        else
            SetStatus("Unavailable", "The output folder does not exist yet.", UiTone.Warning);
    }

    private void OpenInspectFolder()
    {
        string output = _inspectFolder.Text.Trim();
        if (Directory.Exists(output))
            OpenExplorer(QuoteForExplorer(output));
        else
            SetStatus("Unavailable", "Choose an existing folder to inspect.", UiTone.Warning);
    }

    private async Task InspectUnpackOutputAsync()
    {
        string output = _unpackOutput.Text.Trim();
        if (!Directory.Exists(output))
        {
            SetStatus("Unavailable", "The unpack output folder does not exist yet.", UiTone.Warning);
            return;
        }

        _inspectFolder.Text = output;
        SwitchMode(WorkbenchMode.Inspect);
        if (_lastInventory != null && String.Equals(_lastInventory.RootPath, output, StringComparison.OrdinalIgnoreCase))
        {
            DisplayInventory(_lastInventory);
            return;
        }

        await ScanInspectFolderAsync();
    }

    private async Task ScanInspectFolderAsync()
    {
        string folder = _inspectFolder.Text.Trim();
        if (!Directory.Exists(folder))
        {
            SetStatus("Needs Input", "Choose an existing unpacked output folder to inspect.", UiTone.Error);
            return;
        }

        SetBusy("Inspecting", "Scanning extracted files and classifying recovered names, typed unknowns, and unknown binaries.");
        try
        {
            InventoryResult inventory = await Task.Run(delegate { return InventoryScanner.Scan(folder); });
            string inventoryPath = await Task.Run(delegate { return ReportWriter.WriteInventory(folder, inventory); });
            _lastInventory = inventory;
            _lastInventoryPath = inventoryPath;
            _lastRecoverySummaryPath = Path.Combine(folder, "_pak_tool_recovery_summary.txt");
            _lastUnpackOutput = folder;
            DisplayInventory(inventory);
            UpdateReportsPanel("Inspect completed.\r\nOutput folder: " + folder + "\r\nInventory: " + inventoryPath + "\r\n" + inventory.BuildShortSummary());
            AddRecent("Inspected: " + folder);
            SetStatus("Inventory Ready", inventory.BuildShortSummary(), inventory.UnknownBinaryCount > 0 ? UiTone.Warning : UiTone.Success);
        }
        catch (Exception ex)
        {
            string message = ex.GetType().Name + ": " + ex.Message;
            SetStatus("Error", message, UiTone.Error);
            SetDetails("Inspect failed.\r\nFolder: " + folder + "\r\nException: " + message);
            _detailsExpander.IsExpanded = true;
        }
        finally
        {
            FinishBusy();
            UpdateActionAvailability();
        }
    }

    private void SaveCurrentInventory()
    {
        if (_lastInventory == null || String.IsNullOrWhiteSpace(_lastInventory.RootPath) || !Directory.Exists(_lastInventory.RootPath))
        {
            SetStatus("Unavailable", "Scan a folder before saving an inventory.", UiTone.Warning);
            return;
        }

        try
        {
            _lastInventoryPath = ReportWriter.WriteInventory(_lastInventory.RootPath, _lastInventory);
            SetStatus("Saved", "Inventory written to " + _lastInventoryPath, UiTone.Success);
            UpdateReportsPanel(_reportsBox.Text);
        }
        catch (Exception ex)
        {
            SetStatus("Error", ex.GetType().Name + ": " + ex.Message, UiTone.Error);
        }
    }

    private void DisplayInventory(InventoryResult inventory)
    {
        _lastInventory = inventory;
        _inspectSummary.Text = inventory.BuildShortSummary() + "\r\nUnknown does not mean corrupted. Typed unknown files have valid extracted bytes, but their original game paths are missing.";
        ApplyInventoryFilter();
        UpdateInventoryDetails();
        UpdateActionAvailability();
    }

    private void ApplyInventoryFilter()
    {
        if (_inventoryGrid == null || _lastInventory == null)
            return;

        string filter = _inspectFilter.SelectedItem == null ? "All" : _inspectFilter.SelectedItem.ToString();
        string search = _inspectSearch.Text.Trim().ToLowerInvariant();
        List<InventoryEntry> filtered = new List<InventoryEntry>();
        for (int i = 0; i < _lastInventory.Entries.Count; i++)
        {
            InventoryEntry entry = _lastInventory.Entries[i];
            if (!MatchesInventoryFilter(entry, filter))
                continue;
            if (!MatchesInventorySearch(entry, search))
                continue;
            filtered.Add(entry);
        }

        _inventoryGrid.ItemsSource = null;
        _inventoryGrid.ItemsSource = filtered;
        UpdateInventoryDetails();
    }

    private bool MatchesInventoryFilter(InventoryEntry entry, string filter)
    {
        if (filter == "Recovered names")
            return entry.Status == "ExactName";
        if (filter == "Typed unknowns")
            return entry.Status == "TypedUnknown";
        if (filter == "Unknown binary")
            return entry.Status == "UnknownBinary";
        if (filter == "Text/config/script")
            return entry.EditableHint == "text";
        if (filter == "Sprites/resources")
            return entry.DetectedType == "spr" || entry.DetectedType == "asf";
        if (filter == "Risky/uncertain")
            return entry.RepackSafety == "uncertain" || entry.RepackSafety == "unsafe";
        return true;
    }

    private bool MatchesInventorySearch(InventoryEntry entry, string search)
    {
        if (String.IsNullOrWhiteSpace(search))
            return true;
        return Contains(entry.RelativePath, search) ||
            Contains(entry.ArchiveId, search) ||
            Contains(entry.Extension, search) ||
            Contains(entry.DetectedType, search) ||
            Contains(entry.Status, search);
    }

    private static bool Contains(string value, string search)
    {
        return value != null && value.ToLowerInvariant().IndexOf(search, StringComparison.Ordinal) >= 0;
    }

    private InventoryEntry GetSelectedInventoryEntry()
    {
        return _inventoryGrid.SelectedItem as InventoryEntry;
    }

    private string GetSelectedInventoryPath()
    {
        InventoryEntry entry = GetSelectedInventoryEntry();
        if (entry == null || _lastInventory == null)
            return "";
        return Path.Combine(_lastInventory.RootPath, entry.RelativePath);
    }

    private void UpdateInventoryDetails()
    {
        InventoryEntry entry = GetSelectedInventoryEntry();
        _inventoryDetails.Text = InventoryNarrator.Describe(entry);
        UpdateInventoryActionAvailability();
    }

    private void UpdateInventoryActionAvailability()
    {
        bool enabled = !_isBusy && GetSelectedInventoryEntry() != null;
        InventoryEntry entry = GetSelectedInventoryEntry();
        string selectedPath = GetSelectedInventoryPath();
        _inspectOpenFileButton.IsEnabled = enabled && File.Exists(selectedPath) && entry.Status != "UnknownBinary";
        _inspectOpenFolderButton.IsEnabled = enabled && File.Exists(selectedPath);
        _inspectCopyPathButton.IsEnabled = enabled;
        _inspectCopyIdButton.IsEnabled = enabled && !String.IsNullOrWhiteSpace(entry.ArchiveId);
        _inspectShowDetailsButton.IsEnabled = enabled;
    }

    private void OpenSelectedInventoryFile()
    {
        InventoryEntry entry = GetSelectedInventoryEntry();
        if (entry != null && entry.Status == "UnknownBinary")
        {
            OpenSelectedInventoryFolder();
            return;
        }

        string path = GetSelectedInventoryPath();
        if (File.Exists(path))
            Process.Start(path);
    }

    private void OpenSelectedInventoryFolder()
    {
        string path = GetSelectedInventoryPath();
        if (File.Exists(path))
            OpenExplorer("/select," + QuoteForExplorer(path));
    }

    private void CopySelectedInventoryPath()
    {
        string path = GetSelectedInventoryPath();
        if (!String.IsNullOrWhiteSpace(path))
        {
            Clipboard.SetText(path);
            SetStatus("Copied", "File path copied to the clipboard.", UiTone.Info);
        }
    }

    private void CopySelectedInventoryId()
    {
        InventoryEntry entry = GetSelectedInventoryEntry();
        if (entry != null && !String.IsNullOrWhiteSpace(entry.ArchiveId))
        {
            Clipboard.SetText(entry.ArchiveId);
            SetStatus("Copied", "Archive ID copied to the clipboard.", UiTone.Info);
        }
    }

    private void ShowSelectedInventoryDetails()
    {
        InventoryEntry entry = GetSelectedInventoryEntry();
        if (entry == null)
            return;
        SetDetails(InventoryNarrator.Describe(entry));
        _detailsExpander.IsExpanded = true;
        SetStatus("Details", "Selected file details are shown in the operation details panel.", UiTone.Info);
    }

    private void CopyReportsText()
    {
        if (!String.IsNullOrWhiteSpace(_reportsBox.Text))
        {
            Clipboard.SetText(_reportsBox.Text);
            SetStatus("Copied", "Report text copied to the clipboard.", UiTone.Info);
        }
    }

    private void OpenReportFile(string path)
    {
        if (File.Exists(path))
            Process.Start(path);
        else
            SetStatus("Unavailable", "The report file does not exist yet.", UiTone.Warning);
    }

    private void UpdateReportsPanel(string text)
    {
        _reportsBox.Text = text;
        UpdateReportsActionAvailability();
    }

    private void UpdateReportsActionAvailability()
    {
        bool enabled = !_isBusy;
        _reportsCopyButton.IsEnabled = enabled && !String.IsNullOrWhiteSpace(_reportsBox.Text);
        _reportsOpenInventoryButton.IsEnabled = enabled && File.Exists(_lastInventoryPath);
        _reportsOpenSummaryButton.IsEnabled = enabled && File.Exists(_lastRecoverySummaryPath);
    }

    private void UpdateReferenceSourcesPanel()
    {
        if (_settingsReferenceBox == null)
            return;

        string pak = _unpackFile.Text.Trim();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Reference Sources");
        builder.AppendLine();
        if (String.IsNullOrWhiteSpace(pak) || !File.Exists(pak))
        {
            builder.AppendLine("Choose a PAK file in Unpack mode to see where reference manifests will be searched.");
        }
        else
        {
            builder.AppendLine("Source PAK: " + pak);
            builder.AppendLine();
            List<string> candidates = ExactNameRecovery.GetReferenceManifestCandidates(pak);
            if (candidates.Count == 0)
                builder.AppendLine("No candidate reference manifest paths could be derived.");
            for (int i = 0; i < candidates.Count; i++)
            {
                string marker = File.Exists(candidates[i]) ? "found" : "missing";
                builder.AppendLine(marker + "\t" + candidates[i]);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Future dictionary recovery should remain read-only first: scan candidate trees, hash paths, report exact matches, and never auto-rename low-confidence files.");
        _settingsReferenceBox.Text = builder.ToString();
    }

    private void OpenExplorer(string arguments)
    {
        try
        {
            Process.Start("explorer.exe", arguments);
        }
        catch (Exception ex)
        {
            SetStatus("Error", ex.GetType().Name + ": " + ex.Message, UiTone.Error);
        }
    }

    private static string QuoteForExplorer(string path)
    {
        return "\"" + path.Replace("\"", "\\\"") + "\"";
    }

    private void CopyDetails()
    {
        if (String.IsNullOrWhiteSpace(_lastDetails))
            return;

        Clipboard.SetText(_lastDetails);
        SetStatus("Copied", "Operation details copied to the clipboard.", UiTone.Info);
    }

    private void AddRecent(string item)
    {
        for (int i = _recentItems.Count - 1; i >= 0; i--)
        {
            if (String.Equals(_recentItems[i], item, StringComparison.OrdinalIgnoreCase))
                _recentItems.RemoveAt(i);
        }

        _recentItems.Insert(0, item);
        while (_recentItems.Count > 4)
            _recentItems.RemoveAt(_recentItems.Count - 1);

        _recentText.Text = String.Join("\r\n", _recentItems.ToArray());
        _recentPanel.Visibility = Visibility.Visible;
    }

    private bool ConfirmPackOverwrite(string output)
    {
        bool pakExists = File.Exists(output);
        bool sidecarExists = File.Exists(output + ".txt");
        if (!pakExists && !sidecarExists)
            return true;

        string message = "This will overwrite existing output:";
        if (pakExists)
            message += "\r\n- " + output;
        if (sidecarExists)
            message += "\r\n- " + output + ".txt";
        message += "\r\n\r\nContinue?";
        return MessageBox.Show(this, message, "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private bool ConfirmUnpackOverwrite(string output)
    {
        if (!Directory.Exists(output))
            return true;

        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(output);
        }
        catch
        {
            entries = new string[0];
        }

        if (entries.Length == 0)
            return true;

        string message = "The output folder already contains files. Unpacking may overwrite matching paths:\r\n" + output + "\r\n\r\nContinue?";
        return MessageBox.Show(this, message, "Confirm output folder", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private static bool TryCountFiles(string folder, out int count, out string error)
    {
        count = 0;
        error = null;
        try
        {
            foreach (string ignored in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                count++;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }

    private SidecarInfo GetSidecarInfo(string pak)
    {
        string nativeSidecar = pak + ".txt";
        if (File.Exists(nativeSidecar))
        {
            return new SidecarInfo
            {
                Title = "Original filenames available",
                Message = "Native manifest found: " + nativeSidecar,
                Path = nativeSidecar,
                Tone = UiTone.Success
            };
        }

        string alternateSidecar = Path.ChangeExtension(pak, ".txt");
        if (!String.Equals(alternateSidecar, nativeSidecar, StringComparison.OrdinalIgnoreCase) && File.Exists(alternateSidecar))
        {
            return new SidecarInfo
            {
                Title = "Compatible manifest found",
                Message = "Alternate TXT manifest found: " + alternateSidecar,
                Path = alternateSidecar,
                Tone = UiTone.Success
            };
        }

        string referenceManifest = ExactNameRecovery.FindReferenceManifest(pak);
        if (!String.IsNullOrEmpty(referenceManifest))
        {
            ExactNameManifestStats stats = ExactNameRecovery.InspectManifest(referenceManifest);
            return new SidecarInfo
            {
                Title = "Reference names available",
                Message = "No TXT sidecar was found, but a reference unpack manifest is available. Known paths: " + stats.NamedFiles.ToString() + "/" + stats.TotalFiles.ToString() + ". Unmapped IDs: " + stats.UnmappedFiles.ToString() + " preserved under _unknown_by_id with inferred extensions where signatures or text patterns are clear.",
                Path = referenceManifest,
                Tone = UiTone.Success
            };
        }

        return new SidecarInfo
        {
            Title = "Original filenames unavailable",
            Message = "No TXT manifest was found. Extraction will use generated _ID_ filenames and add safe extensions where file signatures are clear.",
            Path = "(generated at runtime)",
            Tone = UiTone.Warning
        };
    }

    private static string DefaultPackOutput(string folder)
    {
        if (String.IsNullOrWhiteSpace(folder))
            return "";

        try
        {
            DirectoryInfo info = new DirectoryInfo(Path.GetFullPath(folder));
            string name = String.IsNullOrWhiteSpace(info.Name) ? "archive" : info.Name;
            string parent = info.Parent != null ? info.Parent.FullName : info.FullName;
            return Path.Combine(parent, name + ".pak");
        }
        catch
        {
            return "";
        }
    }

    private static string DefaultUnpackOutput(string pak)
    {
        if (String.IsNullOrWhiteSpace(pak))
            return "";

        try
        {
            string parent = Path.GetDirectoryName(Path.GetFullPath(pak));
            string name = Path.GetFileNameWithoutExtension(pak);
            return Path.Combine(parent, name);
        }
        catch
        {
            return "";
        }
    }

    private bool CanOpenPackOutput()
    {
        string output = _packOutput.Text.Trim();
        if (String.IsNullOrWhiteSpace(output))
            return false;

        if (File.Exists(output))
            return true;

        try
        {
            string parent = Path.GetDirectoryName(output);
            return !String.IsNullOrWhiteSpace(parent) && Directory.Exists(parent);
        }
        catch
        {
            return false;
        }
    }

    private void UpdateToolTip(TextBox box)
    {
        string value = box.Text.Trim();
        box.ToolTip = String.IsNullOrWhiteSpace(value) ? "Path" : value;
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) || _isBusy)
            return;

        string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (paths == null || paths.Length == 0)
            return;

        string path = paths[0];
        if (Directory.Exists(path))
        {
            if (_activeMode == WorkbenchMode.Inspect)
            {
                _inspectFolder.Text = path;
                SetStatus("Ready", "Folder loaded for inspection.", UiTone.Info);
            }
            else
            {
                SwitchMode(WorkbenchMode.Pack);
                _packFolder.Text = path;
                _packOutput.Text = DefaultPackOutput(path);
                SetStatus("Ready", "Folder loaded for packing.", UiTone.Info);
            }
            return;
        }

        if (File.Exists(path) && String.Equals(Path.GetExtension(path), ".pak", StringComparison.OrdinalIgnoreCase))
        {
            SwitchMode(WorkbenchMode.Unpack);
            _unpackFile.Text = path;
            _unpackOutput.Text = DefaultUnpackOutput(path);
            SetStatus("Ready", "PAK loaded for unpacking.", UiTone.Info);
            return;
        }

        SetStatus("Unsupported Drop", "Drop a folder for Pack mode or a .pak file for Unpack mode.", UiTone.Warning);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (_isBusy)
            return;

        if (e.Key == Key.Escape)
        {
            if (_activeMode == WorkbenchMode.Pack)
                ClearPack();
            else if (_activeMode == WorkbenchMode.Unpack)
                ClearUnpack();
            else if (_activeMode == WorkbenchMode.Inspect)
                _inspectSearch.Clear();
            e.Handled = true;
        }
    }

    private void EnableDarkTitleBar()
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int enabled = 1;
            DwmSetWindowAttribute(handle, 20, ref enabled, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(handle, 19, ref enabled, Marshal.SizeOf(typeof(int)));
        }
        catch
        {
            // Older Windows builds can ignore this; the WPF content remains fully styled.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    private void ConfigureInteractiveBackground()
    {
        _interactiveBackground.StartPoint = new Point(0.04, 0.02);
        _interactiveBackground.EndPoint = new Point(1.0, 1.0);
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(2, 5, 11), 0.0));
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(7, 12, 22), 0.48));
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(3, 7, 16), 1.0));
    }

    private void OnGlassMouseMove(object sender, MouseEventArgs e)
    {
        FrameworkElement element = sender as FrameworkElement;
        if (element == null || element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return;

        Point position = e.GetPosition(element);
        double x = Clamp(position.X / element.ActualWidth);
        double y = Clamp(position.Y / element.ActualHeight);
        UpdateGlassGradient(x, y);
    }

    private void UpdateGlassGradient(double x, double y)
    {
        x = Clamp(x);
        y = Clamp(y);
        _interactiveBackground.StartPoint = new Point(0.02 + x * 0.22, 0.02 + y * 0.18);
        _interactiveBackground.EndPoint = new Point(0.96 - x * 0.16, 0.98 - y * 0.18);

        _interactiveBackground.GradientStops[0].Color = Lerp(
            Color.FromRgb(2, 5, 11),
            Color.FromRgb(5, 13, 28),
            x);
        _interactiveBackground.GradientStops[1].Color = Lerp(
            Color.FromRgb(7, 12, 22),
            Color.FromRgb(8, 21, 42),
            y);
        _interactiveBackground.GradientStops[2].Color = Lerp(
            Color.FromRgb(3, 7, 16),
            Color.FromRgb(6, 17, 34),
            (x + y) / 2.0);
    }

    private void Track(UIElement element)
    {
        _inputControls.Add(element);
    }

    private static void GetToneBrushes(UiTone tone, out Brush background, out Brush border, out Brush foreground)
    {
        if (tone == UiTone.Success)
        {
            background = Argb(86, 16, 88, 62);
            border = Argb(160, 44, 199, 137);
            foreground = Rgb(101, 238, 178);
            return;
        }

        if (tone == UiTone.Warning)
        {
            background = Argb(82, 115, 78, 18);
            border = Argb(156, 238, 180, 65);
            foreground = Rgb(246, 201, 112);
            return;
        }

        if (tone == UiTone.Error)
        {
            background = Argb(82, 112, 28, 32);
            border = Argb(160, 237, 96, 96);
            foreground = Rgb(255, 147, 147);
            return;
        }

        if (tone == UiTone.Busy)
        {
            background = Argb(88, 10, 70, 160);
            border = Argb(170, 88, 166, 255);
            foreground = Rgb(156, 206, 255);
            return;
        }

        background = Argb(78, 18, 27, 42);
        border = Argb(132, 91, 119, 154);
        foreground = Rgb(215, 225, 240);
    }

    private static SolidColorBrush Rgb(int r, int g, int b)
    {
        return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
    }

    private static SolidColorBrush Argb(int a, int r, int g, int b)
    {
        return new SolidColorBrush(Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
    }

    private static LinearGradientBrush CreateGlassSurfaceBrush()
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0, 0);
        brush.EndPoint = new Point(1, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(190, 13, 20, 34), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(144, 8, 13, 24), 0.58));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(176, 16, 26, 43), 1.0));
        return brush;
    }

    private static LinearGradientBrush CreateButtonGlassBrush()
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0, 0);
        brush.EndPoint = new Point(0, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(76, 30, 43, 64), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(124, 10, 16, 27), 1.0));
        return brush;
    }

    private static LinearGradientBrush CreatePrimaryGlassBrush()
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0, 0);
        brush.EndPoint = new Point(1, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 114, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(10, 82, 214), 0.56));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(59, 177, 255), 1.0));
        return brush;
    }

    private static DrawingBrush CreateGridLineBrush()
    {
        DrawingGroup group = new DrawingGroup();
        Pen majorPen = new Pen(Argb(42, 101, 132, 170), 1);
        Pen minorPen = new Pen(Argb(22, 101, 132, 170), 1);
        group.Children.Add(new GeometryDrawing(null, minorPen, new LineGeometry(new Point(0, 36), new Point(72, 36))));
        group.Children.Add(new GeometryDrawing(null, minorPen, new LineGeometry(new Point(36, 0), new Point(36, 72))));
        group.Children.Add(new GeometryDrawing(null, majorPen, new LineGeometry(new Point(0, 0), new Point(72, 0))));
        group.Children.Add(new GeometryDrawing(null, majorPen, new LineGeometry(new Point(0, 0), new Point(0, 72))));

        DrawingBrush brush = new DrawingBrush(group);
        brush.TileMode = TileMode.Tile;
        brush.Viewport = new Rect(0, 0, 72, 72);
        brush.ViewportUnits = BrushMappingMode.Absolute;
        brush.Stretch = Stretch.None;
        return brush;
    }

    private static RadialGradientBrush CreateRadialGlowBrush()
    {
        RadialGradientBrush brush = new RadialGradientBrush();
        brush.Center = new Point(0.52, 0.48);
        brush.GradientOrigin = new Point(0.45, 0.42);
        brush.RadiusX = 0.62;
        brush.RadiusY = 0.58;
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(118, 0, 132, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(44, 0, 94, 210), 0.5));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0));
        return brush;
    }

    private static LinearGradientBrush CreateRibbonBrush(Color start, Color end)
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0.02, 0.08);
        brush.EndPoint = new Point(1, 1);
        brush.GradientStops.Add(new GradientStop(start, 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(220, 22, 104, 255), 0.48));
        brush.GradientStops.Add(new GradientStop(end, 1.0));
        return brush;
    }

    private static double Clamp(double value)
    {
        if (value < 0)
            return 0;
        if (value > 1)
            return 1;
        return value;
    }

    private static Color Lerp(Color start, Color end, double amount)
    {
        amount = Clamp(amount);
        return Color.FromRgb(
            (byte)(start.R + (end.R - start.R) * amount),
            (byte)(start.G + (end.G - start.G) * amount),
            (byte)(start.B + (end.B - start.B) * amount));
    }

    private enum WorkbenchMode
    {
        Pack,
        Unpack,
        Inspect,
        Reports,
        Settings
    }

    private enum UiTone
    {
        Info,
        Success,
        Warning,
        Error,
        Busy
    }

    private sealed class SidecarInfo
    {
        public string Title;
        public string Message;
        public string Path;
        public UiTone Tone;
    }
}

internal sealed class OperationResult
{
    public bool Success;
    public string Message;
    public int FileCount;
    public string RecoverySummary;
    public string RecoveryReportPath;
    public InventoryResult Inventory;
    public string InventoryReportPath;
    public string RecoverySummaryPath;
}

internal static class RuntimeLayout
{
    private const string EngineFolderName = "Engine";
    private const string HostFileName = "PakEngineHost.exe";
    private const string EngineDllFileName = "engine.dll";
    private const string LuaDllFileName = "lualibdll.dll";

    public static string FindEngineHost()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string installedHost = Path.Combine(Path.Combine(appDir, EngineFolderName), HostFileName);
        if (File.Exists(installedHost))
            return installedHost;

        string adjacentHost = Path.Combine(appDir, HostFileName);
        if (File.Exists(adjacentHost))
            return adjacentHost;

        return installedHost;
    }

    public static string GetEngineDirectory()
    {
        return Path.GetDirectoryName(FindEngineHost());
    }

    public static OperationResult Check()
    {
        string host = FindEngineHost();
        string engineDir = Path.GetDirectoryName(host);
        List<string> missing = new List<string>();

        AddMissing(missing, host);
        AddMissing(missing, Path.Combine(engineDir, EngineDllFileName));
        AddMissing(missing, Path.Combine(engineDir, LuaDllFileName));

        if (missing.Count > 0)
        {
            return new OperationResult
            {
                Success = false,
                Message = "The legacy PAK engine could not be loaded. Missing: " + String.Join(", ", missing.ToArray()) + ". Reinstall the app or rebuild it before packing/unpacking."
            };
        }

        return new OperationResult { Success = true, Message = "Legacy PAK engine files are available." };
    }

    private static void AddMissing(List<string> missing, string path)
    {
        if (!File.Exists(path))
            missing.Add(MakeDisplayPath(path));
    }

    private static string MakeDisplayPath(string path)
    {
        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullAppDir = Path.GetFullPath(appDir);
            string fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(fullAppDir, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(fullAppDir.Length).TrimStart('\\', '/');
            return fullPath;
        }
        catch
        {
            return path;
        }
    }
}

internal static class PakOperations
{
    public static OperationResult CheckRuntime()
    {
        return RuntimeLayout.Check();
    }

    public static OperationResult Pack(string folder, string outputPak)
    {
        OperationResult runtime = CheckRuntime();
        if (!runtime.Success)
            return runtime;

        string tempDir = Path.Combine(Path.GetTempPath(), "modern-pak-tool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "The selected folder has no files to pack.",
                    FileCount = 0
                };
            }

            string tempPak = Path.Combine(tempDir, Path.GetFileName(outputPak));

            string host = RuntimeLayout.FindEngineHost();
            ProcessResult process = RunProcess(host, "packfolder " + QuoteArgument(folder) + " " + QuoteArgument(tempPak), RuntimeLayout.GetEngineDirectory());
            if (process.ExitCode != 0)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = CleanMessage(process.Error, process.Output, "The legacy pack engine did not complete successfully."),
                    FileCount = files.Length
                };
            }

            if (!File.Exists(tempPak))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "The legacy pack engine finished but did not produce a PAK file.",
                    FileCount = files.Length
                };
            }

            string parent = Path.GetDirectoryName(outputPak);
            if (!Directory.Exists(parent))
                Directory.CreateDirectory(parent);
            File.Copy(tempPak, outputPak, true);

            string tempSidecar = tempPak + ".txt";
            string outputSidecar = outputPak + ".txt";
            if (File.Exists(tempSidecar))
                File.Copy(tempSidecar, outputSidecar, true);

            string message = "Packed " + files.Length.ToString() + " files to " + outputPak;
            if (File.Exists(outputSidecar))
                message += " and wrote " + outputSidecar;

            return new OperationResult { Success = true, Message = message, FileCount = files.Length };
        }
        finally
        {
            TryDelete(tempDir);
        }
    }

    public static OperationResult Unpack(string pakPath, string outputFolder)
    {
        OperationResult runtime = CheckRuntime();
        if (!runtime.Success)
            return runtime;

        string tempDir = null;
        string sidecar = FindSidecar(pakPath);
        bool generatedSidecar = false;
        string referenceManifest = null;
        int cleanedGeneratedFiles = 0;
        string extractionOutputFolder = outputFolder;
        if (sidecar == null)
        {
            generatedSidecar = true;
            referenceManifest = ExactNameRecovery.FindReferenceManifest(pakPath);
            if (!String.IsNullOrEmpty(referenceManifest))
            {
                cleanedGeneratedFiles = GeneratedIdCleanup.CleanRootFallbackFiles(outputFolder);
            }

            tempDir = Path.Combine(Path.GetTempPath(), "modern-pak-tool-sidecar-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            if (!String.IsNullOrEmpty(referenceManifest))
                extractionOutputFolder = Path.Combine(tempDir, "extract");
            sidecar = Path.Combine(tempDir, Path.GetFileName(pakPath) + ".txt");
            OperationResult synthetic = PakIndexSidecar.WriteSynthetic(pakPath, sidecar);
            if (!synthetic.Success)
            {
                TryDelete(tempDir);
                return synthetic;
            }
        }

        try
        {
            string host = RuntimeLayout.FindEngineHost();
            ProcessResult process = RunProcess(host, "unpackfolder " + QuoteArgument(pakPath) + " " + QuoteArgument(sidecar) + " " + QuoteArgument(extractionOutputFolder), RuntimeLayout.GetEngineDirectory());
            if (process.ExitCode != 0)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = CleanMessage(process.Error, process.Output, "The legacy unpack engine did not complete successfully."),
                    FileCount = 0
                };
            }

            int fileCount = ReadManifestFileCount(sidecar);
            string message = "Unpacked " + fileCount.ToString() + " files to " + outputFolder;
            ExtensionRecoveryResult recovery = null;
            ExactNameRecoveryResult exactRecovery = null;
            if (generatedSidecar)
            {
                if (!String.IsNullOrEmpty(referenceManifest))
                {
                    exactRecovery = ExactNameRecovery.Apply(outputFolder, extractionOutputFolder, referenceManifest);
                    message += ". Recovered original names for " + exactRecovery.NamedFiles.ToString() + " files from reference manifest.";
                    if (exactRecovery.UnknownFiles > 0)
                        message += " Preserved " + exactRecovery.UnknownFiles.ToString() + " unmapped archive IDs under _unknown_by_id.";
                    if (exactRecovery.TypedUnknownFiles > 0)
                        message += " Added inferred extensions to " + exactRecovery.TypedUnknownFiles.ToString() + " of those unmapped files.";
                }
                else
                {
                    recovery = FallbackNameRecovery.Apply(outputFolder, sidecar);
                    if (recovery.RenamedFiles > 0)
                        message += ". Added likely extensions to " + recovery.RenamedFiles.ToString() + " generated-ID files.";
                    else
                        message += ". Original names were unavailable; generated-ID filenames were kept.";
                }
            }

            OperationResult result = new OperationResult
            {
                Success = true,
                Message = message,
                FileCount = fileCount
            };
            if (recovery != null)
            {
                result.RecoverySummary = recovery.Summary;
                result.RecoveryReportPath = recovery.ReportPath;
            }
            if (exactRecovery != null)
            {
                result.RecoverySummary = exactRecovery.Summary;
                if (cleanedGeneratedFiles > 0)
                    result.RecoverySummary += " Removed " + cleanedGeneratedFiles.ToString() + " previous generated-ID fallback files before recovery.";
            }
            return result;
        }
        finally
        {
            if (tempDir != null)
                TryDelete(tempDir);
        }
    }

    private static ProcessResult RunProcess(string exe, string args, string workingDirectory)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = exe;
        psi.Arguments = args;
        psi.WorkingDirectory = workingDirectory;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using (Process process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ProcessResult { ExitCode = process.ExitCode, Output = output, Error = error };
        }
    }

    private static string QuoteArgument(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string FindSidecar(string pakPath)
    {
        string nativeSidecar = pakPath + ".txt";
        if (File.Exists(nativeSidecar))
            return nativeSidecar;

        string oldSidecar = Path.ChangeExtension(pakPath, ".txt");
        if (File.Exists(oldSidecar))
            return oldSidecar;

        return null;
    }

    private static int ReadManifestFileCount(string sidecar)
    {
        try
        {
            using (StreamReader reader = new StreamReader(sidecar, Encoding.Default))
            {
                string first = reader.ReadLine();
                if (first == null || !first.StartsWith("TotalFile:", StringComparison.OrdinalIgnoreCase))
                    return 0;

                int start = "TotalFile:".Length;
                int end = first.IndexOf('\t', start);
                string text = end >= 0 ? first.Substring(start, end - start) : first.Substring(start);
                int value;
                return Int32.TryParse(text, out value) ? value : 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    private static string CleanMessage(string error, string output, string fallback)
    {
        string message = !String.IsNullOrWhiteSpace(error) ? error.Trim() : output.Trim();
        return String.IsNullOrWhiteSpace(message) ? fallback : message;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
        }
    }
}

internal sealed class ProcessResult
{
    public int ExitCode;
    public string Output;
    public string Error;
}

internal sealed class ExtensionRecoveryResult
{
    public int ScannedFiles;
    public int RenamedFiles;
    public int UnknownFiles;
    public int SkippedFiles;
    public string Summary;
    public string ReportPath;
}

internal sealed class ExactNameRecoveryResult
{
    public int ManifestEntries;
    public int NamedFiles;
    public int UnknownFiles;
    public int TypedUnknownFiles;
    public int MissingFiles;
    public int SkippedFiles;
    public string Summary;
}

internal sealed class ExactNameManifestStats
{
    public int TotalFiles;
    public int NamedFiles;
    public int UnmappedFiles;
}

internal sealed class ExactNameEntry
{
    public int Index;
    public string IdHex;
    public string InternalPath;
    public string TargetRelativePath;
    public string Action;
    public string Reason;
}

internal sealed class FileTypeGuess
{
    public string Extension;
    public string Reason;
    public int Confidence;
}

internal sealed class RecoveryEntry
{
    public string OriginalPath;
    public string FinalPath;
    public string Extension;
    public string Reason;
    public string Action;
    public int Confidence;
}

internal static class GeneratedIdNames
{
    public static bool IsGeneratedIdFileName(string fileName)
    {
        if (String.IsNullOrEmpty(fileName))
            return false;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (stem.StartsWith("_ID_", StringComparison.OrdinalIgnoreCase))
            return HasHexSuffix(stem, 4);
        if (stem.StartsWith("_-ID-_", StringComparison.OrdinalIgnoreCase))
            return HasHexSuffix(stem, 6);
        return false;
    }

    private static bool HasHexSuffix(string value, int start)
    {
        if (value.Length != start + 8)
            return false;

        for (int i = start; i < value.Length; i++)
        {
            char c = value[i];
            bool hex = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!hex)
                return false;
        }

        return true;
    }
}

internal static class GeneratedIdCleanup
{
    public static int CleanRootFallbackFiles(string outputFolder)
    {
        if (String.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
            return 0;

        int removed = 0;
        try
        {
            string report = Path.Combine(outputFolder, "_pak_tool_name_recovery_report.txt");
            if (File.Exists(report))
            {
                File.Delete(report);
                removed++;
            }

            string unknownFolder = Path.Combine(outputFolder, "_unknown_by_id");
            if (Directory.Exists(unknownFolder))
            {
                Directory.Delete(unknownFolder, true);
                removed++;
            }

            foreach (string path in Directory.EnumerateFiles(outputFolder, "*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(path);
                if (!GeneratedIdNames.IsGeneratedIdFileName(fileName))
                    continue;

                File.Delete(path);
                removed++;
            }
        }
        catch
        {
        }

        return removed;
    }
}

internal static class ExactNameRecovery
{
    public static ExactNameManifestStats InspectManifest(string manifestPath)
    {
        List<ExactNameEntry> entries = ReadManifest(manifestPath);
        ExactNameManifestStats stats = new ExactNameManifestStats();
        stats.TotalFiles = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            if (String.IsNullOrEmpty(entries[i].InternalPath))
                stats.UnmappedFiles++;
            else
                stats.NamedFiles++;
        }
        return stats;
    }

    public static string FindReferenceManifest(string pakPath)
    {
        List<string> candidates = GetReferenceManifestCandidates(pakPath);
        for (int i = 0; i < candidates.Count; i++)
        {
            if (File.Exists(candidates[i]))
                return candidates[i];
        }

        return null;
    }

    public static List<string> GetReferenceManifestCandidates(string pakPath)
    {
        List<string> candidates = new List<string>();
        string pakStem = Path.GetFileNameWithoutExtension(pakPath);
        if (String.IsNullOrEmpty(pakStem))
            return candidates;

        List<string> ancestors = GetAncestors(Path.GetDirectoryName(Path.GetFullPath(pakPath)));
        for (int i = 0; i < ancestors.Count; i++)
        {
            string ancestor = ancestors[i];
            AddCandidate(candidates, Path.Combine(ancestor, "_unpacked_pak", pakStem, "_manifest.tsv"));

            try
            {
                int scanned = 0;
                foreach (string child in Directory.EnumerateDirectories(ancestor))
                {
                    AddCandidate(candidates, Path.Combine(child, "_unpacked_pak", pakStem, "_manifest.tsv"));
                    scanned++;
                    if (scanned >= 200)
                        break;
                }
            }
            catch
            {
            }
        }

        return candidates;
    }

    public static ExactNameRecoveryResult Apply(string outputFolder, string generatedFolder, string manifestPath)
    {
        ExactNameRecoveryResult result = new ExactNameRecoveryResult();
        List<ExactNameEntry> entries = ReadManifest(manifestPath);
        result.ManifestEntries = entries.Count;

        for (int i = 0; i < entries.Count; i++)
        {
            ExactNameEntry entry = entries[i];
            string source = FindSourceFile(generatedFolder, entry.IdHex);
            if (String.IsNullOrEmpty(source))
            {
                result.MissingFiles++;
                entry.Action = "missing source";
                entry.Reason = "generated-ID file was not found after extraction";
                continue;
            }

            string target;
            bool targetIsNamed = !String.IsNullOrEmpty(entry.InternalPath);
            bool targetHasInferredExtension = false;
            if (!String.IsNullOrEmpty(entry.InternalPath))
            {
                target = SafeCombine(outputFolder, entry.InternalPath);
                entry.TargetRelativePath = MakeRelative(outputFolder, target);
            }
            else
            {
                FileTypeGuess typeGuess = FallbackNameRecovery.Guess(source);
                string extension = String.IsNullOrEmpty(typeGuess.Extension) ? ".bin" : typeGuess.Extension;
                targetHasInferredExtension = !String.Equals(extension, ".bin", StringComparison.OrdinalIgnoreCase);
                string unknownName = entry.IdHex.ToUpperInvariant() + "_" + entry.Index.ToString("00000") + extension;
                target = SafeCombine(outputFolder, Path.Combine("_unknown_by_id", unknownName));
                entry.TargetRelativePath = MakeRelative(outputFolder, target);
            }

            if (String.IsNullOrEmpty(target))
            {
                result.SkippedFiles++;
                entry.Action = "skipped unsafe path";
                entry.Reason = "target path escaped output folder";
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (File.Exists(target))
                    File.Delete(target);
                File.Move(source, target);
                if (targetIsNamed)
                {
                    result.NamedFiles++;
                    entry.Action = "recovered";
                }
                else
                {
                    result.UnknownFiles++;
                    if (targetHasInferredExtension)
                        result.TypedUnknownFiles++;
                    entry.Action = "preserved unmapped";
                }
            }
            catch (Exception ex)
            {
                result.SkippedFiles++;
                entry.Action = "skipped " + ex.GetType().Name;
                entry.Reason = ex.Message;
            }
        }

        result.Summary = BuildSummary(result, manifestPath);
        return result;
    }

    private static List<string> GetAncestors(string start)
    {
        List<string> ancestors = new List<string>();
        try
        {
            DirectoryInfo current = new DirectoryInfo(start);
            int depth = 0;
            while (current != null && depth < 8)
            {
                ancestors.Add(current.FullName);
                current = current.Parent;
                depth++;
            }
        }
        catch
        {
        }

        return ancestors;
    }

    private static void AddCandidate(List<string> candidates, string path)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (String.Equals(candidates[i], path, StringComparison.OrdinalIgnoreCase))
                return;
        }
        candidates.Add(path);
    }

    private static List<ExactNameEntry> ReadManifest(string manifestPath)
    {
        List<ExactNameEntry> entries = new List<ExactNameEntry>();
        try
        {
            string pakStem = Path.GetFileName(Path.GetDirectoryName(manifestPath));
            using (StreamReader reader = new StreamReader(manifestPath, Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                if (header == null)
                    return entries;

                string[] columns = header.Split('\t');
                int indexColumn = FindColumn(columns, "index");
                int idColumn = FindColumn(columns, "id");
                int pathColumn = FindColumn(columns, "internal_path");
                if (indexColumn < 0 || idColumn < 0 || pathColumn < 0)
                    return entries;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length <= Math.Max(pathColumn, Math.Max(indexColumn, idColumn)))
                        continue;

                    int index;
                    if (!Int32.TryParse(parts[indexColumn], out index))
                        continue;

                    string idHex = NormalizeId(parts[idColumn]);
                    if (idHex.Length != 8)
                        continue;

                    string internalPath = parts[pathColumn].Trim();
                    if (internalPath.Length == 0)
                        internalPath = ExactNameOverrides.Find(pakStem, idHex);
                    entries.Add(new ExactNameEntry
                    {
                        Index = index,
                        IdHex = idHex,
                        InternalPath = internalPath,
                        TargetRelativePath = internalPath.TrimStart('\\', '/')
                    });
                }
            }
        }
        catch
        {
        }

        return entries;
    }

    private static int FindColumn(string[] columns, string name)
    {
        for (int i = 0; i < columns.Length; i++)
        {
            if (String.Equals(columns[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string NormalizeId(string value)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = value.Substring(2);
        return value.PadLeft(8, '0').ToLowerInvariant();
    }

    private static string FindSourceFile(string outputFolder, string idHex)
    {
        string lower = idHex.ToLowerInvariant();
        string upper = idHex.ToUpperInvariant();
        string[] candidates = new string[]
        {
            Path.Combine(outputFolder, "_ID_" + lower),
            Path.Combine(outputFolder, "_ID_" + upper),
            Path.Combine(outputFolder, "_-ID-_" + lower),
            Path.Combine(outputFolder, "_-ID-_" + upper)
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
                return candidates[i];
        }

        return null;
    }

    private static string SafeCombine(string outputFolder, string relativePath)
    {
        try
        {
            string root = AppendSlash(Path.GetFullPath(outputFolder));
            string cleaned = relativePath.TrimStart('\\', '/').Replace('/', '\\');
            if (cleaned.Length == 0 || Path.IsPathRooted(cleaned) || cleaned.IndexOf("..\\", StringComparison.Ordinal) >= 0)
                return null;

            string full = Path.GetFullPath(Path.Combine(root, cleaned));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;
            return full;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSummary(ExactNameRecoveryResult result, string manifestPath)
    {
        string summary = "Reference manifest: " + manifestPath +
            ". Recovered original paths for " + result.NamedFiles.ToString() +
            " files; preserved " + result.UnknownFiles.ToString() + " unmapped archive IDs";
        if (result.TypedUnknownFiles > 0)
            summary += " with inferred extensions on " + result.TypedUnknownFiles.ToString();
        if (result.MissingFiles > 0)
            summary += "; missing " + result.MissingFiles.ToString();
        if (result.SkippedFiles > 0)
            summary += "; skipped " + result.SkippedFiles.ToString();
        return summary + ".";
    }

    private static string MakeRelative(string root, string path)
    {
        try
        {
            Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', '\\');
        }
        catch
        {
            return Path.GetFileName(path);
        }
    }

    private static string AppendSlash(string path)
    {
        if (path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal))
            return path;
        return path + Path.DirectorySeparatorChar;
    }
}

internal static class ExactNameOverrides
{
    public static string Find(string pakStem, string idHex)
    {
        if (String.Equals(pakStem, "settings_c", StringComparison.OrdinalIgnoreCase) &&
            String.Equals(idHex, "35962254", StringComparison.OrdinalIgnoreCase))
            return "\\settings\\shop\\contract.txt";

        return "";
    }
}

internal static class FallbackNameRecovery
{
    private const int SampleBytes = 8192;

    public static ExtensionRecoveryResult Apply(string outputFolder, string sidecarPath)
    {
        ExtensionRecoveryResult result = new ExtensionRecoveryResult();
        List<RecoveryEntry> entries = new List<RecoveryEntry>();
        Dictionary<string, int> extensionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        List<string> files = ReadManifestGeneratedFiles(outputFolder, sidecarPath);
        if (files.Count == 0 || !HasExistingFile(files))
            files = FindGeneratedFiles(outputFolder);

        for (int i = 0; i < files.Count; i++)
        {
            string path = files[i];
            if (!File.Exists(path))
                continue;

            string fileName = Path.GetFileName(path);
            if (!IsGeneratedIdFileName(fileName) || Path.HasExtension(fileName))
                continue;

            result.ScannedFiles++;
            FileTypeGuess guess = Guess(path);
            RecoveryEntry entry = new RecoveryEntry();
            entry.OriginalPath = MakeRelative(outputFolder, path);
            entry.FinalPath = entry.OriginalPath;
            entry.Extension = guess.Extension;
            entry.Reason = guess.Reason;
            entry.Confidence = guess.Confidence;

            if (String.IsNullOrEmpty(guess.Extension))
            {
                result.UnknownFiles++;
                entry.Action = "kept";
                entries.Add(entry);
                continue;
            }

            string target = path + guess.Extension;
            if (File.Exists(target) || Directory.Exists(target))
            {
                result.SkippedFiles++;
                entry.Action = "skipped target exists";
                entries.Add(entry);
                continue;
            }

            try
            {
                File.Move(path, target);
                result.RenamedFiles++;
                entry.FinalPath = MakeRelative(outputFolder, target);
                entry.Action = "renamed";
                Increment(extensionCounts, guess.Extension);
            }
            catch (Exception ex)
            {
                result.SkippedFiles++;
                entry.Action = "skipped " + ex.GetType().Name;
            }

            entries.Add(entry);
        }

        result.ReportPath = WriteReport(outputFolder, result, extensionCounts, entries);
        result.Summary = BuildSummary(result, extensionCounts);
        return result;
    }

    private static List<string> ReadManifestGeneratedFiles(string outputFolder, string sidecarPath)
    {
        List<string> files = new List<string>();
        try
        {
            using (StreamReader reader = new StreamReader(sidecarPath, Encoding.Default))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("TotalFile:", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("Index\t", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length < 4)
                        continue;

                    string relative = parts[3].Trim();
                    relative = relative.TrimStart('\\', '/');
                    if (relative.Length == 0)
                        continue;

                    string fullPath = Path.Combine(outputFolder, relative.Replace('/', '\\'));
                    string fileName = Path.GetFileName(fullPath);
                    if (IsGeneratedIdFileName(fileName))
                        files.Add(fullPath);
                }
            }
        }
        catch
        {
        }

        return files;
    }

    private static bool HasExistingFile(List<string> files)
    {
        for (int i = 0; i < files.Count; i++)
        {
            if (File.Exists(files[i]))
                return true;
        }
        return false;
    }

    private static List<string> FindGeneratedFiles(string outputFolder)
    {
        List<string> files = new List<string>();
        try
        {
            foreach (string path in Directory.EnumerateFiles(outputFolder, "*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(path);
                if (IsGeneratedIdFileName(fileName) && !Path.HasExtension(fileName))
                    files.Add(path);
            }
        }
        catch
        {
        }

        return files;
    }

    private static bool IsGeneratedIdFileName(string fileName)
    {
        if (String.IsNullOrEmpty(fileName))
            return false;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (stem.StartsWith("_ID_", StringComparison.OrdinalIgnoreCase))
            return HasHexSuffix(stem, 4);
        if (stem.StartsWith("_-ID-_", StringComparison.OrdinalIgnoreCase))
            return HasHexSuffix(stem, 6);
        return false;
    }

    private static bool HasHexSuffix(string value, int start)
    {
        if (value.Length != start + 8)
            return false;

        for (int i = start; i < value.Length; i++)
        {
            char c = value[i];
            bool hex = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!hex)
                return false;
        }

        return true;
    }

    internal static FileTypeGuess Guess(string path)
    {
        byte[] buffer = new byte[SampleBytes];
        int read = 0;
        try
        {
            using (FileStream stream = File.OpenRead(path))
                read = stream.Read(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            return Unknown("read failed: " + ex.GetType().Name);
        }

        if (read == 0)
            return Unknown("empty file");

        FileTypeGuess signature = GuessBySignature(buffer, read);
        if (!String.IsNullOrEmpty(signature.Extension))
            return signature;

        if (!LooksText(buffer, read))
            return Unknown("unknown binary signature");

        string text = Encoding.Default.GetString(buffer, 0, read);
        string trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

        if (LooksXml(trimmed))
            return Known(".xml", 95, "XML text signature");
        if (LooksLua(text, trimmed))
            return Known(".lua", 90, "Lua script tokens");
        FileTypeGuess config = GuessConfigText(text);
        if (!String.IsNullOrEmpty(config.Extension))
            return config;
        if (text.IndexOf('\t') >= 0)
            return Known(".txt", 80, "tab-delimited text");

        return Known(".txt", 70, "plain text");
    }

    private static FileTypeGuess GuessBySignature(byte[] buffer, int read)
    {
        if (Starts(buffer, read, 0x53, 0x50, 0x52, 0x00))
            return Known(".spr", 98, "SPR resource signature");
        if (Starts(buffer, read, 0x41, 0x53, 0x46, 0x00))
            return Known(".asf", 98, "ASF resource signature");
        if (Starts(buffer, read, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            return Known(".png", 99, "PNG signature");
        if (Starts(buffer, read, 0xFF, 0xD8, 0xFF))
            return Known(".jpg", 99, "JPEG signature");
        if (StartsAscii(buffer, read, "GIF87a") || StartsAscii(buffer, read, "GIF89a"))
            return Known(".gif", 99, "GIF signature");
        if (StartsAscii(buffer, read, "BM"))
            return Known(".bmp", 95, "BMP signature");
        if (StartsAscii(buffer, read, "DDS "))
            return Known(".dds", 95, "DDS signature");
        if (StartsAscii(buffer, read, "OggS"))
            return Known(".ogg", 95, "Ogg signature");
        if (StartsAscii(buffer, read, "RIFF") && read >= 12 &&
            buffer[8] == (byte)'W' && buffer[9] == (byte)'A' && buffer[10] == (byte)'V' && buffer[11] == (byte)'E')
            return Known(".wav", 99, "WAVE signature");
        if (StartsAscii(buffer, read, "ID3") || LooksMp3Frame(buffer, read))
            return Known(".mp3", 90, "MP3 signature");
        if (Starts(buffer, read, 0x50, 0x4B, 0x03, 0x04))
            return Known(".zip", 90, "ZIP signature");

        return Unknown("no known signature");
    }

    private static bool Starts(byte[] buffer, int read, params byte[] signature)
    {
        if (read < signature.Length)
            return false;
        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
                return false;
        }
        return true;
    }

    private static bool StartsAscii(byte[] buffer, int read, string signature)
    {
        if (read < signature.Length)
            return false;
        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != (byte)signature[i])
                return false;
        }
        return true;
    }

    private static bool LooksMp3Frame(byte[] buffer, int read)
    {
        if (read < 2 || buffer[0] != 0xFF)
            return false;
        return (buffer[1] & 0xE0) == 0xE0;
    }

    private static bool LooksText(byte[] buffer, int read)
    {
        int controls = 0;
        int zeros = 0;
        for (int i = 0; i < read; i++)
        {
            byte value = buffer[i];
            if (value == 0)
                zeros++;
            if (value < 32 && value != 9 && value != 10 && value != 13 && value != 26)
                controls++;
        }

        if (zeros > 0)
            return false;
        return controls <= Math.Max(2, read / 20);
    }

    private static bool LooksXml(string trimmed)
    {
        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            return true;
        return trimmed.StartsWith("<", StringComparison.Ordinal) &&
            trimmed.IndexOf(">", StringComparison.Ordinal) > 0 &&
            trimmed.IndexOf("</", StringComparison.Ordinal) > 0;
    }

    private static bool LooksLua(string text, string trimmed)
    {
        int score = 0;
        if (trimmed.StartsWith("--", StringComparison.Ordinal))
            score += 2;
        if (IndexOfIgnoreCase(text, "Include(") >= 0 && IndexOfIgnoreCase(text, "\\script\\") >= 0)
            score += 4;
        if (IndexOfIgnoreCase(text, "function ") >= 0)
            score += 2;
        if (IndexOfIgnoreCase(text, "local ") >= 0)
            score += 1;
        if (IndexOfIgnoreCase(text, "return ") >= 0)
            score += 1;
        if (IndexOfIgnoreCase(text, "end") >= 0)
            score += 1;

        return score >= 3;
    }

    private static FileTypeGuess GuessConfigText(string text)
    {
        int sections = 0;
        int assignments = 0;
        int meaningful = 0;
        int tabbed = 0;
        using (StringReader reader = new StringReader(text))
        {
            string line;
            int lines = 0;
            while ((line = reader.ReadLine()) != null && lines < 80)
            {
                lines++;
                string value = line.Trim();
                if (value.Length == 0 || value.StartsWith(";", StringComparison.Ordinal) || value.StartsWith("#", StringComparison.Ordinal))
                    continue;

                meaningful++;
                if (value.IndexOf('\t') >= 0)
                    tabbed++;
                if (value.StartsWith("[", StringComparison.Ordinal) && value.IndexOf(']') > 1)
                {
                    sections++;
                    continue;
                }
                if (LooksAssignmentLine(value))
                    assignments++;
            }
        }

        if (sections > 0 && assignments > 0)
            return Known(".ini", 88, "INI section/key pattern");

        if (assignments >= 3 && meaningful > 0 && tabbed == 0)
            return Known(".ini", 82, "key/value configuration pattern");

        return Unknown("no config text pattern");
    }

    private static bool LooksAssignmentLine(string value)
    {
        int equals = value.IndexOf('=');
        if (equals <= 0)
            return false;
        if (value.IndexOf('\t') >= 0)
            return false;

        string key = value.Substring(0, equals).Trim();
        if (key.Length == 0 || key.Length > 80)
            return false;

        for (int i = 0; i < key.Length; i++)
        {
            char c = key[i];
            bool ok = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_' ||
                c == '-' ||
                c == '$' ||
                c == '.';
            if (!ok)
                return false;
        }

        return true;
    }

    private static int IndexOfIgnoreCase(string text, string value)
    {
        return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
    }

    private static FileTypeGuess Known(string extension, int confidence, string reason)
    {
        return new FileTypeGuess { Extension = extension, Confidence = confidence, Reason = reason };
    }

    private static FileTypeGuess Unknown(string reason)
    {
        return new FileTypeGuess { Extension = "", Confidence = 0, Reason = reason };
    }

    private static void Increment(Dictionary<string, int> counts, string extension)
    {
        int current;
        counts.TryGetValue(extension, out current);
        counts[extension] = current + 1;
    }

    private static string BuildSummary(ExtensionRecoveryResult result, Dictionary<string, int> extensionCounts)
    {
        string summary = "Scanned " + result.ScannedFiles.ToString() +
            " generated-ID files; applied likely extensions to " + result.RenamedFiles.ToString() +
            "; kept " + result.UnknownFiles.ToString() + " unknown";
        if (result.SkippedFiles > 0)
            summary += "; skipped " + result.SkippedFiles.ToString();

        string counts = FormatExtensionCounts(extensionCounts);
        if (counts.Length > 0)
            summary += ". Types: " + counts;

        return summary + ".";
    }

    private static string FormatExtensionCounts(Dictionary<string, int> extensionCounts)
    {
        if (extensionCounts.Count == 0)
            return "";

        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, int> pair in extensionCounts)
            parts.Add(pair.Key + " " + pair.Value.ToString());
        parts.Sort(StringComparer.OrdinalIgnoreCase);
        return String.Join(", ", parts.ToArray());
    }

    private static string WriteReport(string outputFolder, ExtensionRecoveryResult result, Dictionary<string, int> extensionCounts, List<RecoveryEntry> entries)
    {
        string reportPath = Path.Combine(outputFolder, "_pak_tool_name_recovery_report.txt");
        try
        {
            using (StreamWriter writer = new StreamWriter(reportPath, false, Encoding.UTF8))
            {
                writer.WriteLine("JX2 PAK fallback name recovery report");
                writer.WriteLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("Mode: no original TXT manifest was available");
                writer.WriteLine("Important: original paths were not recovered. Extensions were inferred only from high-confidence file signatures or text patterns.");
                writer.WriteLine();
                writer.WriteLine("ScannedFiles: " + result.ScannedFiles.ToString());
                writer.WriteLine("RenamedFiles: " + result.RenamedFiles.ToString());
                writer.WriteLine("UnknownFiles: " + result.UnknownFiles.ToString());
                writer.WriteLine("SkippedFiles: " + result.SkippedFiles.ToString());
                writer.WriteLine("ExtensionCounts: " + FormatExtensionCounts(extensionCounts));
                writer.WriteLine();
                writer.WriteLine("Action\tOriginalPath\tFinalPath\tExtension\tConfidence\tReason");
                for (int i = 0; i < entries.Count; i++)
                {
                    RecoveryEntry entry = entries[i];
                    writer.WriteLine(CleanReportValue(entry.Action) + "\t" +
                        CleanReportValue(entry.OriginalPath) + "\t" +
                        CleanReportValue(entry.FinalPath) + "\t" +
                        CleanReportValue(entry.Extension) + "\t" +
                        entry.Confidence.ToString() + "\t" +
                        CleanReportValue(entry.Reason));
                }
            }

            return reportPath;
        }
        catch
        {
            return "";
        }
    }

    private static string CleanReportValue(string value)
    {
        if (value == null)
            return "";
        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string MakeRelative(string root, string path)
    {
        try
        {
            Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', '\\');
        }
        catch
        {
            return Path.GetFileName(path);
        }
    }

    private static string AppendSlash(string path)
    {
        if (path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal))
            return path;
        return path + Path.DirectorySeparatorChar;
    }
}

internal static class PakIndexSidecar
{
    private const int HeaderSize = 32;
    private const uint PackMagic = 0x4B434150;
    private const uint SizeMask = 0x07FFFFFF;
    private const uint FlagMask = 0xF8000000;

    public static OperationResult WriteSynthetic(string pakPath, string sidecarPath)
    {
        try
        {
            using (FileStream stream = File.OpenRead(pakPath))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (stream.Length < HeaderSize)
                    return Fail("PAK is too small to contain a valid header.");

                uint magic = reader.ReadUInt32();
                if (magic != PackMagic)
                    return Fail("PAK header magic is not PACK.");

                int fileCount = reader.ReadInt32();
                int indexOffset = reader.ReadInt32();
                reader.ReadInt32();
                uint packageCrc = reader.ReadUInt32();
                uint packageTime = reader.ReadUInt32();

                long tableBytes = (long)fileCount * 16L;
                if (fileCount < 0 || indexOffset < HeaderSize || indexOffset + tableBytes > stream.Length)
                    return Fail("PAK index table is not in the expected location.");

                Directory.CreateDirectory(Path.GetDirectoryName(sidecarPath));
                stream.Position = indexOffset;

                using (StreamWriter writer = new StreamWriter(sidecarPath, false, Encoding.Default))
                {
                    DateTime pakTime = FromUnixTime(packageTime);
                    writer.WriteLine("TotalFile:" + fileCount.ToString() +
                        "\tPakTime:" + FormatTime(pakTime) +
                        "\tPakTimeSave:" + packageTime.ToString("x") +
                        "\tCRC:" + packageCrc.ToString("x"));
                    writer.WriteLine("Index\tID\tTime\tFileName\tSize\tInPakSize\tComprFlag\tCRC");

                    for (int i = 0; i < fileCount; i++)
                    {
                        uint id = reader.ReadUInt32();
                        uint offset = reader.ReadUInt32();
                        uint size = reader.ReadUInt32();
                        uint packedAndFlags = reader.ReadUInt32();
                        uint packedSize = packedAndFlags & SizeMask;
                        uint flags = packedAndFlags & FlagMask;

                        if (offset < HeaderSize || offset >= stream.Length || packedSize > stream.Length - offset)
                            return Fail("PAK index entry " + i.ToString() + " points outside the archive.");

                        writer.WriteLine(i.ToString() + "\t" +
                            id.ToString("x") + "\t" +
                            FormatTime(pakTime) + "\t" +
                            "\\_ID_" + id.ToString("x8") + "\t" +
                            size.ToString() + "\t" +
                            packedSize.ToString() + "\t" +
                            flags.ToString("x") + "\t0");
                    }
                }

                return new OperationResult { Success = true, Message = "Generated synthetic TXT manifest.", FileCount = fileCount };
            }
        }
        catch (Exception ex)
        {
            return Fail(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static OperationResult Fail(string message)
    {
        return new OperationResult { Success = false, Message = message, FileCount = 0 };
    }

    private static DateTime FromUnixTime(uint seconds)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
        try
        {
            return epoch.AddSeconds(seconds);
        }
        catch
        {
            return DateTime.Now;
        }
    }

    private static string FormatTime(DateTime time)
    {
        return time.Year.ToString() + "-" + time.Month.ToString() + "-" + time.Day.ToString() + " " +
            time.Hour.ToString() + ":" + time.Minute.ToString() + ":" + time.Second.ToString();
    }
}
