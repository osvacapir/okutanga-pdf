using Microsoft.Maui.Devices;

namespace OkutangaPDF.Services;

public interface IAppShellService
{
    bool IsDesktopShell { get; }

    /// <summary>UI minimalista do leitor (telefone): barra de páginas + menu, sem toolbar completa.</summary>
    bool UseCompactReaderUi { get; }

    DeviceIdiom DeviceIdiom { get; }
}
