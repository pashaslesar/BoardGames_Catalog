using DataControll;
using DataModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BoardGames_Semestralka;

public partial class AddGameWindow : Window
{
    private ObservableCollection<string> selectedGenres = new();

    public AddGameWindow()
    {
        InitializeComponent();
        LoadGenres();
        SelectedGenresPanel.ItemsSource = selectedGenres;
    }

    private void LoadGenres()
    {
        var genres = DBController.GetGenres();
        GenreComboBox.ItemsSource = genres;
    }

    private void AddGenre_Click(object sender, RoutedEventArgs e)
    {
        string selected = GenreComboBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(selected) && !selectedGenres.Contains(selected))
        {
            selectedGenres.Add(selected);
        }
    }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        string gameName = NameBox.Text.Trim();
        string authorName = AuthorNameBox.Text.Trim();
        string authorCountry = AuthorCountryBox.Text.Trim();

        if (selectedGenres.Count == 0)
        {
            MessageBox.Show("Vyberte alespoň jeden žánr.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(authorName))
        {
            MessageBox.Show("Zadejte název hry a jméno autora.");
            return;
        }

        int authorId = DBController.InsertOrGetAuthorId(authorName, authorCountry);
        int mainGenreId = DBController.GetGenreIdByName(selectedGenres[0]);

        Game newGame = new()
        {
            Name = gameName,
            GenreId = mainGenreId,
            AuthorId = authorId,
            MinPlayers = int.Parse(MinPlayersBox.Text),
            MaxPlayers = int.Parse(MaxPlayersBox.Text),
            PlayTime = int.Parse(PlayTimeBox.Text),
            Price = int.Parse(PriceBox.Text),
            ImagePath = ImagePathBox.Text,
            Age = int.Parse(AgeBox.Text)
        };

        int gameId = DBController.InsertGame(newGame);

        foreach (string genre in selectedGenres)
        {
            int genreId = DBController.GetGenreIdByName(genre);
            DBController.AddGameGenre(gameId, genreId);
        }

        MessageBox.Show("Hra byla úspěšně přidána!");
        this.Close();
    }

    private void RemoveGenre_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string genre)
        {
            selectedGenres.Remove(genre);
        }
    }
}
