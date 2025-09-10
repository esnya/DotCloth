namespace DotCloth.MonoGameSample.Scenarios;

public sealed class MinimalScenario : IScenario
{
    public string Name => "Minimal";
    public int GridSize => 10;
    public ForceCloth Create(string model) => ClothFactory.Create(GridSize, model);
}
