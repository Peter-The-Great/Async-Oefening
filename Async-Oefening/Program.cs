using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public enum AttractieStatus
{
    Werkt,
    Kapot,
    Opstarten
}

public class Logger
{
    private static StreamWriter writer;

    public static async Task Open()
    {
        writer = new StreamWriter("log.txt");
        await writer.WriteLineAsync("Log started.");
    }

    public static async Task Write(string text)
    {
        await writer.WriteLineAsync(text);
    }
}

public static class Willekeurig
{
    public static readonly Random Random = new Random();

    public static async Task Pauzeer(int milliSeconden, double willekeurigheid = 0.3)
    {
        await Task.Delay((int)(milliSeconden * (1 + willekeurigheid * (2 * Random.NextDouble() - 1))));
    }
}

public class Attractie
{
    public string Naam { get; }
    public AttractieStatus Status { get; private set; }

    public Attractie(string naam)
    {
        Naam = naam;
        Status = AttractieStatus.Werkt;
    }

    public int AantalWachtenden()
    {
        // Simuleer ingewikkelde AI-logica
        Willekeurig.Pauzeer(1000).Wait();
        return Willekeurig.Random.Next(31);
    }

    private async Task VerzendHerstartCommando()
    {
        var delayTask = Willekeurig.Pauzeer(1000);
        var timeoutTask = Task.Delay(1200);
        var completedTask = await Task.WhenAny(delayTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new Exception("VerzendHerstartCommando heeft een time-out bereikt.");
        }
    }

    public async Task Herstart()
    {
        Status = AttractieStatus.Opstarten;
        await Logger.Write($"Attractie {Naam} aan het opstarten...");

        try
        {
            await VerzendHerstartCommando();
            Status = AttractieStatus.Werkt;
            await Logger.Write($"Attractie {Naam} is opgestart.");
        }
        catch (Exception ex)
        {
            Status = AttractieStatus.Kapot;
            await Logger.Write($"Attractie {Naam} is kapot: {ex.Message}");
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
                throw new Exception("Een monteur kan maximaal vijf attracties beheren.");
            }
            BeheerdeAttracties.Add(attractie);
        }
    }
}

public class MonteurContext
{
    private static List<Monteur> Monteurs { get; } = new List<Monteur>();

    public static int AantalMonteurs()
    {
        // Simuleer database-operaties
        Thread.Sleep(100);
        return Monteurs.Count;
    }

    public static Monteur GetMonteur(int index)
    {
        // Simuleer database-operaties
        Thread.Sleep(100);
        if (index >= 0 && index < Monteurs.Count)
        {
            return Monteurs[index];
        }
        return null;
    }

    public static void VoegMonteurToe(Monteur monteur)
    {
        // Simuleer database-operaties
        Thread.Sleep(100);
        Monteurs.Add(monteur);
    }
}

public class AdminPaneel
{
    private static List<Attractie> attracties = new List<Attractie>
    {
        new Attractie("Draaimolen"),
        new Attractie("Reuzenrad"),
        new Attractie("Achtbaan"),
        new Attractie("Achtbaan 2"),
        new Attractie("Spin"),
        new Attractie("Schommel")
    };

    public static async Task GemiddeldeWachtenden()
    {
        Console.WriteLine("[ ] Gemiddeld aantal wachtenden: berekening bezig");

        // Simuleer AI-berekeningen
        await Task.Delay(2000);

        var gemiddeldAantal = attracties.Select(attractie => attractie.AantalWachtenden()).Average();
        Console.WriteLine($"[ ] Gemiddeld aantal wachtenden: {gemiddeldAantal:F2}");
    }

    public static async Task Main()
    {
        await Logger.Open();

        Console.WriteLine("Dit is het adminpaneel!");

        int selectedIndex = 0;
        bool menuOpen = true;

        while (menuOpen)
        {
            Console.Clear();

            Console.WriteLine("[ ] Gemiddeld aantal wachtenden: onbekend");

            for (int i = 0; i < attracties.Count; i++)
            {
                var isSelected = i == selectedIndex;
                var statusText = attracties[i].Status == AttractieStatus.Werkt ? "(werkt)" :
                                 attracties[i].Status == AttractieStatus.Kapot ? "(kapot)" :
                                 "(opstarten)";
                var selectionIndicator = isSelected ? "[X]" : "[ ]";

                Console.WriteLine($"{selectionIndicator} ({i + 1}) {attracties[i].Naam} {statusText}");
            }

            for (int i = 0; i < MonteurContext.AantalMonteurs(); i++)
            {
                var monteur = MonteurContext.GetMonteur(i);
                var attractiesInBeheer = string.Join(", ", monteur.BeheerdeAttracties.Select(a => a.Naam));
                Console.WriteLine($"{i + 1}) Monteur {i + 1}: {attractiesInBeheer}");
            }

            var key = Console.ReadKey().Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = Math.Min(attracties.Count - 1, selectedIndex + 1);
                    break;
                case ConsoleKey.Enter:
                    if (selectedIndex == 0)
                    {
                        await GemiddeldeWachtenden();
                    }
                    else if (selectedIndex <= attracties.Count)
                    {
                        var selectedAttractie = attracties[selectedIndex - 1];
                        await selectedAttractie.Herstart();
                    }
                    break;
                default:
                    if (char.IsDigit((char)key) && Convert.ToChar(key) != '0')
                    {
                        int monteurIndex = int.Parse(key.ToString()) - 1;
                        if (monteurIndex < MonteurContext.AantalMonteurs())
                        {
                            var selectedMonteur = MonteurContext.GetMonteur(monteurIndex);
                            selectedMonteur.Beheer(attracties[selectedIndex - 1]);
                        }
                    }
                    break;
            }
        }
    }
}