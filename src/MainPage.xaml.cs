using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FluentPdfViewer;

public enum EditMode
{
    Navigate,
    Highlight,
    Note,
    Pen
}

public enum AnnotationType
{
    Highlight,
    Note,
    Pen
}

public sealed partial class MainPage : Page
{
    public static MainPage? Current { get; private set; }

    private PdfDocument? _pdfDocument;
    private ObservableCollection<PdfPageViewModel> _pages = new();
    private int _currentPageIndex = 0;
    private bool _isScrollingProgrammatically = false;
    private string _currentPdfPath = "";
    
    // Annotation states
    private EditMode _currentMode = EditMode.Navigate;
    private bool _isDrawingHighlight = false;
    private Point _startPoint;
    private AnnotationViewModel? _activeHighlight;
    private string _activeColorHex = "#FFFF00"; // Default Yellow

    // Undo/Redo history stack
    private List<(PdfPageViewModel Page, AnnotationViewModel Annotation)> _annotationHistory = new();

    // Recent Files collection
    private ObservableCollection<RecentDocumentViewModel> _recentDocs = new();

    public MainPage()
    {
        Current = this;
        this.InitializeComponent();
        PagesRepeater.ItemsSource = _pages;
        PageListView.ItemsSource = _pages;
        RecentGridView.ItemsSource = _recentDocs;

        // Start at welcome screen
        WelcomePanel.Visibility = Visibility.Visible;
        SidebarSplitView.Visibility = Visibility.Collapsed;

        LoadRecentFiles();
    }

