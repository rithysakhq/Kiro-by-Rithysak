using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private const string AppTitle = "Modern PAK Tool by Rithysak";
    private const string LogoResourceName = "ModernPakTool.super_logo.png";
    private const string LogoFileName = "super_logo.png";
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
    private readonly ContentControl _modeContent = new ContentControl();
    private readonly Button _packButton = new Button();
    private readonly Button _unpackButton = new Button();
    private readonly Button _packClearButton = new Button();
    private readonly Button _packOpenButton = new Button();
    private readonly Button _unpackClearButton = new Button();
    private readonly Button _unpackOpenButton = new Button();
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
    private bool _isPackMode = true;
    private bool _isBusy;
    private bool _engineReady = true;
    private string _lastDetails = "";

    public MainWindow()
    {
        Title = AppTitle;
        Icon = LoadLogoImage();
        Width = 820;
        Height = 760;
        MinWidth = 820;
        MinHeight = 760;
        MaxWidth = 820;
        MaxHeight = 760;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ConfigureInteractiveBackground();
        Background = _interactiveBackground;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;
        KeyDown += OnWindowKeyDown;

        Content = BuildUi();
        WireEvents();
        SwitchMode(true);
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
        root.Margin = new Thickness(10);
        root.AllowDrop = true;
        root.PreviewDragOver += OnWindowDragOver;
        root.Drop += OnWindowDrop;
        root.MouseMove += OnGlassMouseMove;
        root.MouseLeave += delegate { UpdateGlassGradient(0.46, 0.28); };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement header = BuildHeader();
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        UIElement statusPanel = BuildStatusPanel();
        ((FrameworkElement)statusPanel).Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(statusPanel, 1);
        root.Children.Add(statusPanel);

        Border workPanel = CreatePanel(10);
        workPanel.Margin = new Thickness(0, 8, 0, 0);
        workPanel.ClipToBounds = true;
        Grid.SetRow(workPanel, 2);
        Grid workGrid = new Grid();
        workGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        workGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workPanel.Child = workGrid;

        workGrid.Children.Add(BuildModeSwitch());

        _packView = BuildPackView();
        _unpackView = BuildUnpackView();
        _modeContent.Margin = new Thickness(0, 8, 0, 0);
        _modeContent.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _modeContent.VerticalContentAlignment = VerticalAlignment.Top;

        Grid.SetRow(_modeContent, 1);
        workGrid.Children.Add(_modeContent);

        ConfigureRecentPanel();
        Grid.SetRow(_recentPanel, 2);
        workGrid.Children.Add(_recentPanel);

        root.Children.Add(workPanel);

        return root;
    }

    private UIElement BuildHeader()
    {
        Grid header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border iconShell = new Border();
        iconShell.Width = 34;
        iconShell.Height = 34;
        iconShell.CornerRadius = new CornerRadius(8);
        iconShell.Background = Rgb(232, 239, 248);
        iconShell.BorderBrush = Rgb(190, 205, 224);
        iconShell.BorderThickness = new Thickness(1);
        iconShell.Child = new Image
        {
            Source = LoadLogoImage(),
            Width = 23,
            Height = 23,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Children.Add(iconShell);

        StackPanel titleStack = new StackPanel();
        titleStack.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(titleStack, 1);

        TextBlock title = new TextBlock();
        title.Text = AppTitle;
        title.FontSize = 20;
        title.FontWeight = FontWeights.SemiBold;
        title.Foreground = Rgb(28, 36, 46);
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        titleStack.Children.Add(title);

        TextBlock subtitle = new TextBlock();
        subtitle.Text = "Safe pack and unpack workflow for JX2 PAK archives.";
        subtitle.FontSize = 12;
        subtitle.Foreground = Rgb(82, 93, 108);
        subtitle.Margin = new Thickness(0, 2, 0, 0);
        subtitle.TextWrapping = TextWrapping.Wrap;
        titleStack.Children.Add(subtitle);

        header.Children.Add(titleStack);
        return header;
    }

    private UIElement BuildModeSwitch()
    {
        Border shell = new Border();
        shell.HorizontalAlignment = HorizontalAlignment.Left;
        shell.Background = Rgb(229, 234, 240);
        shell.BorderBrush = Rgb(204, 212, 222);
        shell.BorderThickness = new Thickness(1);
        shell.CornerRadius = new CornerRadius(8);
        shell.Padding = new Thickness(3);

        StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
        ConfigureButton(_packModeButton, "Pack", false);
        ConfigureButton(_unpackModeButton, "Unpack", false);
        _packModeButton.MinWidth = 88;
        _unpackModeButton.MinWidth = 88;
        _packModeButton.Margin = new Thickness(0, 0, 3, 0);
        _unpackModeButton.Margin = new Thickness(0);
        _packModeButton.Click += delegate { SwitchMode(true); };
        _unpackModeButton.Click += delegate { SwitchMode(false); };
        Track(_packModeButton);
        Track(_unpackModeButton);
        buttons.Children.Add(_packModeButton);
        buttons.Children.Add(_unpackModeButton);
        shell.Child = buttons;
        return shell;
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

        UIElement actions = BuildActionRow(_unpackButton, "Unpack PAK", _unpackClearButton, _unpackOpenButton);
        _unpackButton.Click += async delegate { await UnpackAsync(); };
        _unpackClearButton.Click += delegate { ClearUnpack(); };
        _unpackOpenButton.Click += delegate { OpenUnpackOutput(); };
        Grid.SetRow(actions, 3);
        grid.Children.Add(actions);

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
        row.Margin = new Thickness(0, 0, 0, 7);
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
        labels.Margin = new Thickness(10, 0, 10, 2);

        TextBlock heading = new TextBlock();
        heading.Text = label;
        heading.FontWeight = FontWeights.SemiBold;
        heading.Foreground = Rgb(45, 54, 66);
        labels.Children.Add(heading);

        TextBlock description = new TextBlock();
        description.Text = hint;
        description.Foreground = Rgb(101, 112, 128);
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
        browseButton.Click += browseHandler;
        buttons.Children.Add(browseButton);
        Track(browseButton);

        if (resetButton != null)
        {
            ConfigureButton(resetButton, "Default", false);
            resetButton.Margin = new Thickness(6, 0, 0, 0);
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
        panel.MinHeight = 42;
        panel.Child = grid;

        DockPanel line = new DockPanel();
        _phaseBadge.CornerRadius = new CornerRadius(5);
        _phaseBadge.Padding = new Thickness(8, 2, 8, 3);
        _phaseBadge.Margin = new Thickness(0, 0, 10, 0);
        _phaseText.FontSize = 12;
        _phaseText.FontWeight = FontWeights.SemiBold;
        _phaseBadge.Child = _phaseText;
        DockPanel.SetDock(_phaseBadge, Dock.Left);
        line.Children.Add(_phaseBadge);

        _status.TextWrapping = TextWrapping.Wrap;
        _status.Foreground = Rgb(48, 55, 64);
        _status.VerticalAlignment = VerticalAlignment.Center;
        line.Children.Add(_status);
        Grid.SetRow(line, 0);
        grid.Children.Add(line);

        _progress.Height = 6;
        _progress.Margin = new Thickness(0, 7, 0, 0);
        _progress.Visibility = Visibility.Collapsed;
        Grid.SetRow(_progress, 1);
        grid.Children.Add(_progress);

        _detailsExpander.Header = "Details";
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
        _detailsBox.Background = Rgb(248, 250, 252);
        _detailsBox.BorderBrush = Rgb(207, 215, 224);
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
        panel.BorderBrush = Argb(190, 204, 218, 234);
        panel.BorderThickness = new Thickness(1);
        panel.CornerRadius = new CornerRadius(8);
        panel.Padding = new Thickness(padding);
        panel.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 14,
            ShadowDepth = 1,
            Opacity = 0.12,
            Color = Color.FromRgb(80, 104, 132)
        };
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
        body.Foreground = Rgb(64, 74, 88);
        stack.Children.Add(title);
        stack.Children.Add(body);
        panel.Child = stack;
    }

    private void ConfigureRecentPanel()
    {
        _recentPanel.CornerRadius = new CornerRadius(7);
        _recentPanel.BorderThickness = new Thickness(1);
        _recentPanel.BorderBrush = Rgb(222, 228, 236);
        _recentPanel.Background = Rgb(249, 251, 253);
        _recentPanel.Padding = new Thickness(10);
        _recentPanel.Margin = new Thickness(0, 8, 0, 0);
        _recentPanel.Visibility = Visibility.Collapsed;

        StackPanel stack = new StackPanel();
        TextBlock label = new TextBlock();
        label.Text = "Recent outputs";
        label.FontWeight = FontWeights.SemiBold;
        label.Foreground = Rgb(64, 74, 88);
        label.Margin = new Thickness(0, 0, 0, 4);
        stack.Children.Add(label);
        _recentText.Foreground = Rgb(88, 99, 113);
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
        icon.Margin = new Thickness(0, 14, 0, 0);

        Border shell = new Border();
        shell.CornerRadius = new CornerRadius(8);
        shell.Background = Rgb(237, 242, 248);
        shell.BorderBrush = Rgb(207, 217, 228);
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
            Stream embedded = typeof(MainWindow).Assembly.GetManifestResourceStream(LogoResourceName);
            if (embedded != null)
            {
                using (embedded)
                {
                    BitmapImage embeddedImage = new BitmapImage();
                    embeddedImage.BeginInit();
                    embeddedImage.CacheOption = BitmapCacheOption.OnLoad;
                    embeddedImage.StreamSource = embedded;
                    embeddedImage.EndInit();
                    embeddedImage.Freeze();
                    return embeddedImage;
                }
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogoFileName);
            if (!File.Exists(path))
                return CreateArchiveIcon();

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return CreateArchiveIcon();
        }
    }

    private static void ConfigureTextBox(TextBox box)
    {
        box.Height = 29;
        box.VerticalContentAlignment = VerticalAlignment.Center;
        box.Padding = new Thickness(8, 0, 8, 0);
        box.Background = Rgb(251, 252, 254);
        box.BorderBrush = Rgb(193, 202, 214);
        box.BorderThickness = new Thickness(1);
        box.ToolTip = "Path";
    }

    private static void ConfigureButton(Button button, string text, bool primary)
    {
        button.Content = text;
        button.Height = 29;
        button.MinWidth = primary ? 112 : 80;
        button.Padding = new Thickness(12, 0, 12, 0);
        button.FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal;
        button.Background = primary ? CreatePrimaryGlassBrush() : CreateButtonGlassBrush();
        button.Foreground = primary ? Brushes.White : Rgb(45, 54, 66);
        button.BorderBrush = primary ? Argb(230, 42, 120, 198) : Argb(210, 182, 198, 216);
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
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.48, "border"));
        template.Triggers.Add(disabled);

        return template;
    }

    private void WireEvents()
    {
        _packFolder.TextChanged += delegate { OnPackInputsChanged(); };
        _packOutput.TextChanged += delegate { OnPackInputsChanged(); };
        _unpackFile.TextChanged += delegate { OnUnpackInputsChanged(); };
        _unpackOutput.TextChanged += delegate { OnUnpackInputsChanged(); };
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
                SetStatus("Complete", result.Message, sidecar.Tone == UiTone.Warning ? UiTone.Warning : UiTone.Success);
                string details = "Unpack completed.\r\nSource PAK: " + pak + "\r\nOutput folder: " + output + "\r\nManifest status: " + sidecar.Title + "\r\nFiles unpacked: " + result.FileCount.ToString();
                if (!String.IsNullOrWhiteSpace(result.RecoverySummary))
                    details += "\r\nName recovery: " + result.RecoverySummary;
                if (!String.IsNullOrWhiteSpace(result.RecoveryReportPath))
                    details += "\r\nRecovery report: " + result.RecoveryReportPath;
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
        body.Foreground = Rgb(62, 72, 86);
        title.Text = titleText;
        body.Text = bodyText;
    }

    private void SwitchMode(bool pack)
    {
        _isPackMode = pack;
        _modeContent.Content = pack ? _packView : _unpackView;
        SetSegmentState(_packModeButton, pack);
        SetSegmentState(_unpackModeButton, !pack);
        _packButton.IsDefault = pack;
        _unpackButton.IsDefault = !pack;
        UpdateActionAvailability();
    }

    private void SetSegmentState(Button button, bool selected)
    {
        button.Background = selected ? CreatePrimaryGlassBrush() : CreateButtonGlassBrush();
        button.Foreground = selected ? Brushes.White : Rgb(45, 54, 66);
        button.BorderBrush = selected ? Argb(230, 42, 120, 198) : Argb(210, 182, 198, 216);
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
        _packClearButton.IsEnabled = enabled && (!String.IsNullOrWhiteSpace(_packFolder.Text) || !String.IsNullOrWhiteSpace(_packOutput.Text));
        _unpackClearButton.IsEnabled = enabled && (!String.IsNullOrWhiteSpace(_unpackFile.Text) || !String.IsNullOrWhiteSpace(_unpackOutput.Text));
        _packDefaultButton.IsEnabled = enabled && Directory.Exists(_packFolder.Text.Trim());
        _unpackDefaultButton.IsEnabled = enabled && File.Exists(_unpackFile.Text.Trim());
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
                Message = "No TXT sidecar was found, but a reference unpack manifest is available. Known paths: " + stats.NamedFiles.ToString() + "/" + stats.TotalFiles.ToString() + ". Unmapped IDs: " + stats.UnmappedFiles.ToString() + " preserved under _unknown_by_id.",
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
            SwitchMode(true);
            _packFolder.Text = path;
            _packOutput.Text = DefaultPackOutput(path);
            SetStatus("Ready", "Folder loaded for packing.", UiTone.Info);
            return;
        }

        if (File.Exists(path) && String.Equals(Path.GetExtension(path), ".pak", StringComparison.OrdinalIgnoreCase))
        {
            SwitchMode(false);
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
            if (_isPackMode)
                ClearPack();
            else
                ClearUnpack();
            e.Handled = true;
        }
    }

    private void ConfigureInteractiveBackground()
    {
        _interactiveBackground.StartPoint = new Point(0.04, 0.02);
        _interactiveBackground.EndPoint = new Point(1.0, 1.0);
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(239, 245, 251), 0.0));
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(248, 252, 251), 0.45));
        _interactiveBackground.GradientStops.Add(new GradientStop(Color.FromRgb(235, 241, 248), 1.0));
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
            Color.FromRgb(238, 245, 252),
            Color.FromRgb(231, 248, 243),
            x);
        _interactiveBackground.GradientStops[1].Color = Lerp(
            Color.FromRgb(250, 252, 251),
            Color.FromRgb(247, 249, 255),
            y);
        _interactiveBackground.GradientStops[2].Color = Lerp(
            Color.FromRgb(234, 241, 248),
            Color.FromRgb(245, 240, 232),
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
            background = Rgb(231, 246, 238);
            border = Rgb(160, 211, 185);
            foreground = Rgb(36, 105, 72);
            return;
        }

        if (tone == UiTone.Warning)
        {
            background = Rgb(255, 247, 224);
            border = Rgb(224, 185, 92);
            foreground = Rgb(123, 82, 16);
            return;
        }

        if (tone == UiTone.Error)
        {
            background = Rgb(253, 236, 234);
            border = Rgb(224, 153, 147);
            foreground = Rgb(151, 52, 44);
            return;
        }

        if (tone == UiTone.Busy)
        {
            background = Rgb(232, 240, 250);
            border = Rgb(146, 183, 224);
            foreground = Rgb(35, 88, 145);
            return;
        }

        background = Rgb(238, 243, 249);
        border = Rgb(194, 207, 222);
        foreground = Rgb(51, 65, 82);
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
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(232, 255, 255, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(202, 246, 250, 255), 0.54));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(224, 255, 255, 255), 1.0));
        return brush;
    }

    private static LinearGradientBrush CreateButtonGlassBrush()
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0, 0);
        brush.EndPoint = new Point(0, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(248, 255, 255, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(224, 244, 248, 253), 1.0));
        return brush;
    }

    private static LinearGradientBrush CreatePrimaryGlassBrush()
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.StartPoint = new Point(0, 0);
        brush.EndPoint = new Point(1, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(35, 116, 199), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(28, 96, 175), 0.55));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(45, 139, 185), 1.0));
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
        string pakStem = Path.GetFileNameWithoutExtension(pakPath);
        if (String.IsNullOrEmpty(pakStem))
            return null;

        List<string> candidates = new List<string>();
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

        for (int i = 0; i < candidates.Count; i++)
        {
            if (File.Exists(candidates[i]))
                return candidates[i];
        }

        return null;
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
            if (!String.IsNullOrEmpty(entry.InternalPath))
            {
                target = SafeCombine(outputFolder, entry.InternalPath);
                entry.TargetRelativePath = MakeRelative(outputFolder, target);
            }
            else
            {
                string unknownName = entry.IdHex.ToUpperInvariant() + "_" + entry.Index.ToString("00000") + ".bin";
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

    private static FileTypeGuess Guess(string path)
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
        if (LooksIni(text, trimmed))
            return Known(".ini", 85, "INI section/key pattern");
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

    private static bool LooksIni(string text, string trimmed)
    {
        if (!trimmed.StartsWith("[", StringComparison.Ordinal))
            return false;

        int sections = 0;
        int assignments = 0;
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
                if (value.StartsWith("[", StringComparison.Ordinal) && value.IndexOf(']') > 1)
                    sections++;
                if (value.IndexOf('=') > 0)
                    assignments++;
            }
        }

        return sections > 0 && assignments > 0;
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
