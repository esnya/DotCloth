namespace DotCloth.MonoGameSample.Scenarios;

public sealed class XLargeScenario : IScenario
{
    public string Name => "XLarge";
    public int GridSize => 30;
    public ForceCloth Create(string model) => ClothFactory.Create(GridSize, model);
}
