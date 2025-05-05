using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataControll;
using DataModels;

namespace BoardGames_Semestralka;

public partial class MainWindow : Window
{
    List<Game> games = new List<Game>();

    private RadioButton? lastCheckedAgeButton = null;
    private RadioButton? lastCheckedPlayTimeButton = null;

    private List<Game> favoriteGames = new List<Game>();
    private HashSet<int> favoriteGameIds = new();

    private int selectedAge = -1;

    public MainWindow()
    {
        InitializeComponent();
        LoadGenres();
        games = DBController.GetGames();
        DisplayGames(games, gamePanel);
    }

    private void LoadGenres()
    {
        var genres = DBController.GetGenres();
        GenresList.ItemsSource = genres;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyAllFilters();
    }

    private void GenresList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GenresList.SelectedItems.Count > 5)
        {
            foreach (var item in e.AddedItems)
            {
                GenresList.SelectedItems.Remove(item);
                break;
            }

            MessageBox.Show("Lze vybrat maximálně 5 žánrů.");
        }

        ApplyAllFilters();
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyAllFilters();

    private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PriceToTextBox != null)
            PriceToTextBox.Text = ((int)PriceSlider.Value).ToString();

        ApplyAllFilters();
    }

    private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ApplyAllFilters();

    private void PlayTime_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            lastCheckedPlayTimeButton = rb;
        }

        ApplyAllFilters();
    }

    private void AgeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int age))
        {
            selectedAge = age;
            lastCheckedAgeButton = rb;
            ApplyAllFilters();
        }
    }

    private void GenresList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var item = ItemsControl.ContainerFromElement(GenresList, e.OriginalSource as DependencyObject) as ListBoxItem;

        if (item != null && item.IsSelected)
        {
            GenresList.SelectedItem = null;
            e.Handled = true;
            ApplyAllFilters();
        }
    }

    private void ApplyAllFilters()
    {
        var filteredGames = new List<Game>(games);

        string query = SearchBox.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(query))
        {
            filteredGames = filteredGames
                .Where(g => g.Name.ToLower().Contains(query))
                .ToList();
        }

        if (GenresList.SelectedItems.Count > 0)
        {
            List<int> selectedGenreIds = GenresList.SelectedItems
                .Cast<string>()
                .Select(g => DBController.GetGenreIdByName(g))
                .Where(id => id != -1)
                .ToList();

            filteredGames = filteredGames
                .Where(game =>
                {
                    List<int> gameGenreIds = DBController.GetGenreIdsByGameId(game.Id);
                    return selectedGenreIds.All(id => gameGenreIds.Contains(id));
                })
                .ToList();
        }

        bool isMinOk = int.TryParse(PriceFromTextBox.Text, out int minPrice);
        bool isMaxOk = int.TryParse(PriceToTextBox.Text, out int maxPrice);

        if (!isMinOk) minPrice = 0;
        if (!isMaxOk) maxPrice = (int)PriceSlider.Maximum;

        filteredGames = filteredGames
            .Where(game => game.Price >= minPrice && game.Price <= maxPrice)
            .ToList();

        int selectedPlayers = (int)PlayerSlider.Value;
        if (selectedPlayers > 0)
        {
            filteredGames = filteredGames
                .Where(game => game.MinPlayers <= selectedPlayers && game.MaxPlayers >= selectedPlayers)
                .ToList();
        }

        List<Func<int, bool>> timeFilters = new();

        if (PlayTime30CheckBox.IsChecked == true) timeFilters.Add(time => time <= 30);
        if (PlayTime60CheckBox.IsChecked == true) timeFilters.Add(time => time <= 60);
        if (PlayTime120CheckBox.IsChecked == true) timeFilters.Add(time => time <= 120);
        if (PlayTimeMoreCheckBox.IsChecked == true) timeFilters.Add(time => time > 120);

        if (timeFilters.Count > 0)
        {
            filteredGames = filteredGames
                .Where(game => timeFilters.Any(f => f(game.PlayTime)))
                .ToList();
        }

        if (selectedAge != -1)
        {
            List<int> ageGroups = new List<int> { 0, 4, 10, 14, 16, 18 };
            int index = ageGroups.IndexOf(selectedAge);

            if (index != -1 && index < ageGroups.Count - 1)
            {
                int nextAge = ageGroups[index + 1];
                filteredGames = filteredGames
                    .Where(game => game.Age >= selectedAge && game.Age < nextAge)
                    .ToList();
            }
            else
            {
                filteredGames = filteredGames
                    .Where(game => game.Age >= selectedAge)
                    .ToList();
            }
        }

        DisplayGames(filteredGames, gamePanel);
    }

    private void AgeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RadioButton clicked = sender as RadioButton;

        if (clicked != null && clicked == lastCheckedAgeButton)
        {
            clicked.IsChecked = false;
            selectedAge = -1;
            lastCheckedAgeButton = null;
            ApplyAllFilters();
            e.Handled = true;
        }
    }

    private void PlayTimeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RadioButton clicked = sender as RadioButton;

        if (clicked != null && clicked == lastCheckedPlayTimeButton)
        {
            clicked.IsChecked = false;
            lastCheckedPlayTimeButton = null;

            ApplyAllFilters();

            e.Handled = true;
        }
    }

    private StackPanel CreateInfoBlock(string emoji, string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 10, 0),
            Children =
        {
            new TextBlock
            {
                Text = emoji,
                FontSize = 12
            },
            new TextBlock
            {
                Text = text,
                FontSize = 12,
                Margin = new Thickness(3, 0, 0, 0)
            }
        }};
    }

    private void DisplayGames(List<Game> gamesToDisplay, WrapPanel targetPanel)
    {
        targetPanel.Children.Clear();

        foreach (var game in gamesToDisplay)
        {
            Border cardBorder = new Border
            {
                Width = 220,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(10)
            };

            StackPanel mainPanel = new StackPanel();

            Image image = new Image
            {
                Height = 185,
                Margin = new Thickness(10),
                Stretch = Stretch.UniformToFill
            };

            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.ImagePath);
                image.Source = new BitmapImage(new Uri(fullPath, UriKind.Absolute));
            }
            catch
            {
                image.Source = new BitmapImage(new Uri("images/Dixit.png", UriKind.Relative));
            }

            WrapPanel infoPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 0, 5, 5)
            };

            if (game.MinPlayers != game.MaxPlayers)
            {
                infoPanel.Children.Add(CreateInfoBlock("👥", $"{game.MinPlayers}–{game.MaxPlayers}"));
            } else
            {
                infoPanel.Children.Add(CreateInfoBlock("👥", $"{game.MinPlayers}"));
            }

            infoPanel.Children.Add(CreateInfoBlock("⏱", $"{game.PlayTime} min"));

            TextBlock ageBlock = new TextBlock
            {
                Text = $"{game.Age}+",
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(5, 0, 0, 5)
            };

            TextBlock title = new TextBlock
            {
                Text = game.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap
            };

            TextBlock priceBlock = new TextBlock
            {
                Text = $"{game.Price},-",
                FontSize = 16,
                Foreground = Brushes.DarkRed,
                Margin = new Thickness(5, 0, 0, 5),
                FontWeight = FontWeights.SemiBold
            };

            string authorName = DBController.GetAuthorNameById(game.AuthorId);

            TextBlock autorBlock = new TextBlock
            {
                Text = $"Autor: {authorName}",
                FontSize = 12,
                Foreground = Brushes.Black,
                Margin = new Thickness(5, 0, 0, 5)
            };

            Button heartButton = new Button
            {
                Content = favoriteGames.Contains(game) ? "❤️" : "🤍",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                FontSize = 16,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            heartButton.MouseEnter += (s, e) => heartButton.Opacity = 0.7;
            heartButton.MouseLeave += (s, e) => heartButton.Opacity = 1.0;

            heartButton.Click += (s, e) =>
            {
                if (favoriteGames.Contains(game))
                    favoriteGames.Remove(game);
                else
                    favoriteGames.Add(game);

                DisplayGames(this.games, gamePanel);
                DisplayGames(favoriteGames, favoritesPanel);
            };

            Button deleteButton = new Button
            {
                Content = "🗑️",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                FontSize = 16,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            deleteButton.Click += (s, e) =>
            {
                var dialog = new ConfirmDialog($"Opravdu chcete odstranit hru '{game.Name}'?");
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    DBController.DeleteGame(game.Id);
                    games = DBController.GetGames();
                    ApplyAllFilters();
                }
            };

            mainPanel.Children.Add(image);
            mainPanel.Children.Add(heartButton);
            mainPanel.Children.Add(deleteButton);
            mainPanel.Children.Add(infoPanel);
            mainPanel.Children.Add(ageBlock);
            mainPanel.Children.Add(autorBlock);
            mainPanel.Children.Add(title);
            mainPanel.Children.Add(priceBlock);

            WrapPanel genrePanel = new WrapPanel
            {
                Margin = new Thickness(5, 0, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var genres = DBController.GetGenreNamesByGameId(game.Id);

            foreach (var genre in genres)
            {
                Border genreTag = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(3, 2, 3, 2),
                    Padding = new Thickness(6, 2, 6, 2),
                    Height = Double.NaN,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock genreText = new TextBlock
                {
                    Text = genre,
                    FontSize = 11,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                };

                genreTag.Child = genreText;
                genrePanel.Children.Add(genreTag);
            }

            mainPanel.Children.Add(genrePanel);
            cardBorder.Child = mainPanel;
            targetPanel.Children.Add(cardBorder);
        }
    }

    private void FavoritesDisplay()
    {
        favoritesPanel.Children.Clear();
        DisplayGames(favoriteGames, favoritesPanel);
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";

        GenresList.SelectedItem = null;

        PriceFromTextBox.Text = "";
        PriceToTextBox.Text = "";
        PriceSlider.Value = PriceSlider.Maximum;

        PlayerSlider.Value = 0;

        lastCheckedPlayTimeButton = null;
        PlayTime30CheckBox.IsChecked = false;
        PlayTime60CheckBox.IsChecked = false;
        PlayTime120CheckBox.IsChecked = false;
        PlayTimeMoreCheckBox.IsChecked = false;

        lastCheckedAgeButton = null;
        selectedAge = -1;

        Age0RadioButton.IsChecked = false;
        Age4RadioButton.IsChecked = false;
        Age10RadioButton.IsChecked = false;
        Age14RadioButton.IsChecked = false;
        Age16RadioButton.IsChecked = false;
        Age18RadioButton.IsChecked = false;

        DisplayGames(games, gamePanel);
        FavoritesDisplay();
    }

    private void OpenAddGameWindow_Click(object sender, RoutedEventArgs e)
    {
        AddGameWindow addWindow = new AddGameWindow();
        addWindow.ShowDialog();

        games = DBController.GetGames();
        ApplyAllFilters();
    }

    private void AddGenre_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddGenreDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            string newGenre = dialog.GenreName;

            if (!string.IsNullOrWhiteSpace(newGenre))
            {
                if (!DBController.GenreExists(newGenre))
                {
                    DBController.AddGenre(newGenre);
                    LoadGenres();

                    var successDialog = new OKDialog($"Žánr '{newGenre}' byl úspěšně přidán.");
                    successDialog.Owner = this;
                    successDialog.ShowDialog();
                }
                else
                {
                    var infoDialog = new OKDialog("Tento žánr již existuje.");
                    infoDialog.Owner = this;
                    infoDialog.ShowDialog();
                }
            }
        }
    }
}