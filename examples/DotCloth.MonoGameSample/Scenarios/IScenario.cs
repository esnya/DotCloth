using DotCloth;

namespace DotCloth.MonoGameSample.Scenarios;

public interface IScenario
{
    string Name { get; }
    int GridSize { get; }
    ForceCloth Create(ForceModel model);
    void Update(float dt);
}