    private async void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        
        // WinUI 3 desktop apps need to associate the picker with the window handle (HWND)
        var app = Application.Current as App;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        
        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            DocTitleText.Text = file.Name;
            await LoadPdfAsync(file);
        }
    }

    private async Task LoadPdfAsync(StorageFile file)
    {
        try
        {
            // Block all ScrollViewer layout and scroll sync events during loading
            _isScrollingProgrammatically = true;

            _pages.Clear();
            _annotationHistory.Clear();
            _currentPageIndex = 0;
            PageNumberInput.Text = "1";
            TotalPagesText.Text = "/ 0";
            _currentPdfPath = file.Path;

            // Hide welcome screen, show document panel
            WelcomePanel.Visibility = Visibility.Collapsed;
            SidebarSplitView.Visibility = Visibility.Visible;

            _pdfDocument = await PdfDocument.LoadFromFileAsync(file);
            
            if (_pdfDocument == null) return;

            TotalPagesText.Text = $"/ {_pdfDocument.PageCount}";

            // Load and render all pages
            for (uint i = 0; i < _pdfDocument.PageCount; i++)
            {
                using PdfPage page = _pdfDocument.GetPage(i);
                
                // Render page to a memory stream
                var stream = new InMemoryRandomAccessStream();
                
                var renderOptions = new PdfPageRenderOptions();
                // Render at 2x resolution for crispness on screen
                renderOptions.DestinationWidth = (uint)(page.Size.Width * 2.0);
                
                await page.RenderToStreamAsync(stream, renderOptions);
                
                // Create bitmap and set its source from the stream
                var bitmap = new BitmapImage();
                stream.Seek(0);
                await bitmap.SetSourceAsync(stream);
                
                _pages.Add(new PdfPageViewModel 
                { 
                    ImageSource = bitmap,
                    PageIndex = (int)i,
                    PageWidth = page.Size.Width,
                    PageHeight = page.Size.Height
                });

                // If it is the first page, copy its stream as a PNG thumbnail for the Welcome Screen
                if (i == 0)
                {
                    try
                    {
                        string safeName = file.Path.Replace(":", "_").Replace("\\", "_").Replace("/", "_");
                        string thumbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, $"thumb_{safeName}.png");
                        
                        using (var fileStream = File.Create(thumbPath))
                        {
                            stream.Seek(0);
                            stream.AsStreamForRead().CopyTo(fileStream);
                        }
                        
                        AddToRecentFiles(file.Path, file.Name, thumbPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save cover thumbnail: {ex.Message}");
                    }
                }
            }

            // Load annotations if they exist for this PDF
            LoadAnnotations(_currentPdfPath);
            
            // Reset zoom to 100% (default Index 2)
            ZoomComboBox.SelectedIndex = 2;
            PdfScrollViewer.ChangeView(null, null, 1.0f);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PDF: {ex.Message}");
            var dialog = new ContentDialog
            {
                Title = "Error al cargar PDF",
                Content = $"Error: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
        finally
        {
            // Re-enable ScrollViewer scroll sync events
            _isScrollingProgrammatically = false;
        }
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        SidebarSplitView.IsPaneOpen = !SidebarSplitView.IsPaneOpen;
    }

    private void PageListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var clickedItem = e.ClickedItem as PdfPageViewModel;
        if (clickedItem != null && clickedItem.PageIndex != _currentPageIndex)
        {
            NavigateToPage(clickedItem.PageIndex);
        }
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfDocument != null && _currentPageIndex > 0)
        {
            NavigateToPage(_currentPageIndex - 1);
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfDocument != null && _currentPageIndex < _pdfDocument.PageCount - 1)
        {
            NavigateToPage(_currentPageIndex + 1);
        }
    }

    private void NavigateToPage(int index)
    {
        if (index >= 0 && _pdfDocument != null && index < _pdfDocument.PageCount)
        {
            _currentPageIndex = index;
            
            // Temporary block ViewChanged scroll sync to avoid recursive selection jumps
            _isScrollingProgrammatically = true;
            
            PageNumberInput.Text = (index + 1).ToString();
            PageListView.SelectedIndex = index;
            
            var element = PagesRepeater.GetOrCreateElement(index) as UIElement;
            if (element != null)
            {
                element.StartBringIntoView(new BringIntoViewOptions 
                { 
                    VerticalAlignmentRatio = 0.0 // Scroll to top of the item
                });
            }
            
            _isScrollingProgrammatically = false;
        }
    }

    private void PageNumberInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            JumpToInputPage();
            // Lose focus from input box
            DocTitleText.Focus(FocusState.Programmatic);
        }
    }

    private void PageNumberInput_LosingFocus(object sender, RoutedEventArgs e)
    {
        JumpToInputPage();
    }

    private void JumpToInputPage()
    {
        if (int.TryParse(PageNumberInput.Text, out int pageNum))
        {
            NavigateToPage(pageNum - 1);
        }
        else
        {
            // Reset to current page
            PageNumberInput.Text = (_currentPageIndex + 1).ToString();
        }
    }

    private void ZoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PdfScrollViewer == null) return;

        float zoomFactor = 1.0f;
        switch (ZoomComboBox.SelectedIndex)
        {
            case 0: zoomFactor = 0.5f; break; // 50%
            case 1: zoomFactor = 0.75f; break; // 75%
            case 2: zoomFactor = 1.0f; break; // 100%
            case 3: zoomFactor = 1.25f; break; // 125%
            case 4: zoomFactor = 1.5f; break; // 150%
            case 5: zoomFactor = 2.0f; break; // 200%
            case 6: // Fit Width
                if (_pdfDocument != null && _pdfDocument.PageCount > 0)
                {
                    using var firstPage = _pdfDocument.GetPage(0);
                    double pageWidth = firstPage.Size.Width;
                    double viewportWidth = PdfScrollViewer.ViewportWidth;
                    // Account for margin spacing (16 * 2 + scrollbars)
                    zoomFactor = (float)((viewportWidth - 48.0) / pageWidth);
                    zoomFactor = Math.Max(0.2f, Math.Min(zoomFactor, 4.0f));
                }
                break;
        }

        PdfScrollViewer.ChangeView(null, null, zoomFactor);
    }

    private void PdfScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isScrollingProgrammatically) return;
        if (_pdfDocument == null || _pdfDocument.PageCount == 0) return;

        double offset = PdfScrollViewer.VerticalOffset;

        if (_pdfDocument.PageCount > 0)
        {
            using var firstPage = _pdfDocument.GetPage(0);
            double zoom = PdfScrollViewer.ZoomFactor;
            // Get rendered height + StackLayout spacing (16) + item borders/margins (16)
            double pageHeight = (firstPage.Size.Height * 2.0) * zoom;
            double itemHeight = pageHeight + 32.0;

            int activePageIndex = (int)Math.Round(offset / itemHeight);
            activePageIndex = Math.Max(0, Math.Min(activePageIndex, (int)_pdfDocument.PageCount - 1));

            if (activePageIndex != _currentPageIndex)
            {
                _currentPageIndex = activePageIndex;
                PageNumberInput.Text = (_currentPageIndex + 1).ToString();
                
                // Highlight item in sidebar
                _isScrollingProgrammatically = true;
                PageListView.SelectedIndex = _currentPageIndex;
                PageListView.ScrollIntoView(PageListView.SelectedItem);
                _isScrollingProgrammatically = false;
            }
        }
    }

    // Annotation Mode Handlers
    private void ReadMode_Click(object sender, RoutedEventArgs e)
    {
        SetEditMode(EditMode.Navigate);
    }

    private void HighlightMode_Click(object sender, RoutedEventArgs e)
    {
        SetEditMode(EditMode.Highlight);
    }

    private void NoteMode_Click(object sender, RoutedEventArgs e)
    {
        SetEditMode(EditMode.Note);
    }

    private void PenMode_Click(object sender, RoutedEventArgs e)
    {
        SetEditMode(EditMode.Pen);
    }

    private void SetEditMode(EditMode mode)
    {
        _currentMode = mode;
        ReadModeButton.IsChecked = (mode == EditMode.Navigate);
        HighlightModeButton.IsChecked = (mode == EditMode.Highlight);
        NoteModeButton.IsChecked = (mode == EditMode.Note);
        PenModeButton.IsChecked = (mode == EditMode.Pen);

        // Toggle visibility of the color selector panel for drawing modes
        bool showColors = (mode == EditMode.Highlight || mode == EditMode.Pen);
        ColorSelectorPanel.Visibility = showColors ? Visibility.Visible : Visibility.Collapsed;
        ColorSeparator.Visibility = showColors ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button != null && button.Tag != null)
        {
            _activeColorHex = button.Tag.ToString() ?? "#FFFF00";
        }
    }

    private void Rotate_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfDocument == null) return;

        foreach (var page in _pages)
        {
            // Swap width and height for layout calculation
            double temp = page.PageWidth;
            page.PageWidth = page.PageHeight;
            page.PageHeight = temp;

            page.RotationAngle = (page.RotationAngle + 90.0) % 360.0;
        }
    }

    // Canvas Pointer Events for Drawing
    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_currentMode == EditMode.Navigate) return;

        var element = sender as FrameworkElement;
        if (element == null) return;
        
        var pageVm = element.DataContext as PdfPageViewModel;
        if (pageVm == null) return;

        var point = e.GetCurrentPoint(element).Position;

        if (_currentMode == EditMode.Highlight)
        {
            _isDrawingHighlight = true;
            _startPoint = point;
            
            _activeHighlight = new AnnotationViewModel
            {
                Type = AnnotationType.Highlight,
                X = point.X,
                Y = point.Y,
                Width = 0,
                Height = 0,
                ColorHex = _activeColorHex
            };
            pageVm.Annotations.Add(_activeHighlight);
            _annotationHistory.Add((pageVm, _activeHighlight));
            
            element.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        else if (_currentMode == EditMode.Note)
        {
            var note = new AnnotationViewModel
            {
                Type = AnnotationType.Note,
                X = point.X,
                Y = point.Y,
                Content = "Escribe tu nota aquí..."
            };
            pageVm.Annotations.Add(note);
            _annotationHistory.Add((pageVm, note));
            SaveAnnotations();
            e.Handled = true;
        }
        else if (_currentMode == EditMode.Pen)
        {
            _isDrawingHighlight = true; // Use drawing flag
            _activeHighlight = new AnnotationViewModel
            {
                Type = AnnotationType.Pen,
                X = 0, // No translation needed for absolute polyline coordinates
                Y = 0,
                ColorHex = _activeColorHex
            };
            _activeHighlight.PointsCollection.Add(point);
            pageVm.Annotations.Add(_activeHighlight);
            _annotationHistory.Add((pageVm, _activeHighlight));

            element.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawingHighlight && _activeHighlight != null)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;

            var currentPoint = e.GetCurrentPoint(element).Position;

            if (_currentMode == EditMode.Highlight)
            {
                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(_startPoint.X - currentPoint.X);
                double h = Math.Abs(_startPoint.Y - currentPoint.Y);

                _activeHighlight.X = x;
                _activeHighlight.Y = y;
                _activeHighlight.Width = w;
                _activeHighlight.Height = h;
            }
            else if (_currentMode == EditMode.Pen)
            {
                _activeHighlight.PointsCollection.Add(currentPoint);
            }
            
            e.Handled = true;
        }
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawingHighlight)
        {
            _isDrawingHighlight = false;
            _activeHighlight = null;
            
            var element = sender as FrameworkElement;
            element?.ReleasePointerCapture(e.Pointer);
            
            SaveAnnotations();
            e.Handled = true;
        }
    }

    // Keyboard Accelerator for Undo (Ctrl+Z)
    private void Undo_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        UndoLastAnnotation();
        args.Handled = true;
    }

    private void UndoLastAnnotation()
    {
        if (_annotationHistory.Count > 0)
        {
            // Remove the last annotation from the history stack and page collection
            var last = _annotationHistory[_annotationHistory.Count - 1];
            _annotationHistory.RemoveAt(_annotationHistory.Count - 1);

            last.Page.Annotations.Remove(last.Annotation);
            SaveAnnotations();
        }
    }

    // Recent Files Management
    private void AddToRecentFiles(string filePath, string fileName, string thumbnailPath)
    {
        // Remove if it already exists to place it back at the top
        for (int i = 0; i < _recentDocs.Count; i++)
        {
            if (_recentDocs[i].FilePath == filePath)
            {
                _recentDocs.RemoveAt(i);
                break;
            }
        }

        var item = new RecentDocumentViewModel
        {
            FilePath = filePath,
            FileName = fileName,
            ThumbnailPath = thumbnailPath
        };

        try
        {
            if (File.Exists(thumbnailPath))
            {
                item.ThumbnailImage = new BitmapImage(new Uri(thumbnailPath));
            }
        }
        catch { }

        _recentDocs.Insert(0, item);

        // Cap list at 8 recent items
        while (_recentDocs.Count > 8)
        {
            _recentDocs.RemoveAt(_recentDocs.Count - 1);
        }

        SaveRecentFilesJson();
        UpdateRecentHeaderVisibility();
    }

    private void RemoveFromRecentFiles(string filePath)
    {
        for (int i = 0; i < _recentDocs.Count; i++)
        {
            if (_recentDocs[i].FilePath == filePath)
            {
                _recentDocs.RemoveAt(i);
                break;
            }
        }
        SaveRecentFilesJson();
        UpdateRecentHeaderVisibility();
    }

    private void SaveRecentFilesJson()
    {
        try
        {
            var savedList = new List<SavedRecentDoc>();
            foreach (var doc in _recentDocs)
            {
                savedList.Add(new SavedRecentDoc
                {
                    FilePath = doc.FilePath,
                    FileName = doc.FileName,
                    ThumbnailPath = doc.ThumbnailPath
                });
            }
            string json = System.Text.Json.JsonSerializer.Serialize(savedList);
            string recentPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "recent_files.json");
            File.WriteAllText(recentPath, json);
        }
        catch { }
    }

    private void LoadRecentFiles()
    {
        try
        {
            _recentDocs.Clear();
            string recentPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "recent_files.json");
            if (!File.Exists(recentPath))
            {
                UpdateRecentHeaderVisibility();
                return;
            }

            string json = File.ReadAllText(recentPath);
            var savedList = System.Text.Json.JsonSerializer.Deserialize<List<SavedRecentDoc>>(json);

            if (savedList != null)
            {
                foreach (var saved in savedList)
                {
                    if (File.Exists(saved.FilePath))
                    {
                        var doc = new RecentDocumentViewModel
                        {
                            FilePath = saved.FilePath,
                            FileName = saved.FileName,
                            ThumbnailPath = saved.ThumbnailPath
                        };

                        if (File.Exists(saved.ThumbnailPath))
                        {
                            doc.ThumbnailImage = new BitmapImage(new Uri(saved.ThumbnailPath));
                        }
                        _recentDocs.Add(doc);
                    }
                }
            }
            UpdateRecentHeaderVisibility();
        }
        catch
        {
            UpdateRecentHeaderVisibility();
        }
    }

    private void UpdateRecentHeaderVisibility()
    {
        RecientesHeader.Visibility = _recentDocs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void RecentGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var recent = e.ClickedItem as RecentDocumentViewModel;
        if (recent != null)
        {
            if (File.Exists(recent.FilePath))
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(recent.FilePath);
                    DocTitleText.Text = file.Name;
                    await LoadPdfAsync(file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error opening recent file: {ex.Message}");
                    RemoveFromRecentFiles(recent.FilePath);
                }
            }
            else
            {
                // File no longer exists, clean it up
                RemoveFromRecentFiles(recent.FilePath);
            }
        }
    }

    // Save and Load Annotations (Auto-save)
    public void SaveAnnotations()
    {
        if (string.IsNullOrEmpty(_currentPdfPath) || _pages.Count == 0) return;

        try
        {
            var savedList = new List<SavedAnnotation>();
            foreach (var page in _pages)
            {
                foreach (var anno in page.Annotations)
                {
                    var savedAnno = new SavedAnnotation
                    {
                        PageIndex = page.PageIndex,
                        Type = anno.Type,
                        X = anno.X,
                        Y = anno.Y,
                        Width = anno.Width,
                        Height = anno.Height,
                        Content = anno.Content ?? "",
                        ColorHex = anno.ColorHex
                    };

                    foreach (var pt in anno.PointsCollection)
                    {
                        savedAnno.Points.Add(new SavedPoint { X = pt.X, Y = pt.Y });
                    }

                    savedList.Add(savedAnno);
                }
            }

            string json = System.Text.Json.JsonSerializer.Serialize(savedList);
            string filePath = GetAnnotationFilePath(_currentPdfPath);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving annotations: {ex.Message}");
        }
    }

    private void LoadAnnotations(string pdfPath)
    {
        try
        {
            string filePath = GetAnnotationFilePath(pdfPath);
            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            var savedList = System.Text.Json.JsonSerializer.Deserialize<List<SavedAnnotation>>(json);

            if (savedList == null) return;

            foreach (var saved in savedList)
            {
                if (saved.PageIndex >= 0 && saved.PageIndex < _pages.Count)
                {
                    var annoVm = new AnnotationViewModel
                    {
                        Type = saved.Type,
                        X = saved.X,
                        Y = saved.Y,
                        Width = saved.Width,
                        Height = saved.Height,
                        Content = saved.Content,
                        ColorHex = saved.ColorHex ?? "#FFFF00"
                    };

                    if (saved.Points != null)
                    {
                        foreach (var pt in saved.Points)
                        {
                            annoVm.PointsCollection.Add(new Point(pt.X, pt.Y));
                        }
                    }

                    _pages[saved.PageIndex].Annotations.Add(annoVm);
                    _annotationHistory.Add((_pages[saved.PageIndex], annoVm));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading annotations: {ex.Message}");
        }
    }

    private string GetAnnotationFilePath(string pdfPath)
    {
        string safeName = pdfPath.Replace(":", "_").Replace("\\", "_").Replace("/", "_");
        string localFolder = ApplicationData.Current.LocalFolder.Path;
        return Path.Combine(localFolder, $"{safeName}.json");
    }
}

public class RecentDocumentViewModel
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ThumbnailPath { get; set; } = "";
    public ImageSource? ThumbnailImage { get; set; }
}

