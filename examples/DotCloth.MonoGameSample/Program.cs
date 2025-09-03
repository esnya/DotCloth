using System;

namespace DotCloth.MonoGameSample;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using var game = new SampleGame();
        game.Run();
    }
}

