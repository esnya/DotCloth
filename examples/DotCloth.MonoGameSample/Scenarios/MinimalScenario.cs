namespace DotCloth.MonoGameSample.Scenarios;

public sealed class MinimalScenario : IScenario
{
    public string Name => "Minimal";
    public int GridSize => 10;
    public ForceCloth Create(ForceModel model) => ClothFactory.Create(GridSize, model);
    public void Update(float dt) { }
}
