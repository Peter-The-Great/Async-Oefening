namespace Logger;
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
    private static readonly object lockObject = new object();

    public static async Task Open(string text)
    {
        await Task.Delay(100); // Simulate file opening
        lock (lockObject)
        {
            using (StreamWriter writer = File.AppendText("log.txt"))
            {
                writer.WriteLine(text);
            }
        }
    }

    public static async Task Write(string text)
    {
        await Task.Delay(100); // Simulate writing to log
        lock (lockObject)
        {
            using (StreamWriter writer = File.AppendText("log.txt"))
            {
                writer.WriteLine(text);
            }
        }
    }
}
public enum AttractieStatus
{
    Werkt,
    Kapot,
    Opstarten
}

public class Attractie
{
    public string Naam { get; set; }
    public AttractieStatus Status { get; private set; }
    public Attractie(string Naam, AttractieStatus status)
    {
        this.Naam = Naam;
        this.Status = status;
    }
    public async Task<int> AantalWachtenden()
    {
        await Willekeurig.Pauzeer(1000); // hier wordt zogenaamd ingewikkelde AI gedaan om het aantal wachtende te bepalen mbv een camera en face recognition
        return Willekeurig.Random.Next(0, 30);
    }
    private async Task VerzendHerstartCommando()
    {
        await Willekeurig.Pauzeer(1000); // verzend een commando naar een stukje hardware
        if (Willekeurig.Random.NextDouble() > .9) // soms gaat er iets fout
            throw new Exception("Error bij opstarten");
    }
    public async Task Herstart()
    {
        Status = AttractieStatus.Opstarten;
        await Logger.Write("Opstarten " + Naam);
        try
        {
            await VerzendHerstartCommando();
        }
        catch
        {
            Status = AttractieStatus.Kapot;
            return;
        }
        Status = AttractieStatus.Werkt;
        await Logger.Write("Opstarten " + Naam);
    }
}
public class Monteur
{
    private readonly object lockObject = new object();

    public List<Attractie> Beheert { get; set; } = new List<Attractie>();

    public void Beheer(Attractie attractie)
    {
        lock (lockObject)
        {
            if (Beheert.Count >= 5)
            {
                throw new Exception("Een monteur kan niet meer dan vijf attracties beheren.");
            }
            Beheert.Add(attractie);
        }
    }

    public IReadOnlyList<Attractie> InBeheer()
    {
        lock (lockObject)
        {
            return Beheert.ToList().AsReadOnly();
        }
    }
}

public class MonteurContext
{
    private static List<Monteur> monteurs = new List<Monteur>();
    public static async Task<int> AantalMonteurs()
    {
        await Task.Delay(100); // haalt zogenaamd op uit de database
        return monteurs.Count;
    }
    public static async Task<Monteur> GetMonteur(int index)
    {
        await Task.Delay(100); // haalt zogenaamd op uit de database
        return monteurs[index];
    }
    public static async Task VoegMonteurToe(Monteur monteur)
    {
        await Task.Delay(100); // schrijf zogenaamd weg in de database
        monteurs.Add(monteur);
    }
}
public class Optie
{
    public string Title { get; set; }
    public Action Actie { get; set; }

    public Optie(string title, Action actie)
    {
        Title = title;
        Actie = actie;
    }
}

public static class AdminPaneel
{
    private static List<Attractie> attracties = new List<Attractie>
    {
        new Attractie("Draaimolen", AttractieStatus.Werkt),
        new Attractie("Reuzenrad", AttractieStatus.Werkt),
        new Attractie("Achtbaan", AttractieStatus.Kapot),
        new Attractie("Achtbaan 2", AttractieStatus.Werkt),
        new Attractie("Spin", AttractieStatus.Werkt),
        new Attractie("Schommel", AttractieStatus.Opstarten),
    };

    public static async Task<double> GemiddeldeWachtenden()
    {
        int totaal = 0;
        List<Task<int>> taken = new List<Task<int>>();
        for (int i = 0; i < await MonteurContext.AantalMonteurs(); i++)
            foreach (Attractie g in (await MonteurContext.GetMonteur(i)).Beheert)
                taken.Add(Task.Run(async () => await g.AantalWachtenden()));
        foreach (var t in taken)
            totaal += await t;
        if (taken.Count == 0)
        {
            return 0;
        }
        return totaal / taken.Count;
    }

    public static async Task Main(string[] args)
    {
        Console.CursorVisible = false;
        var selectedOption = 0;
        string wachtenden;
        Console.WriteLine("Dit is het adminpaneel!");
        List<Optie> opties = new List<Optie>()
        {
            new Optie("Nieuwe monteur", async () => await MonteurContext.VoegMonteurToe(new Monteur())),
            new Optie("Update gemiddelde wachtenden", async () => {
                wachtenden = "Gemiddelde wachtenden wordt uitgerekend";
                wachtenden = "Het gemiddeld aantal wachtenden is " + await GemiddeldeWachtenden();
            })
        };

        while (true)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            wachtenden = await GemiddeldeWachtenden() + " wachtenden";
            Console.WriteLine($"Gemiddeld aantal wachtenden: {await GemiddeldeWachtenden()}");
            for (int i = 0; i < attracties.Count; i++)
            {
                var attractie = attracties[i];
                var isSelected = i == selectedOption;
                var statusText = attractie.Status switch
                {
                    AttractieStatus.Werkt => "(werkt)",
                    AttractieStatus.Kapot => "(kapot)",
                    AttractieStatus.Opstarten => "(opstarten)",
                    _ => "",
                };

                var selectionMarker = isSelected ? "[X]" : "[ ]";
                Console.WriteLine($"{selectionMarker} ({i + 1}) {attractie.Naam} {statusText}");
            }


            for (int i = 0; i < MonteurContext.AantalMonteurs().Result; i++)
            {
                var monteur = MonteurContext.GetMonteur(i).Result;
                var attractiesInBeheer = monteur.Beheert.Select(a => a.Naam).ToList();
                Console.WriteLine($"{i + 1}) Monteur {i + 1}: {string.Join(", ", attractiesInBeheer)}");
            }

            Console.WriteLine("Gebruik de pijltjes toetsen om te navigeren in het menu");
            Console.WriteLine("Als een attractie geselecteerd is, toets enter als je die wilt herstarten");
            Console.WriteLine("Als een monteur geselecteerd is, toets een cijfer van 1 t/m 6 om een attractie in het beheer van een monteur te zetten");
            Console.WriteLine("Als het gemiddeld aantal wachtenden selecteerd is, toets enter om te (her)berekenen");

            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.UpArrow)
            {
                if (selectedOption > 0)
                    selectedOption--;
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (selectedOption < attracties.Count - 1)
                    selectedOption++;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                var selectedAttractie = attracties[selectedOption];

                if (selectedAttractie.Status == AttractieStatus.Kapot)
                {
                    // Herstart de geselecteerde attractie
                    await selectedAttractie.Herstart();
                }
                else
                {
                    Console.WriteLine("Deze actie is alleen beschikbaar voor attracties met status 'Kapot'. Druk op een willekeurige toets om verder te gaan.");
                    Console.ReadKey();
                }
            }
        }
    }
}