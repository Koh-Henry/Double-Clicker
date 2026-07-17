using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

public interface IInputInjector
{
    void Click(MouseButtonKind button);
}
