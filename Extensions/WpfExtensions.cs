using System.Windows.Controls;

namespace MeloongCore.Extensions;
public static class WpfExtensions {

    public static bool Any(this UIElementCollection? Arr) => Arr?.Count > 0;

}