public class SavedRecentDoc
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ThumbnailPath { get; set; } = "";
}

public class PdfPageViewModel : INotifyPropertyChanged
{
    private double _pageWidth;
    private double _pageHeight;
    private double _rotationAngle = 0.0;
    private int _pageIndex;
    private ImageSource? _imageSource;

    public ImageSource? ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            if (SetProperty(ref _pageIndex, value))
            {
                OnPropertyChanged(nameof(PageNumberText));
            }
        }
    }
    
    public double PageWidth
    {
        get => _pageWidth;
        set => SetProperty(ref _pageWidth, value);
    }

    public double PageHeight
    {
        get => _pageHeight;
        set => SetProperty(ref _pageHeight, value);
    }

    public double RotationAngle
    {
        get => _rotationAngle;
        set => SetProperty(ref _rotationAngle, value);
    }

    public string PageNumberText => $"Página {PageIndex + 1}";
    public ObservableCollection<AnnotationViewModel> Annotations { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SavedPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class SavedAnnotation
{
    public int PageIndex { get; set; }
    public AnnotationType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Content { get; set; } = "";
    public string ColorHex { get; set; } = "#FFFF00";
    public List<SavedPoint> Points { get; set; } = new();
}

public class AnnotationViewModel : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private string _content = "";
    private string _colorHex = "#FFFF00";
    private PointCollection _pointsCollection = new();

    public AnnotationType Type { get; set; }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                MainPage.Current?.SaveAnnotations();
            }
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (SetProperty(ref _colorHex, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBrush)));
                MainPage.Current?.SaveAnnotations();
            }
        }
    }

    public PointCollection PointsCollection
    {
        get => _pointsCollection;
        set => SetProperty(ref _pointsCollection, value);
    }

    public Brush ColorBrush
    {
        get
        {
            try
            {
                string hex = _colorHex.Replace("#", "");
                if (Type == AnnotationType.Highlight)
                {
                    // Semi-transparent alpha (50 hex = 80 dec / 255 = 31% opacity)
                    if (hex.Length == 6)
                        hex = "50" + hex;
                    else if (hex.Length == 8)
                        hex = "50" + hex.Substring(2);
                }
                else
                {
                    // Solid alpha for pen drawing
                    if (hex.Length == 6)
                        hex = "FF" + hex;
                }

                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);

                return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            }
            catch
            {
                return new SolidColorBrush(Microsoft.UI.Colors.Yellow);
            }
        }
    }

    public Visibility IsHighlightVisibility => Type == AnnotationType.Highlight ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsNoteVisibility => Type == AnnotationType.Note ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsPenVisibility => Type == AnnotationType.Pen ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(Type))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlightVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNoteVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPenVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBrush)));
        }
        return true;
    }
}
