namespace DE;
public static class Willekeurig
{
    public static Random Random = new Random();
    public static async Task Pauzeer(int milliSeconden, double willekeurigheid = 0.3)
    {
        await Task.Delay((int)(milliSeconden * (1 + willekeurigheid * (2 * Random.NextDouble() - 1))));
    }
}

public static class Logger
{
    private static StreamWriter? _streamWriter;

    public static async Task Open()
    {
        _streamWriter = new StreamWriter("log.txt");
        await Task.Yield();
    }

    public static async Task Write(string text)
    {
        await Task.Yield();
        if (_streamWriter != null)
        {
            await _streamWriter.WriteLineAsync(text);
        }
        else
        {
            throw new InvalidOperationException("Logger is niet geopend.");
        }
    }
}

public class Attractie
{
    public enum Status
    {
        Werkt,
        Kapot,
        Opstarten
    }

    public string Naam { get; }
    public Status CurrentStatus { get; set; }

    public Attractie(string naam, Status status)
    {
        Naam = naam;
        CurrentStatus = status;
    }

    public async Task<int> AantalWachtenden()
    {
        await Willekeurig.Pauzeer(1000);
        return Willekeurig.Random.Next(0, 31);
    }

    private async Task VerzendHerstartCommando()
    {
        var timeoutTask = Task.Delay(1200);
        var commandTask = Willekeurig.Pauzeer(1000);

        var completedTask = await Task.WhenAny(commandTask, timeoutTask);

        if (completedTask == commandTask)
        {
            // Het commando is succesvol verstuurd.
        }
        else
        {
            // Time-out, gooi een exceptie.
            throw new TimeoutException("Het versturen van het commando duurde te lang.");
        }
    }

    public async Task Herstart()
    {
        CurrentStatus = Status.Opstarten;
        await Logger.Write($"De attractie {Naam} is aan het opstarten.");
        try
        {
            await VerzendHerstartCommando();
            CurrentStatus = Status.Werkt;
            await Logger.Write($"De attractie {Naam} is opgestart.");
        }
        catch (TimeoutException)
        {
            CurrentStatus = Status.Kapot;
        }
    }
}

public class Monteur
{
    public List<Attractie> BeheerdeAttracties { get; } = new List<Attractie>();

    public void Beheer(Attractie attractie)
    {
        lock (BeheerdeAttracties)
        {
            if (BeheerdeAttracties.Count >= 5)
            {
                throw new InvalidOperationException("Een monteur kan niet meer dan vijf attracties beheren.");
            }

            BeheerdeAttracties.Add(attractie);
        }
    }

    public IReadOnlyList<Attractie> InBeheer()
    {
        lock (BeheerdeAttracties)
        {
            return BeheerdeAttracties.AsReadOnly();
        }
    }
}

public class MonteurContext
{
    private static List<Monteur> _monteurs = new List<Monteur>();

    public static int AantalMonteurs => _monteurs.Count;

    public static Monteur GetMonteur(int index)
    {
        if (index >= 0 && index < _monteurs.Count)
        {
            return _monteurs[index];
        }
        else
        {
            throw new IndexOutOfRangeException("Monteur index is buiten bereik.");
        }
    }

    public static void VoegMonteurToe(Monteur monteur)
    {
        _monteurs.Add(monteur);
    }
}

public class AdminPaneel
{
    private static List<Attractie> Attracties { get; } = new List<Attractie>
    {
        new Attractie("Draaimolen", Attractie.Status.Werkt),
        new Attractie("Reuzenrad", Attractie.Status.Werkt),
        new Attractie("Achtbaan", Attractie.Status.Kapot),
        new Attractie("Achtbaan 2", Attractie.Status.Werkt),
        new Attractie("Spin", Attractie.Status.Werkt),
        new Attractie("Schommel", Attractie.Status.Opstarten),
    };


    public static async Task GemiddeldeWachtenden()
    {
        Console.WriteLine("Gemiddeld aantal wachtenden: berekening bezig");
        var tasks = Attracties.Select(a => a.AantalWachtenden()).ToList();
        await Task.WhenAll(tasks);
        var resultaten = tasks.Select(t => t.Result).ToList();
        double gemiddeldAantalWachtenden = resultaten.Average();
        Console.WriteLine($"Gemiddeld aantal wachtenden: {gemiddeldAantalWachtenden:F2}");
    }

    public static async Task Hoofd()
    {
        await Logger.Open();

        Console.WriteLine("Dit is het adminpaneel!");
        while (true)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            Console.WriteLine("[ ] Gemiddeld aantal wachtenden: onbekend");

            for (int i = 0; i < Attracties.Count; i++)
            {
                var attractie = Attracties[i];
                string statusText = attractie.CurrentStatus switch
                {
                    Attractie.Status.Werkt => "(werkt)",
                    Attractie.Status.Kapot => "(kapot)",
                    Attractie.Status.Opstarten => "(opstarten)",
                    _ => throw new InvalidOperationException("Ongeldige attractie status."),
                };

                string isSelected = i == 0 ? "[X]" : "[ ]";

                Console.WriteLine($"{isSelected} ({i + 1}) {attractie.Naam} {statusText}");
            }

            for (int i = 0; i < MonteurContext.AantalMonteurs; i++)
            {
                var monteur = MonteurContext.GetMonteur(i);
                var attractiesInBeheer = monteur.InBeheer().Select(a => a.Naam).ToList();
                Console.WriteLine($"{i + 1}) Monteur {i + 1}: {string.Join(", ", attractiesInBeheer)}");
            }

            var key = Console.ReadKey(true).Key;
            int selectedIndex = 0;

            if (key == ConsoleKey.UpArrow)
            {
                selectedIndex = (selectedIndex - 1 + Attracties.Count) % Attracties.Count;
            }
            else if (key == ConsoleKey.DownArrow)
            {
                selectedIndex = (selectedIndex + 1) % Attracties.Count;
            }
            else if (key == ConsoleKey.Enter)
            {
                if (selectedIndex == 0)
                {
                    await GemiddeldeWachtenden();
                }
                else
                {
                    var selectedAttractie = Attracties[selectedIndex - 1];
                    await selectedAttractie.Herstart();
                }
            }
            else if (key >= ConsoleKey.D1 && key <= ConsoleKey.D6)
            {
                int monteurIndex = (int)key - (int)ConsoleKey.D1;
                var selectedAttractie = Attracties[selectedIndex - 1];
                var monteur = MonteurContext.GetMonteur(monteurIndex);
                monteur.Beheer(selectedAttractie);
            }
        }
    }
}
