namespace DotCloth.MonoGameSample.Scenarios;

public sealed class XLargeScenario : IScenario
{
    public string Name => "XLarge";
    public int GridSize => 30;
    public ForceCloth Create(ForceModel model) => ClothFactory.Create(GridSize, model);
    public void Update(float dt) { }
}
