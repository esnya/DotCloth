namespace DotCloth.MonoGameSample.Scenarios;

public sealed class LargeScenario : IScenario
{
    public string Name => "Large";
    public int GridSize => 20;
    public ForceCloth Create(string model) => ClothFactory.Create(GridSize, model);
}
